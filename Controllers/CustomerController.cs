using African_Beauty_Trading.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Browse all products
        [AllowAnonymous] // Allow browsing without login
        public ActionResult Browse(int? categoryId)
        {
            var products = db.Products
                .Include("Category") // Use string include
                .Where(p => p.Stock > 0) // Only show products in stock
                .AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            // Get categories for filter dropdown
            ViewBag.Categories = db.Categories.ToList();
            ViewBag.SelectedCategoryId = categoryId;

            return View(products.OrderByDescending(p => p.Featured).ThenBy(p => p.Name).ToList());
        }

        // Product details
        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            var product = db.Products
                .Include("Category") // Use string include
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return HttpNotFound();

            // Check if product is in stock
            ViewBag.IsInStock = product.Stock > 0;
            ViewBag.MaxQuantity = Math.Min(product.Stock, 10); // Limit to 10 or available stock

            return View(product);
        }

        // Dashboard: Show all orders for logged-in customer
        public ActionResult Dashboard()
        {
            var userId = User.Identity.GetUserId();
            var orders = db.Orders
                .Include("OrderItems.Product") // Use string include for nested properties
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(10) // Show only recent 10 orders
                .ToList();

            // Calculate dashboard statistics
            ViewBag.TotalOrders = db.Orders.Count(o => o.CustomerId == userId);
            ViewBag.PendingOrders = db.Orders.Count(o => o.CustomerId == userId && o.PaymentStatus == "Pending");
            ViewBag.CompletedOrders = db.Orders.Count(o => o.CustomerId == userId && o.PaymentStatus == "Paid");

            return View(orders);
        }

        // View all orders
        public ActionResult Orders()
        {
            var userId = User.Identity.GetUserId();
            var orders = db.Orders
                .Include("OrderItems.Product") // Use string include
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // View specific order details
        public ActionResult OrderDetails(int id)
        {
            var userId = User.Identity.GetUserId();
            var order = db.Orders
                .Include("OrderItems.Product") // Use string include
                .FirstOrDefault(o => o.Id == id && o.CustomerId == userId);

            if (order == null)
                return HttpNotFound();

            // Calculate delivery progress
            ViewBag.DeliveryProgress = CalculateDeliveryProgress(order);
            ViewBag.EstimatedDelivery = order.EstimatedDeliveryDate?.ToString("dd MMM yyyy") ?? "Not available";

            return View(order);
        }

        // Track specific order
        public ActionResult Track(int id)
        {
            var userId = User.Identity.GetUserId();
            var order = db.Orders
                .Include("OrderItems.Product") // Use string include
                .FirstOrDefault(o => o.Id == id && o.CustomerId == userId);

            if (order == null)
                return HttpNotFound();

            // Get tracking timeline
            ViewBag.TrackingTimeline = GetTrackingTimeline(order);
            ViewBag.EstimatedDelivery = order.EstimatedDeliveryDate?.ToString("dd MMM yyyy") ?? "Not available";

            return View(order);
        }

        // Customer profile
        public ActionResult Profile()
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // Update customer profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    // Update allowed fields (if they exist in your ApplicationUser class)
                    user.PhoneNumber = model.PhoneNumber;

                    // Only update these if the properties exist in your ApplicationUser
                    // If you don't have these properties, remove these lines
                    if (model.GetType().GetProperty("FirstName") != null)
                        user.GetType().GetProperty("FirstName")?.SetValue(user, model.FirstName);

                    if (model.GetType().GetProperty("LastName") != null)
                        user.GetType().GetProperty("LastName")?.SetValue(user, model.LastName);

                    if (model.GetType().GetProperty("Address") != null)
                        user.GetType().GetProperty("Address")?.SetValue(user, model.Address);

                    if (model.GetType().GetProperty("City") != null)
                        user.GetType().GetProperty("City")?.SetValue(user, model.City);

                    if (model.GetType().GetProperty("PostalCode") != null)
                        user.GetType().GetProperty("PostalCode")?.SetValue(user, model.PostalCode);

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
            }

            return View(model);
        }

        // Helper method to calculate delivery progress
        private int CalculateDeliveryProgress(Order order)
        {
            if (order.CourierStatus == "Delivered") return 100;
            if (order.CourierStatus == "Shipped") return 75;
            if (order.CourierStatus == "Processing") return 50;
            if (order.CourierStatus == "Assigned") return 25;
            return 0;
        }

        // Helper method to get tracking timeline
        private List<TrackingEvent> GetTrackingTimeline(Order order)
        {
            var timeline = new List<TrackingEvent>();

            // Order placed
            timeline.Add(new TrackingEvent
            {
                Event = "Order Placed",
                Date = order.OrderDate?.ToString("dd MMM yyyy HH:mm"),
                Description = "Your order has been received",
                IsCompleted = true
            });

            // Payment confirmed
            if (order.PaymentStatus == "Paid")
            {
                timeline.Add(new TrackingEvent
                {
                    Event = "Payment Confirmed",
                    Date = order.OrderDate?.AddMinutes(5).ToString("dd MMM yyyy HH:mm"),
                    Description = "Payment has been processed successfully",
                    IsCompleted = true
                });
            }

            // Processing
            timeline.Add(new TrackingEvent
            {
                Event = "Processing",
                Date = order.OrderDate?.AddHours(1).ToString("dd MMM yyyy HH:mm"),
                Description = "Preparing your order for shipment",
                IsCompleted = order.CourierStatus == "Processing" || order.CourierStatus == "Shipped" || order.CourierStatus == "Delivered"
            });

            // Shipped
            timeline.Add(new TrackingEvent
            {
                Event = "Shipped",
                Date = order.ShippedDate?.ToString("dd MMM yyyy HH:mm"),
                Description = $"Order shipped via {order.CourierName}",
                IsCompleted = order.CourierStatus == "Shipped" || order.CourierStatus == "Delivered"
            });

            // Out for delivery
            if (order.CourierStatus == "Shipped" || order.CourierStatus == "Delivered")
            {
                timeline.Add(new TrackingEvent
                {
                    Event = "Out for Delivery",
                    Date = order.EstimatedDeliveryDate?.AddHours(-2).ToString("dd MMM yyyy HH:mm"),
                    Description = "Your order is out for delivery",
                    IsCompleted = order.CourierStatus == "Delivered"
                });
            }

            // Delivered
            timeline.Add(new TrackingEvent
            {
                Event = "Delivered",
                Date = order.DeliveredDate?.ToString("dd MMM yyyy HH:mm"),
                Description = "Order has been delivered",
                IsCompleted = order.CourierStatus == "Delivered"
            });

            return timeline;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Tracking event model for timeline
    public class TrackingEvent
    {
        public string Event { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
    }
}