#if NET7_0
using Microsoft.AspNetCore.Mvc;
using African_Beauty_Trading.CoreApp;
using African_Beauty_Trading.Models;
using African_Beauty_Trading.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Net;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string SessionCartKey = "Cart";

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // View Cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(SessionCartKey) ?? new List<CartItem>();
            return View(cart);
        }

        // POST: AddToCart
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = _db.Products.Find(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return RedirectToAction("Index");
            }

            if (product.Stock <= 0)
            {
                TempData["ErrorMessage"] = "Product is out of stock";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(SessionCartKey) ?? new List<CartItem>();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            int totalQuantityInCart = existing?.Quantity ?? 0;
            int requestedTotalQuantity = totalQuantityInCart + quantity;

            if (requestedTotalQuantity > product.Stock)
            {
                int availableToAdd = product.Stock - totalQuantityInCart;
                if (availableToAdd <= 0)
                {
                    TempData["ErrorMessage"] = "Cannot add more items. Stock limit reached.";
                    return RedirectToAction("Index");
                }
                TempData["ErrorMessage"] = $"Only {availableToAdd} more items can be added to cart.";
                return RedirectToAction("Index");
            }

            if (existing != null)
                existing.Quantity += quantity;
            else
                cart.Add(new CartItem { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = quantity });

            HttpContext.Session.SetObjectAsJson(SessionCartKey, cart);
            TempData["SuccessMessage"] = "Item added to cart successfully!";
            return RedirectToAction("Index");
        }

        // Remove from Cart
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(SessionCartKey);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null) cart.Remove(item);
                HttpContext.Session.SetObjectAsJson(SessionCartKey, cart);
            }
            return RedirectToAction("Index");
        }

        // GET: Checkout
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(SessionCartKey);
            if (cart == null || !cart.Any()) return RedirectToAction("Index");
            return View();
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string address, string city, string postalCode, string phone, string notes)
        {
            try
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(SessionCartKey) ?? new List<CartItem>();
                if (!cart.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;

                var order = new Order
                {
                    CustomerId = userId,
                    OrderDate = DateTime.Now,
                    DeliveryAddress = $"{address}, {city}, {postalCode}",
                    PaymentStatus = "Pending",
                    CourierName = "PEP",
                    CourierStatus = "Awaiting Payment",
                    Priority = "Normal",
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0m;
                foreach (var c in cart)
                {
                    decimal itemTotal = c.Price * c.Quantity;
                    totalAmount += itemTotal;
                    order.OrderItems.Add(new OrderItem { ProductId = c.ProductId, Quantity = c.Quantity, Price = itemTotal });
                }

                order.TotalPrice = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        _db.Orders.Add(order);
                        _db.SaveChanges();

                        foreach (var cartItem in cart)
                        {
                            var product = _db.Products.Find(cartItem.ProductId);
                            if (product != null)
                            {
                                if (product.Stock >= cartItem.Quantity)
                                    product.Stock -= cartItem.Quantity;
                                else
                                {
                                    transaction.Rollback();
                                    TempData["ErrorMessage"] = $"Insufficient stock for {product.Name}. Please update your cart.";
                                    return RedirectToAction("Index");
                                }
                            }
                        }

                        _db.SaveChanges();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Order processing failed. Please try again.";
                        return RedirectToAction("Index");
                    }
                }

                // Redirect to PayFast
                var baseUrl = string.Concat(Request.Scheme, "://", Request.Host.Value);
                var payfast = new PayFastService(baseUrl);
                var paymentUrl = payfast.GeneratePaymentUrl(order.Id.ToString(), "African Heritage Order", order.TotalPrice);

                // persist orderId in session for reference
                HttpContext.Session.SetInt32("LastOrderId", order.Id);

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Checkout error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        public IActionResult PaymentSuccess(string m_payment_id)
        {
            var orderId = m_payment_id ?? Request.Query["orderId"].ToString();
            if (string.IsNullOrEmpty(orderId))
            {
                ViewBag.Message = "Invalid order reference. Please contact support.";
                return View();
            }

            var order = _db.Orders.Find(int.Parse(orderId));
            if (order != null && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.CourierStatus = "Processing";
                order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);
                order.ShippedDate = DateTime.Now.AddDays(1);
                order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');
                _db.SaveChanges();
            }

            HttpContext.Session.Remove(SessionCartKey);
            HttpContext.Session.Remove("LastOrderId");

            ViewBag.Message = "Payment completed successfully! Your order is being processed.";
            ViewBag.OrderNumber = order?.Id;
            ViewBag.TrackingNumber = order?.CourierTrackingNumber;
            return View();
        }

        [AllowAnonymous]
        public IActionResult PaymentCancel(string orderId)
        {
            if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out int id))
            {
                var order = _db.Orders.Find(id);
                if (order != null)
                {
                    order.PaymentStatus = "Cancelled";
                    _db.SaveChanges();
                }
            }
            TempData["ErrorMessage"] = "Payment was cancelled.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult PaymentNotify()
        {
            try
            {
                // Read form values
                var form = Request.Form;
                var data = new NameValueCollection();
                foreach (var k in form.Keys)
                {
                    data[k.ToString()] = form[k.ToString()];
                }

                // Validate signature (original implementation expects MD5 of payload)
                string passphrase = null;
                bool valid = ValidatePayFastSignature(data, passphrase);
                if (!valid) return StatusCode((int)HttpStatusCode.BadRequest);

                var orderId = data["m_payment_id"];
                var paymentStatus = data["payment_status"];
                var amountGross = data["amount_gross"];

                if (string.IsNullOrEmpty(orderId)) return StatusCode((int)HttpStatusCode.BadRequest);

                var order = _db.Orders.Find(int.Parse(orderId));
                if (order == null) return Ok();

                if (paymentStatus == "COMPLETE")
                {
                    var paidAmount = decimal.Parse(amountGross, CultureInfo.InvariantCulture);
                    if (paidAmount == order.TotalPrice)
                    {
                        order.PaymentStatus = "Paid";
                        order.CourierStatus = "Processing";
                        order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);
                        order.ShippedDate = DateTime.Now.AddDays(1);
                        order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');
                        _db.SaveChanges();

                        HttpContext.Session.Remove(SessionCartKey);
                        HttpContext.Session.Remove("LastOrderId");
                        // Optionally send email
                    }
                    else
                    {
                        // log mismatch
                    }
                }
                else
                {
                    order.PaymentStatus = paymentStatus;
                    _db.SaveChanges();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // log
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private bool ValidatePayFastSignature(NameValueCollection data, string passphrase)
        {
            var signatureSent = data["signature"];
            if (string.IsNullOrEmpty(signatureSent)) return false;

            var filtered = new NameValueCollection(data);
            filtered.Remove("signature");
            var keys = filtered.AllKeys.OrderBy(k => k);
            var sb = new StringBuilder();
            foreach (var key in keys)
            {
                sb.Append($"{key}={HttpUtility.UrlEncode(filtered[key])}&");
            }
            var payload = sb.ToString().TrimEnd('&');
            if (!string.IsNullOrEmpty(passphrase)) payload += "&passphrase=" + HttpUtility.UrlEncode(passphrase);

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return computed == signatureSent;
            }
        }
    }
}
#endif