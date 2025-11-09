using African_Beauty_Trading.Models;
using African_Beauty_Trading.Services;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using PayFast;
using PayFast.Base;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

namespace African_Beauty_Trading.Controllers
{

    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // View Cart
        public ActionResult Index()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

        // POST: Cart/UpdateCart with Stock Validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCart(List<CartQuantityUpdate> quantities, string actionType)
        {
            try
            {
                var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
                var stockErrors = new List<string>();

                if (quantities != null && quantities.Any())
                {
                    foreach (var quantityUpdate in quantities)
                    {
                        var cartItem = cart.FirstOrDefault(item =>
                            item.ProductId == quantityUpdate.ProductId);

                        if (cartItem != null)
                        {
                            // Get current product stock
                            var product = db.Products.Find(quantityUpdate.ProductId);
                            if (product == null)
                            {
                                stockErrors.Add($"Product {cartItem.ProductName} not found.");
                                continue;
                            }

                            // Validate requested quantity against available stock
                            int requestedQuantity = Math.Max(1, quantityUpdate.Quantity);

                            if (requestedQuantity > product.Stock)
                            {
                                // Set to maximum available stock
                                cartItem.Quantity = Math.Max(1, product.Stock);
                                stockErrors.Add($"Only {product.Stock} units of {cartItem.ProductName} are available. Quantity adjusted.");
                            }
                            else
                            {
                                cartItem.Quantity = requestedQuantity;
                            }
                        }
                    }

                    // Save updated cart back to session
                    Session["Cart"] = cart;

                    if (stockErrors.Any())
                    {
                        TempData["WarningMessage"] = string.Join(" ", stockErrors);
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Cart updated successfully!";
                    }
                }

                // Redirect based on action type
                if (actionType == "continue")
                {
                    return RedirectToAction("Browse", "Customer");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating cart: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ViewModel for quantity updates
        public class CartQuantityUpdate
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // Add to Cart with Stock Validation
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = db.Products.Find(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return RedirectToAction("Index");
            }

            // Check if product has sufficient stock
            if (product.Stock <= 0)
            {
                TempData["ErrorMessage"] = "Product is out of stock";
                return RedirectToAction("Index");
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // Calculate total quantity that would be in cart after adding
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            int totalQuantityInCart = existing?.Quantity ?? 0;
            int requestedTotalQuantity = totalQuantityInCart + quantity;

            // Validate against available stock
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

            // Add or update cart item
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            Session["Cart"] = cart;
            TempData["SuccessMessage"] = "Item added to cart successfully!";
            return RedirectToAction("Index");
        }

        // Remove from Cart
        public ActionResult RemoveFromCart(int productId)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                    cart.Remove(item);

                Session["Cart"] = cart;
            }
            return RedirectToAction("Index");
        }

        // Checkout
        // GET: Checkout
        public ActionResult Checkout()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
                return RedirectToAction("Index");

            return View();
        }

