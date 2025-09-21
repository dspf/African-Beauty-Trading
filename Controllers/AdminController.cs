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
        private EmailService emailService = new EmailService();
        private OtpService otpService = new OtpService();


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

            // Last 7 days: counts for Paid vs Pending
            var start = DateTime.Today.AddDays(-6);
            var last7 = db.Orders
                .Where(o => o.OrderDate >= start)
                .ToList()   // materialize to do grouping in-memory (works across providers)
                .GroupBy(o => o.OrderDate.Value)
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

            // 🔔 Latest Notifications (top 10)
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

        // GET: Assign a driver
        public ActionResult AssignDriver(int id)
        {
            var order = db.Orders.Include("Customer").FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

             
           
            // Get all users who are in the "Driver" role
            var drivers = (from u in db.Users
                           from ur in u.Roles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where r.Name == "Driver"
                           select u).ToList();



            ViewBag.DriverId = new SelectList(drivers, "Id", "Email");

            return View(order);
        }

        // POST: Save driver assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignDriver(int orderId, string driverId)
        {
            var order = db.Orders
                .Include(o => o.Customer)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null) return HttpNotFound();

            // Check if there's already a pending assignment for this order
            var existingAssignment = db.DriverAssignments
                .FirstOrDefault(a => a.OrderId == orderId && a.Status == "Pending");

            if (existingAssignment != null)
            {
                // Update existing assignment with specific driver
                existingAssignment.DriverId = driverId;
                existingAssignment.Status = "Accepted";
                existingAssignment.ResponseDate = DateTime.UtcNow;
                existingAssignment.AssignedBy = User.Identity.GetUserId();
            }
            else
            {
                // Create new assignment directly accepted by admin
                var assignment = new DriverAssignment
                {
                    OrderId = orderId,
                    DriverId = driverId,
                    Status = "Accepted",
                    AssignedDate = DateTime.UtcNow,
                    ResponseDate = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddHours(24), // Give 24 hours for delivery
                    CreatedDate = DateTime.UtcNow,
                    AssignedBy = User.Identity.GetUserId()
                };
                db.DriverAssignments.Add(assignment);
            }

            // Generate OTP
            string otp = otpService.GenerateOtp();

            // Update order with driver assignment
            order.DriverId = driverId;
            order.DeliveryStatus = "Accepted"; // Use consistent status with driver flow
            order.DeliveryOtp = otp;
            order.OtpGeneratedAt = DateTime.UtcNow;
            order.DriverAssignedDate = DateTime.UtcNow;
            order.DriverAccepted = true;
            order.Priority = "Urgent"; // Agent-assigned orders are marked as urgent (red flag)

            db.SaveChanges();

            TempData["Message"] = $"Driver assigned successfully! OTP for customer: {otp}";

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
                        smtpClient.Timeout = 20000; // 20 seconds

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
                    OrderDate = DateTime.Now,
                    PaymentStatus = "Paid", // Admin created orders are marked as paid
                    DeliveryStatus = "Processing",
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
                        IsRental = item.IsRental
                    };

                    if (item.IsRental)
                    {
                        // Rental item
                        orderItem.RentalStartDate = item.RentalStartDate;
                        orderItem.RentalEndDate = item.RentalEndDate;
                        orderItem.RentalFeePerDay = product.RentalFee;
                        orderItem.Deposit = 0; // Set default deposit or add deposit field to Product model
                        
                        var days = (item.RentalEndDate - item.RentalStartDate).Days + 1;
                        orderItem.Price = product.RentalFee * days;
                    }
                    else
                    {
                        // Purchase item
                        orderItem.Price = product.Price * item.Quantity;
                    }

                    totalPrice += orderItem.Price;
                    order.OrderItems.Add(orderItem);

                    // Reduce stock
                    product.Stock -= item.Quantity;
                }

                order.TotalPrice = totalPrice;
                order.Priority = !string.IsNullOrEmpty(Priority) ? Priority : "Normal"; // Use selected priority or default to Normal
                db.Orders.Add(order);
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
                rentalPricePerDay = product.RentalFee,
                rentalDeposit = 0, // Default deposit value
                stock = product.Stock,
                canRent = product.RentalFee > 0
            }, JsonRequestBehavior.AllowGet);
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
}