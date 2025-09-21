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

namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private string deliveryAddress;

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
                            item.ProductId == quantityUpdate.ProductId &&
                            item.IsRental == quantityUpdate.IsRental);

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
            public bool IsRental { get; set; }
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
        public ActionResult AddToCart(int productId, bool isRental, int quantity = 1)
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
            var existing = cart.FirstOrDefault(c => c.ProductId == productId && c.IsRental == isRental);
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
                    RentalFee = product.RentalFee,
                    Quantity = quantity,
                    IsRental = isRental
                });
            }

            Session["Cart"] = cart;
            TempData["SuccessMessage"] = "Item added to cart successfully!";
            return RedirectToAction("Index");
        }

        // Remove from Cart
        public ActionResult RemoveFromCart(int productId, bool isRental)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId && c.IsRental == isRental);
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

            ViewBag.OrderType = cart.Any(c => c.IsRental) ? "Rent" : "Buy";

            return View();
        }

        // POST: Checkout with Stock Validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(Order model, string Priority)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
                return RedirectToAction("Index");

            // ✅ CRITICAL: Real-time stock validation before processing order
            var stockValidationErrors = new List<string>();
            foreach (var cartItem in cart)
            {
                var product = db.Products.Find(cartItem.ProductId);
                if (product == null)
                {
                    stockValidationErrors.Add($"Product {cartItem.ProductName} is no longer available.");
                    continue;
                }

                if (product.Stock < cartItem.Quantity)
                {
                    if (product.Stock == 0)
                    {
                        stockValidationErrors.Add($"{cartItem.ProductName} is out of stock.");
                    }
                    else
                    {
                        stockValidationErrors.Add($"Only {product.Stock} units of {cartItem.ProductName} are available (you have {cartItem.Quantity} in cart).");
                    }
                }
            }

            if (stockValidationErrors.Any())
            {
                ModelState.AddModelError("", "Stock validation failed: " + string.Join(" ", stockValidationErrors));
                TempData["ErrorMessage"] = "Please update your cart - some items are no longer available in the requested quantities.";
                return View(model);
            }

            // ✅ VALIDATION: Rental date checks
            if (cart.Any(c => c.IsRental))
            {
                if (!model.RentStartDate.HasValue || !model.RentEndDate.HasValue)
                {
                    ModelState.AddModelError("", "Rental start and end dates must be provided.");
                    return View(model);
                }

                var days = (model.RentEndDate.Value - model.RentStartDate.Value).Days;
                if (days < 1) days = 1;
            }

            // Now create the order and calculate the total
            var order = new Order
            {
                CustomerId = User.Identity.GetUserId(),
                OrderDate = DateTime.UtcNow,
                PaymentStatus = "Pending",
                OrderType = cart.Any(c => c.IsRental) ? "Rent" : "Buy",
                DeliveryAddress = model.DeliveryAddress,
                OrderItems = new List<OrderItem>(),
                RentStartDate = model.RentStartDate,
                RentEndDate = model.RentEndDate,
                Priority = !string.IsNullOrEmpty(Priority) ? Priority : "Normal" // Use selected priority or default to Normal
            };

            decimal total = 0;

            // 🔍 CRITICAL: ADD DETAILED LOGGING TO FIND THE ISSUE
            System.Diagnostics.Trace.TraceInformation($"=== CART ANALYSIS START ===");
            System.Diagnostics.Trace.TraceInformation($"Cart has {cart.Count} items");

            foreach (var c in cart)
            {
                System.Diagnostics.Trace.TraceInformation($"Processing: {c.ProductName}, Qty: {c.Quantity}, IsRental: {c.IsRental}");

                if (c.IsRental)
                {
                    int rentalDays = (model.RentEndDate.Value - model.RentStartDate.Value).Days;
                    if (rentalDays < 1) rentalDays = 1;

                    // ✅ CONVERT TO RANDS if stored as cents
                    decimal rentalFeeInRands = c.RentalFee;
                    decimal depositInRands = c.Deposit ?? 0;

                    // 🔍 LOG THE RAW VALUES
                    System.Diagnostics.Trace.TraceInformation($"RENTAL - Raw Fee: {c.RentalFee}, Raw Deposit: {c.Deposit}");

                    decimal rentalCost = rentalFeeInRands * c.Quantity * rentalDays;
                    decimal deposit = depositInRands;

                    System.Diagnostics.Trace.TraceInformation($"RENTAL - Days: {rentalDays}, Fee: {rentalFeeInRands}, Cost: {rentalCost}, Deposit: {deposit}");

                    total += rentalCost + deposit;
                    System.Diagnostics.Trace.TraceInformation($"Subtotal after rental: {total}");

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        Price = rentalCost,
                        IsRental = true,
                        RentalStartDate = model.RentStartDate,
                        RentalEndDate = model.RentEndDate,
                        RentalFeePerDay = rentalFeeInRands,
                        Deposit = deposit
                    });

                    // Note: Stock will be reduced in transaction after order creation
                }
                else
                {
                    // ✅ CONVERT TO RANDS if stored as cents
                    decimal priceInRands = c.Price;

                    // 🔍 LOG THE RAW VALUE
                    System.Diagnostics.Trace.TraceInformation($"PURCHASE - Raw Price: {c.Price}");

                    decimal itemTotal = priceInRands * c.Quantity;

                    System.Diagnostics.Trace.TraceInformation($"PURCHASE - Price: {priceInRands}, Qty: {c.Quantity}, Total: {itemTotal}");

                    total += itemTotal;
                    System.Diagnostics.Trace.TraceInformation($"Subtotal after purchase: {total}");

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        Price = priceInRands,
                        IsRental = false
                    });

                    // Note: Stock will be reduced in transaction after order creation
                }
            }

            System.Diagnostics.Trace.TraceInformation($"=== FINAL TOTAL: {total} ===");

            // ✅ CRITICAL: TEMPORARY HARDCODE FOR TESTING
            // ✅ CRITICAL FIX: Capture the calculated total in a LOCAL VARIABLE immediately
            decimal finalAmount = total / 100;  // ← ADD THIS DIVISION BY 100
            System.Diagnostics.Trace.TraceInformation($"1. Calculated Final Amount: {finalAmount}");

            order.TotalPrice = finalAmount;
            System.Diagnostics.Trace.TraceInformation($"2. Assigned to order.TotalPrice: {order.TotalPrice}");
            // 📍 Geocode delivery address
            try
            {
                var coordinates = await GeocodeAddressWithMapbox(order.DeliveryAddress);
                if (coordinates != null)
                {
                    order.Latitude = coordinates.Value.Latitude;
                    order.Longitude = coordinates.Value.Longitude;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Geocoding failed: {ex.Message}");
            }

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

            // 🚚 Create DriverAssignment
            var assignment = new DriverAssignment
            {
                OrderId = order.Id,
                DriverId = null,
                Status = "Pending",
                ExpiryTime = DateTime.UtcNow.AddHours(2),
                CreatedDate = DateTime.UtcNow
            };
            db.DriverAssignments.Add(assignment);
            db.SaveChanges();

            // 🔗 PayFast redirect
            string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            var payfast = new PayFastService(baseUrl);

            System.Diagnostics.Trace.TraceInformation($"Using amount for PayFast: {finalAmount}");

            string paymentUrl = payfast.GeneratePaymentUrl(
                order.Id.ToString(),
                order.OrderType == "Rent" ? "African Heritage Rental" : "African Heritage Order",
                finalAmount
            );

            System.Diagnostics.Trace.TraceInformation($"Redirecting to PayFast with URL: {paymentUrl}");
            return Redirect(paymentUrl);
        }




        private async Task<(double Latitude, double Longitude)?> GeocodeAddressWithMapbox(string address)
        {
            string mapboxKey = ConfigurationManager.AppSettings["Mapbox:AccessToken"];

            if (string.IsNullOrEmpty(mapboxKey) || string.IsNullOrEmpty(address))
            {
                System.Diagnostics.Trace.TraceWarning("Mapbox key or address is missing");
                return null;
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // URL encode the address
                    var encodedAddress = Uri.EscapeDataString(address);

                    // Mapbox Geocoding API URL - optimized for South Africa
                    var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{encodedAddress}.json?access_token={mapboxKey}&country=ZA&limit=1";

                    var response = await httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Trace.TraceWarning($"Mapbox API returned: {response.StatusCode}");
                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(content);

                    // Check if we have results
                    if (result.features != null && result.features.Count > 0)
                    {
                        var firstFeature = result.features[0];
                        var coordinates = firstFeature.geometry.coordinates;

                        // Mapbox returns [longitude, latitude] order
                        double longitude = coordinates[0];
                        double latitude = coordinates[1];

                        return (latitude, longitude);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Mapbox geocoding error: {ex.Message}");
            }

            return null;
        }



        // After successful payment
        [AllowAnonymous]
        public ActionResult PaymentSuccess(string orderId)
        {
            var order = db.Orders.Find(int.Parse(orderId));
            if (order != null)
            {
                order.PaymentStatus = "Paid";  // ⚠️ You may later replace this with IPN validation
                // Priority is already set during checkout, no need to override it here
                db.SaveChanges();

                // 🔹 Create driver assignment (unclaimed, pending) - only if one doesn't exist
                var existingAssignment = db.DriverAssignments.FirstOrDefault(a => a.OrderId == order.Id);
                if (existingAssignment == null)
                {
                    var assignment = new DriverAssignment
                    {
                        OrderId = order.Id,
                        Status = "Pending",
                        ExpiryTime = DateTime.UtcNow.AddHours(1),
                        CreatedDate = DateTime.UtcNow,
                        AssignedDate = DateTime.UtcNow   // ensure not left as MinValue
                    };

                    db.DriverAssignments.Add(assignment);
                    db.SaveChanges();
                }

                // Clear the cart after successful payment
                Session.Remove("Cart");
                Session.Remove("OrderId");
            }

            ViewBag.Message = "Payment completed successfully!";
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

            ViewBag.Message = "Your payment was cancelled.";
            return View();
        }


        //PayFast server-to-server notification(IPN)
        [HttpPost]
        [AllowAnonymous]
        public ActionResult PaymentNotify()
        {
            // 1. Read all the data sent by PayFast into a NameValueCollection
            NameValueCollection incomingData = Request.Form;

            // 2. Get your passphrase from PayFast dashboard
            string merchantPassphrase = null; // Change to your actual passphrase if set

            // 3. ✅ MANUAL SIGNATURE VALIDATION
            bool isValid = ValidatePayFastSignature(incomingData, merchantPassphrase);

            if (!isValid)
            {
                System.Diagnostics.Trace.TraceError("ITN request validation FAILED. Potential fraud.");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 4. Extract data from the validated request
            string orderId = incomingData["m_payment_id"];
            string paymentStatus = incomingData["payment_status"];
            // ✅ THE CRITICAL LINE: Get the amount the user actually paid from PayFast's POST data
            decimal amountPaid = Decimal.Parse(incomingData["amount_gross"]);

            var order = db.Orders.Find(int.Parse(orderId));
            if (order == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.OK); // Acknowledge receipt even if order not found
            }

            // 5. ✅ CRITICAL: Verify the amount paid matches the order total in your database
            if (amountPaid != order.TotalPrice)
            {
                System.Diagnostics.Trace.TraceError($"ITN Amount MISMATCH for Order {orderId}. Paid: {amountPaid}, Expected: {order.TotalPrice}.");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            // 6. Process the valid and verified payment
            if (paymentStatus == "COMPLETE")
            {
                order.PaymentStatus = "Paid";
                db.SaveChanges();

                if (!db.DriverAssignments.Any(a => a.OrderId == order.Id))
                {
                    var assignment = new DriverAssignment
                    {
                        OrderId = order.Id,
                        DriverId = null,
                        Status = "Pending",
                        ExpiryTime = DateTime.UtcNow.AddHours(1),
                        CreatedDate = DateTime.UtcNow
                    };
                    db.DriverAssignments.Add(assignment);
                    db.SaveChanges();
                }

                // Clear the cart after successful payment
                Session.Remove("Cart");
                Session.Remove("OrderId");
            }
            else
            {
                order.PaymentStatus = paymentStatus;
                db.SaveChanges();
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
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