        // POST: Checkout with Stock Validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string address, string city, string postalCode, string phone, string notes)
        {
            try
            {
                var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
                if (!cart.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                // Create new order
                var order = new Order
                {
                    CustomerId = User.Identity.GetUserId(),
                    OrderDate = DateTime.Now,
                    DeliveryAddress = $"{address}, {city}, {postalCode}",
                    PaymentStatus = "Pending",
                    CourierName = "PEP",
                    CourierStatus = "Awaiting Payment",
                    Priority = "Normal",
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0; // Calculate total amount

                System.Diagnostics.Trace.TraceInformation($"=== CART ANALYSIS START ===");
                System.Diagnostics.Trace.TraceInformation($"Cart has {cart.Count} items");

                foreach (var c in cart)
                {
                    System.Diagnostics.Trace.TraceInformation($"Processing: {c.ProductName}, Qty: {c.Quantity}");

                    {
                        // ✅ CALCULATE AMOUNT (NO DIVISION)
                        decimal itemTotal = c.Price * c.Quantity;

                        System.Diagnostics.Trace.TraceInformation($"PURCHASE - Price: {c.Price}, Qty: {c.Quantity}, Total: {itemTotal}");

                        totalAmount += itemTotal;
                        System.Diagnostics.Trace.TraceInformation($"Subtotal after purchase: {totalAmount}");

                        // Store in database (NO DIVISION)
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = c.ProductId,
                            Quantity = c.Quantity,
                            Price = itemTotal, // NO DIVISION
                        });
                    }
                }

                System.Diagnostics.Trace.TraceInformation($"=== FINAL TOTAL: {totalAmount} ===");

                // ✅ CRITICAL FIX: NO DIVISION - Send the amount as is to PayFast
                decimal finalAmount = totalAmount; // NO DIVISION BY 100
                                                   // ✅ Ensure consistent rounding to 2 decimals (away from zero)
                decimal roundedAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero);
                System.Diagnostics.Trace.TraceInformation($"1. Calculated Final Amount: {finalAmount}");

                order.TotalPrice = roundedAmount;
                System.Diagnostics.Trace.TraceInformation($"2. Assigned to order.TotalPrice: {order.TotalPrice}");

                // SAVE THE ORDER AND REDUCE STOCK IN TRANSACTION
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Orders.Add(order);
                        db.SaveChanges();
                        System.Diagnostics.Trace.TraceInformation($"Order {order.Id} saved with TotalPrice: {order.TotalPrice}");

                        // ✅ CRITICAL: Reduce stock for all items in a single transaction
                        foreach (var cartItem in cart)
                        {
                            var product = db.Products.Find(cartItem.ProductId);
                            if (product != null)
                            {
                                // Final stock validation (in case of concurrent orders)
                                if (product.Stock >= cartItem.Quantity)
                                {
                                    product.Stock -= cartItem.Quantity;
                                    System.Diagnostics.Trace.TraceInformation($"Reduced stock for Product {cartItem.ProductId} by {cartItem.Quantity}. New stock: {product.Stock}");
                                }
                                else
                                {
                                    // This should not happen due to earlier validation, but handle gracefully
                                    transaction.Rollback();
                                    TempData["ErrorMessage"] = $"Insufficient stock for {product.Name}. Please update your cart.";
                                    return RedirectToAction("Index");
                                }
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        System.Diagnostics.Trace.TraceInformation("Stock reduction completed successfully");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Trace.TraceError($"Transaction failed: {ex.Message}");
                        TempData["ErrorMessage"] = "Order processing failed. Please try again.";
                        return RedirectToAction("Index");
                    }
                }

                // 🔗 PayFast redirect - Send the amount WITHOUT DIVISION
                string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
                var payfast = new PayFastService(baseUrl);

                System.Diagnostics.Trace.TraceInformation($"Using amount for PayFast (rounded): {roundedAmount}");

                string paymentUrl = payfast.GeneratePaymentUrl(
                    order.Id.ToString(),
                    "African Heritage Order",
                    roundedAmount // NO DIVISION - Send 800.00 for R800.00, rounded to 2 decimals
                );

                System.Diagnostics.Trace.TraceInformation($"Redirecting to PayFast with URL: {paymentUrl}");
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Checkout error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // After successful payment
        [AllowAnonymous]
        public ActionResult PaymentSuccess(string m_payment_id)
        {
            System.Diagnostics.Trace.TraceInformation($"=== PAYMENT SUCCESS CALLED ===");
            System.Diagnostics.Trace.TraceInformation($"m_payment_id: {m_payment_id}");

            // Also check for other possible parameter names
            string orderId = m_payment_id ?? Request.QueryString["orderId"] ?? Request.QueryString["pf_payment_id"];

            if (string.IsNullOrEmpty(orderId))
            {
                ViewBag.Message = "Invalid order reference. Please contact support with your payment details.";
                return View();
            }

            var order = db.Orders.Find(int.Parse(orderId));
            if (order != null)
            {
                System.Diagnostics.Trace.TraceInformation($"Found Order {order.Id}, Current Status: {order.PaymentStatus}");

                // CRITICAL: Update payment status immediately regardless of current status
                if (order.PaymentStatus != "Paid")
                {
                    System.Diagnostics.Trace.TraceInformation($"Updating Order {order.Id} from {order.PaymentStatus} to PAID");

                    order.PaymentStatus = "Paid";
                    order.CourierStatus = "Processing";
                    order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);
                    order.ShippedDate = DateTime.Now.AddDays(1);
                    order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');

                    db.SaveChanges();
                    System.Diagnostics.Trace.TraceInformation($"✅ PAYMENT SUCCESS: Order {order.Id} marked as PAID");
                }
                else
                {
                    System.Diagnostics.Trace.TraceInformation($"Order {order.Id} is already Paid");
                }

                // Clear cart
                Session.Remove("Cart");
                Session.Remove("OrderId");

                ViewBag.Message = "Payment completed successfully! Your order is being processed.";
                ViewBag.OrderNumber = order.Id;
                ViewBag.TrackingNumber = order.CourierTrackingNumber;
            }
            else
            {
                System.Diagnostics.Trace.TraceError($"❌ Order {orderId} not found in database");
                ViewBag.Message = "Order not found. Please contact support with your payment reference.";
            }

            return View();
        }
        // If user cancels
        [AllowAnonymous]
        public ActionResult PaymentCancel(string orderId)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                int id = int.Parse(orderId);
                var order = db.Orders.Find(id);

