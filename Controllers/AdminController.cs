using African_Beauty_Trading.Models;
using African_Beauty_Trading.Services;
using African_Beauty_Trading.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity; // Add this for .Include() method
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private ApplicationDbContext db = new ApplicationDbContext();
        private UserManager<ApplicationUser> _userManager;
        private EmailService emailService = new EmailService();



        public AdminController()
        {
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        public ActionResult Users()
        {
            var users = db.Users.ToList();

            // Create a view model to pass user data with roles
            var userViewModels = users.Select(user => new UserViewModel
            {
                User = user,
                Role = GetUserRole(user.Id)
            }).ToList();

            return View(userViewModels);
        }

        private string GetUserRole(string userId)
        {
            var roles = _userManager.GetRoles(userId);
            return roles.FirstOrDefault() ?? "Customer";
        }

        [HttpPost]
        public async Task<ActionResult> UpdateUser(string userId, string role, bool emailConfirmed, string lockoutEnd)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Update email confirmation
                    user.EmailConfirmed = emailConfirmed;

                    // Update lockout
                    switch (lockoutEnd)
                    {
                        case "1":
                            user.LockoutEndDateUtc = DateTime.UtcNow.AddHours(1);
                            break;
                        case "24":
                            user.LockoutEndDateUtc = DateTime.UtcNow.AddHours(24);
                            break;
                        case "168":
                            user.LockoutEndDateUtc = DateTime.UtcNow.AddHours(168);
                            break;
                        case "permanent":
                            user.LockoutEndDateUtc = DateTime.MaxValue;
                            break;
                        default:
                            user.LockoutEndDateUtc = null;
                            break;
                    }

                    // Update role
                    var currentRoles = await _userManager.GetRolesAsync(user.Id);
                    await _userManager.RemoveFromRolesAsync(user.Id, currentRoles.ToArray());
                    await _userManager.AddToRoleAsync(user.Id, role);

                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "User not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        return Json(new { success = true });
                    }
                    return Json(new { success = false, error = string.Join(", ", result.Errors) });
                }
                return Json(new { success = false, error = "User not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
         // Dashboard home
        public ActionResult Dashboard()
        {
            ViewBag.NavActive = "Dashboard";

            // KPIs
            var paidCount = db.Orders.Count(o => o.PaymentStatus == "Paid");
            var pendingCount = db.Orders.Count(o => o.PaymentStatus == "Pending");
            var cancelledCount = db.Orders.Count(o => o.PaymentStatus == "Cancelled");
            var totalCount = db.Orders.Count();
            var totalRevenue = db.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .Select(o => (decimal?)o.TotalPrice).Sum() ?? 0m;

            ViewBag.PaidOrders = paidCount;
            ViewBag.PendingOrders = pendingCount;
            ViewBag.CancelledOrders = cancelledCount;
            ViewBag.TotalOrders = totalCount;
            ViewBag.TotalRevenue = totalRevenue;

            // Last 7 days: counts for Paid vs Pending - FIXED for nullable DateTime
            var start = DateTime.Today.AddDays(-6);
            var last7 = db.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate >= start)
                .ToList()
                .GroupBy(o => o.OrderDate.Value.Date) // Use .Value.Date for nullable DateTime
                .ToDictionary(g => g.Key, g => new {
                    Paid = g.Count(x => x.PaymentStatus == "Paid"),
                    Pending = g.Count(x => x.PaymentStatus == "Pending")
                });

            var labels = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).ToList();
            var paidSeries = labels.Select(d => last7.ContainsKey(d) ? last7[d].Paid : 0).ToList();
            var pendingSeries = labels.Select(d => last7.ContainsKey(d) ? last7[d].Pending : 0).ToList();

            ViewBag.ChartLabels = labels.Select(d => d.ToString("dd MMM")).ToArray();
            ViewBag.ChartPaid = paidSeries.ToArray();
            ViewBag.ChartPending = pendingSeries.ToArray();

            // Recent Orders (top 8)
            var recent = db.Orders
                .Include("Customer")
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToList();

            // Latest Notifications (top 10)
            var notifications = db.AdminNotifications
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .ToList();

            ViewBag.Notifications = notifications;

            return View(recent);
        }

        // List all orders
        public ActionResult Orders(string status)
        {
            var orders = db.Orders.Include("Customer").Include("OrderItems").AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.PaymentStatus == status);
            }

            return View(orders.ToList());
        }

        // View order details
        public ActionResult OrderDetails(int id)
        {
            var order = db.Orders.Include("OrderItems.Product").FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            return View(order);
        }

        // GET: Assign Courier
        public ActionResult AssignCourier(int id)
        {
            var order = db.Orders.Include("Customer").FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            // Courier options
            var couriers = new List<SelectListItem>
        {
            new SelectListItem { Value = "PEP", Text = "PEP Courier" },
            new SelectListItem { Value = "Other", Text = "Other Courier" }
        };

            ViewBag.CourierName = new SelectList(couriers, "Value", "Text");

            return View(order);
        }

        // POST: Save courier assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignCourier(int orderId, string courierName, string trackingNumber, DateTime? estimatedDelivery)
        {
            if (string.IsNullOrWhiteSpace(courierName))
            {
                TempData["Error"] = "Please select a courier.";
                return RedirectToAction("AssignCourier", new { id = orderId });
            }

            var order = db.Orders
                .Include(o => o.Customer)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null) return HttpNotFound();

            // Update order with courier assignment
            order.CourierName = courierName;
            order.CourierStatus = "Assigned";
            order.CourierTrackingNumber = trackingNumber;
            order.EstimatedDeliveryDate = estimatedDelivery ?? DateTime.Now.AddDays(3);
            order.ShippedDate = DateTime.Now;
            order.Priority = "Normal";

            db.SaveChanges();

            TempData["Message"] = $"Courier assigned successfully! Tracking Number: {trackingNumber}";

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        public async Task<bool> TestStudentEmail()
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress("22246252@dut4life.ac.za", "African Beauty Trading");
                    message.To.Add("22221164@dut4life.ac.za");
                    message.Subject = "Test from DUT4Life Account";
                    message.Body = "This is a test email from your DUT student account.";

                    using (var smtpClient = new SmtpClient("mail.dut.ac.za", 587))
                    {
                        smtpClient.Credentials = new NetworkCredential(
                            "22246252@dut4life.ac.za",
                            "$$Dut010929"
                        );
                        smtpClient.EnableSsl = true;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Timeout = 20000;

                        await smtpClient.SendMailAsync(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"DUT Email Test failed: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        [HttpPost]
        public JsonResult MarkNotificationAsRead(int id)
        {
            var note = db.AdminNotifications.Find(id);
            if (note != null)
            {
                note.IsRead = true;
                note.ReadDate = DateTime.Now;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult MarkAllNotificationsAsRead()
        {
            var notes = db.AdminNotifications.Where(n => !n.IsRead).ToList();
            foreach (var n in notes)
            {
                n.IsRead = true;
                n.ReadDate = DateTime.Now;
            }
            db.SaveChanges();
            return Json(new { success = true, count = notes.Count });
        }

        // GET: Create Order
        public ActionResult CreateOrder()
        {
            ViewBag.NavActive = "Orders";

            // Get all customers
            var customerRoleId = db.Roles.Where(r => r.Name == "Customer").Select(r => r.Id).FirstOrDefault();
            var customers = db.Users.Where(u => u.Roles.Any(r => r.RoleId == customerRoleId)).ToList();
            ViewBag.CustomerId = new SelectList(customers, "Id", "Email");

            // Get all products
            var products = db.Products.Where(p => p.Stock > 0).ToList();
            ViewBag.Products = products;

            return View();
        }

        // POST: Create Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrder(string customerId, List<OrderItemViewModel> orderItems, string Priority)
        {
            if (string.IsNullOrEmpty(customerId) || orderItems == null || !orderItems.Any())
            {
                TempData["Error"] = "Please select a customer and add at least one item.";
                return RedirectToAction("CreateOrder");
            }

            try
            {
                var customer = db.Users.Find(customerId);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction("CreateOrder");
                }

                // Create new order
                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now, // This sets the nullable DateTime
                    PaymentStatus = "Paid",
                    CourierName = "PEP",
                    CourierStatus = "Processing",
                    TotalPrice = 0,
                    OrderItems = new List<OrderItem>()
                };

                decimal totalPrice = 0;

                foreach (var item in orderItems.Where(i => i.Quantity > 0))
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product == null || product.Stock < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product?.Name ?? "unknown product"}.";
                        return RedirectToAction("CreateOrder");
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price * item.Quantity
                    };

                    totalPrice += orderItem.Price;
                    order.OrderItems.Add(orderItem);

                    // Reduce stock
                    product.Stock -= item.Quantity;
                }

                order.TotalPrice = totalPrice;
                order.Priority = !string.IsNullOrEmpty(Priority) ? Priority : "Normal";

                // Save order first to generate ID
                db.Orders.Add(order);
                db.SaveChanges();

                // Now generate tracking number with actual ID
                order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');
                db.SaveChanges();

                TempData["Message"] = "Order created successfully!";
                return RedirectToAction("OrderDetails", new { id = order.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating order: " + ex.Message;
                return RedirectToAction("CreateOrder");
            }
        }

        // Helper method to get product details for AJAX
        [HttpGet]
        public JsonResult GetProductDetails(int productId)
        {
            var product = db.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                name = product.Name,
                price = product.Price,
                stock = product.Stock
            }, JsonRequestBehavior.AllowGet);
        }

        // POST: Update Courier Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCourierStatus(int orderId, string courierStatus, string trackingNumber, DateTime? estimatedDelivery)
        {
            var order = db.Orders.Find(orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            order.CourierStatus = courierStatus;

            if (!string.IsNullOrEmpty(trackingNumber))
            {
                order.CourierTrackingNumber = trackingNumber;
            }

            if (estimatedDelivery.HasValue)
            {
                order.EstimatedDeliveryDate = estimatedDelivery;
            }

            // Update shipping date if status changed to Shipped
            if (courierStatus == "Shipped" && !order.ShippedDate.HasValue)
            {
                order.ShippedDate = DateTime.Now;
            }

            // Update delivery date if status changed to Delivered
            if (courierStatus == "Delivered")
            {
                order.DeliveredDate = DateTime.Now; // Use DeliveredDate instead of DeliveryDate
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Order status updated successfully.";
            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // View for managing courier assignments
        public ActionResult ManageCouriers()
        {
            var orders = db.Orders
                .Include("Customer")
                .Where(o => o.PaymentStatus == "Paid" && o.CourierStatus != "Delivered")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // Quick assign PEP courier
        [HttpPost]
        public ActionResult QuickAssignPep(int orderId)
        {
            var order = db.Orders.Find(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            try
            {
                order.CourierName = "PEP";
                order.CourierStatus = "Assigned";
                order.CourierTrackingNumber = "PEP" + order.Id.ToString().PadLeft(8, '0');
                order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);
                order.ShippedDate = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true, message = "PEP Courier assigned successfully", trackingNumber = order.CourierTrackingNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error assigning courier: " + ex.Message });
            }
        }

        // Bulk update courier status
        [HttpPost]
        public ActionResult BulkUpdateCourierStatus(int[] orderIds, string courierStatus)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return Json(new { success = false, message = "No orders selected." });
            }

            try
            {
                var orders = db.Orders.Where(o => orderIds.Contains(o.Id)).ToList();
                foreach (var order in orders)
                {
                    order.CourierStatus = courierStatus;

                    if (courierStatus == "Shipped" && !order.ShippedDate.HasValue)
                    {
                        order.ShippedDate = DateTime.Now;
                    }

                    if (courierStatus == "Delivered")
                    {
                        order.DeliveredDate = DateTime.Now; // Use DeliveredDate
                    }
                }

                db.SaveChanges();

                return Json(new { success = true, message = $"Updated {orders.Count} orders to {courierStatus} status." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating orders: " + ex.Message });
            }
        }

        // Order statistics for reporting
        public ActionResult OrderStatistics()
        {
            ViewBag.NavActive = "Statistics";

            // Monthly revenue - handle nullable OrderDate
            var monthlyRevenue = db.Orders
                .Where(o => o.PaymentStatus == "Paid" && o.OrderDate.HasValue)
                .AsEnumerable() // Switch to client-side for complex operations
                .GroupBy(o => new { Year = o.OrderDate.Value.Year, Month = o.OrderDate.Value.Month })
                .Select(g => new
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Revenue = g.Sum(o => o.TotalPrice),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList();

            ViewBag.MonthlyRevenue = monthlyRevenue;

            // Top products
            var topProducts = db.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    ProductName = db.Products.First(p => p.Id == g.Key).Name
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToList();

            ViewBag.TopProducts = topProducts;

            return View();
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

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
    public class UserViewModel
    {
        public ApplicationUser User { get; set; }
        public string Role { get; set; }
    }
}