                if (order != null)
                {
                    order.PaymentStatus = "Cancelled"; // Mark as cancelled
                    db.SaveChanges();
                }
            }

            TempData["ErrorMessage"] = "Payment was cancelled.";
            return RedirectToAction("Index");
        }

        // PayFast server-to-server notification (IPN) - FIXED VERSION
        [HttpPost]
        [AllowAnonymous]
        public ActionResult PaymentNotify()
        {
            System.Diagnostics.Trace.TraceInformation("=== 🚀 PAYFAST IPN STARTED ===");

            try
            {
                // Log all incoming data
                System.Diagnostics.Trace.TraceInformation("=== 📦 INCOMING IPN DATA ===");
                foreach (string key in Request.Form.AllKeys)
                {
                    System.Diagnostics.Trace.TraceInformation("  " + key + ": " + Request.Form[key]);
                }

                NameValueCollection incomingData = Request.Form;

                // Validate signature
                string merchantPassphrase = null; // Set this if you have a passphrase
                bool isValid = ValidatePayFastSignature(incomingData, merchantPassphrase);

                if (!isValid)
                {
                    System.Diagnostics.Trace.TraceError("❌ IPN SIGNATURE VALIDATION FAILED");
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                // Extract data
                string orderId = incomingData["m_payment_id"];
                string paymentStatus = incomingData["payment_status"];
                string pfPaymentId = incomingData["pf_payment_id"];
                string amountGross = incomingData["amount_gross"];

                System.Diagnostics.Trace.TraceInformation("📋 IPN DATA - Order: " + orderId + ", Status: " + paymentStatus + ", PF ID: " + pfPaymentId + ", Amount: " + amountGross);

                if (string.IsNullOrEmpty(orderId))
                {
                    System.Diagnostics.Trace.TraceError("❌ IPN: No order ID received");
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                // Find and update order
                var order = db.Orders.Find(int.Parse(orderId));
                if (order == null)
                {
                    System.Diagnostics.Trace.TraceError("❌ IPN: Order " + orderId + " not found in database");
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }

                System.Diagnostics.Trace.TraceInformation("📊 ORDER FOUND - ID: " + order.Id + ", Current Status: " + order.PaymentStatus + ", Total: " + order.TotalPrice);

                // Process payment
                if (paymentStatus == "COMPLETE")
                {
                    // Verify amount
                    decimal paidAmount = decimal.Parse(amountGross, CultureInfo.InvariantCulture);
                    if (paidAmount == order.TotalPrice)
                    {
                        order.PaymentStatus = "Paid";
                        order.CourierStatus = "Processing";
                        order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);
                        order.ShippedDate = DateTime.Now.AddDays(1);
                        order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');

                        db.SaveChanges();

                        Session.Remove("Cart");
                        Session.Remove("OrderId");

                        System.Diagnostics.Trace.TraceInformation($"✅ IPN SUCCESS: Order {orderId} marked as PAID");

                        // Send email notification (optional)
                        SendPaymentConfirmation(order);
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceError("💰 AMOUNT MISMATCH: Paid " + paidAmount + ", Expected " + order.TotalPrice);
                    }
                }
                else
                {
                    order.PaymentStatus = paymentStatus;
                    db.SaveChanges();
                    System.Diagnostics.Trace.TraceInformation("ℹ️ IPN: Order " + order.Id + " status: " + paymentStatus);
                }

                System.Diagnostics.Trace.TraceInformation("=== ✅ PAYFAST IPN COMPLETED ===");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("💥 IPN ERROR: " + ex.Message);
                System.Diagnostics.Trace.TraceError("Stack Trace: " + ex.StackTrace);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        private void SendPaymentConfirmation(Order order)
        {
            try
            {
                // Implement email sending logic here
                System.Diagnostics.Trace.TraceInformation("Payment confirmation email would be sent for Order " + order.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Email sending failed: " + ex.Message);
            }
        }

        // ✅ MANUAL SIGNATURE VALIDATION METHOD (Follows PayFast's official process)
        private bool ValidatePayFastSignature(NameValueCollection data, string passphrase)
        {
            // Get the signature sent by PayFast
            string signatureSent = data["signature"];
            if (string.IsNullOrEmpty(signatureSent))
            {
                return false;
            }

            // Remove the 'signature' field from the data for validation
            var filteredData = new NameValueCollection(data);
            filteredData.Remove("signature");

            // Create a sorted dictionary of the remaining parameters
            var sortedKeys = filteredData.AllKeys.Where(k => k != "signature").OrderBy(k => k);
            StringBuilder builder = new StringBuilder();

            // Concatenate the parameters in alphabetical order
            foreach (var key in sortedKeys)
            {
                builder.Append($"{key}={HttpUtility.UrlEncode(filteredData[key])}&");
            }

            // Add the passphrase if it exists
            string payload = builder.ToString().TrimEnd('&');
            if (!string.IsNullOrEmpty(passphrase))
            {
                payload += $"&passphrase={HttpUtility.UrlEncode(passphrase)}";
            }

            // Create the MD5 hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Compare the computed signature with the one sent by PayFast
                return computedSignature == signatureSent;
            }
        }

        public ActionResult OrderConfirmation(int id)
        {
            var order = db.Orders.Find(id);
            return View(order);
        }
    }
}
