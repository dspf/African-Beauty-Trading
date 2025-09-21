using African_Beauty_Trading.Models;
using African_Beauty_Trading.Services;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private OtpService otpService = new OtpService();

        // Dashboard - Show pending assignments, active deliveries, and notifications
        public ActionResult Dashboard()
        {
            string driverId = User.Identity.GetUserId();

            // PENDING assignments - paid rental orders not yet accepted and not delivered/returned
            var pendingAssignments = db.DriverAssignments
                .Include(a => a.Order)
                .Include(a => a.Order.Customer)
                .Include(a => a.Order.OrderItems)
                .Where(a => a.Status == "Pending"
                    && a.DriverId == null
                    && a.Order.PaymentStatus == "Paid"
                    && a.Order.OrderType == "Rent"
                    && a.Order.DeliveryStatus != "Delivered"
                    && a.Order.DeliveryStatus != "Returned"
                    && a.ExpiryTime > DateTime.UtcNow)
                .ToList();

            // ACTIVE deliveries - accepted rental orders not completed/returned
            var activeDeliveries = db.DriverAssignments
                .Include(a => a.Order)
                .Include(a => a.Order.Customer)
                .Where(a => a.DriverId == driverId
                    && (a.Status == "Accepted" || a.Status == "ReturnAccepted")
                    && a.Order.OrderType == "Rent"
                    && a.Order.DeliveryStatus != "Delivered"
                    && a.Order.DeliveryStatus != "Returned")
                .Select(a => a.Order)
                .ToList();

            // COMPLETED deliveries - delivered/completed orders (rental or purchase)
            var completedDeliveries = db.DriverAssignments
                .Include(a => a.Order)
                .Include(a => a.Order.Customer)
                .Where(a => a.DriverId == driverId
                    && a.Status == "Completed"
                    && (a.Order.DeliveryStatus == "Delivered" || a.Order.DeliveryStatus == "Returned"))
                .Select(a => a.Order)
                .ToList();

            // RETURN assignments - rental orders that require pickup (not pending or active)
            var returnAssignments = db.DriverAssignments
                .Include(a => a.Order)
                .Include(a => a.Order.Customer)
                .Where(a => a.Status == "Pending"
                    && a.DriverId == null
                    && a.Order.OrderType == "Rent"
                    && a.Order.RentEndDate.HasValue
                    && a.Order.DeliveryStatus == "Delivered"
                    && a.ExpiryTime > DateTime.UtcNow)
                .ToList();

            // Counts
            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.ActiveDeliveries = activeDeliveries;
            ViewBag.CompletedDeliveries = completedDeliveries;
            ViewBag.ReturnAssignments = returnAssignments;

            ViewBag.PendingCount = pendingAssignments.Count;
            ViewBag.ActiveCount = activeDeliveries.Count;
            ViewBag.CompletedCount = completedDeliveries.Count;
            ViewBag.ReturnCount = returnAssignments.Count;

            // Earnings
            const decimal deliveryFee = 30.00m;
            int completedCount = completedDeliveries.Count;
            decimal totalEarnings = completedCount * deliveryFee;
            int thisMonthCount = completedDeliveries
                .Count(o => o.DeliveryDate.HasValue && o.DeliveryDate.Value.Month == DateTime.Now.Month);
            decimal thisMonthEarnings = thisMonthCount * deliveryFee;
            ViewBag.TotalEarnings = totalEarnings;
            ViewBag.ThisMonthEarnings = thisMonthEarnings;
            ViewBag.CompletedDeliveriesCount = completedCount;
            ViewBag.DeliveryFee = deliveryFee;

            return View();
        }



        // View and respond to a specific assignment
        public ActionResult ViewAssignment(int assignmentId)
        {
            string driverId = User.Identity.GetUserId();

            var assignment = db.DriverAssignments
                .Include("Order")
                .Include("Order.Customer")
                .Include("Order.OrderItems")
                .Include("Order.OrderItems.Product")
                .FirstOrDefault(a => a.Id == assignmentId && 
                    (a.DriverId == driverId || (a.DriverId == null && a.Status == "Pending")));

            if (assignment == null)
            {
                CreateNotification(driverId, "Error", "Assignment not found or you don't have permission to view it.", "error");
                return RedirectToAction("Dashboard");
            }

            if (assignment.Status != "Pending")
            {
                CreateNotification(driverId, "Info", "This assignment has already been processed.", "info");
            }

            if (assignment.ExpiryTime < DateTime.Now)
            {
                CreateNotification(driverId, "Warning", "This assignment has expired.", "warning");
            }

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AcceptAssignment(int assignmentId)
        {
            string driverId = User.Identity.GetUserId();

            var assignment = db.DriverAssignments
                .Include("Order")
                .FirstOrDefault(a => a.Id == assignmentId && a.Status == "Pending" && a.DriverId == null);

            if (assignment == null)
            {
                CreateNotification(driverId, "Error", "Assignment not found or already taken.", "error");
                return RedirectToAction("Dashboard");
            }

            if (assignment.ExpiryTime < DateTime.UtcNow)
            {
                CreateNotification(driverId, "Error", "This assignment has expired.", "error");
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Mark assignment as accepted
                assignment.Status = "Accepted";
                assignment.DriverId = driverId;
                assignment.ResponseDate = DateTime.UtcNow;

                // Update the order
                assignment.Order.DriverId = driverId;
                assignment.Order.DriverAccepted = true;
                assignment.Order.DriverAssignedDate = DateTime.UtcNow;

                // ✅ Canonical delivery status
                assignment.Order.DeliveryStatus = "Accepted";

                // Generate OTP
                string otp = otpService.GenerateOtp();
                assignment.Order.DeliveryOtp = otp;
                assignment.Order.OtpGeneratedAt = DateTime.UtcNow;

                db.SaveChanges();

                CreateNotification(driverId, "Assignment Accepted",
                    $"You accepted Order #{assignment.OrderId}. OTP: {otp}", "success");

                CreateAdminNotification(
                    $"Driver {User.Identity.Name} accepted Order #{assignment.OrderId}",
                    "Driver Assignment Accepted",
                    "success",
                    assignment.OrderId
                );

                return RedirectToAction("DeliveryDetails", new { id = assignment.OrderId });
            }
            catch (Exception ex)
            {
                CreateNotification(driverId, "Error", "Error accepting: " + ex.Message, "error");
                return RedirectToAction("Dashboard");
            }
        }




        // Decline an assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeclineAssignment(int assignmentId, string declineReason)
        {
            string driverId = User.Identity.GetUserId();

            var assignment = db.DriverAssignments
                .Include("Order")
                .FirstOrDefault(a => a.Id == assignmentId && a.Status == "Pending" && a.DriverId == null);


            if (assignment == null || assignment.Status != "Pending")
            {
                CreateNotification(driverId, "Error", "Assignment not found or already processed.", "error");
                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrEmpty(declineReason))
            {
                CreateNotification(driverId, "Error", "Please provide a reason for declining.", "error");
                return RedirectToAction("ViewAssignment", new { assignmentId });
            }

            try
            {
                assignment.DriverId = driverId; // mark who declined
                assignment.Status = "Declined";
                assignment.ResponseDate = DateTime.Now;
                assignment.DeclineReason = declineReason;

                // Reset order
                assignment.Order.DriverId = null;
                assignment.Order.DeliveryStatus = "Unassigned";
                assignment.Order.DriverAccepted = false;
                assignment.Order.DeclineReason = declineReason;

                db.SaveChanges();

                CreateNotification(driverId, "Assignment Declined",
                    $"Assignment for Order #{assignment.OrderId} declined successfully.", "info");

                CreateAdminNotification(
                    $"Driver {User.Identity.Name} declined assignment for Order #{assignment.OrderId}. Reason: {declineReason}",
                    "Driver Assignment Declined", "warning");

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                CreateNotification(driverId, "Error", $"Error declining assignment: {ex.Message}", "error");
                return RedirectToAction("Dashboard");
            }
        }


        // Delivery Details
        public ActionResult DeliveryDetails(int id)
        {
            string driverId = User.Identity.GetUserId();

            var order = db.Orders
                .Include("Customer")
                .Include("OrderItems")
                .Include("OrderItems.Product")
                .FirstOrDefault(o => o.Id == id && o.DriverId == driverId);

            if (order == null)
            {
                CreateNotification(driverId, "Error", "Order not found or you are not assigned to this delivery.", "error");
                return RedirectToAction("Dashboard");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsArrived(int orderId)
        {
            string driverId = User.Identity.GetUserId();

            var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.DriverId == driverId);
            if (order == null)
            {
                CreateNotification(driverId, "Error", "Order not found.", "error");
                return RedirectToAction("Dashboard");
            }

            order.DeliveryStatus = "Arrived";
            order.DriverArrivedDate = DateTime.Now; // <-- NEW FIELD
            db.SaveChanges();

            CreateNotification(driverId, "Arrival Notification", "Customer has been notified of your arrival!", "success");
            CreateAdminNotification($"Driver {User.Identity.Name} has arrived at delivery location for Order #{orderId}", "Driver Arrival", "info");

            return RedirectToAction("DeliveryDetails", new { id = orderId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteDelivery(int orderId, string otp)
        {
            string driverId = User.Identity.GetUserId();

            var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.DriverId == driverId);
            if (order == null)
            {
                CreateNotification(driverId, "Error", "Order not found or not assigned to you.", "error");
                TempData["DriverActionError"] = "Order not found or not assigned to you.";
                return RedirectToAction("Dashboard");
            }

            // Guard: OTP must exist
            if (!order.OtpGeneratedAt.HasValue || string.IsNullOrWhiteSpace(order.DeliveryOtp))
            {
                CreateNotification(driverId, "Error", "OTP not generated for this order yet.", "error");
                TempData["DriverActionError"] = "OTP not generated for this order yet.";
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            otp = (otp ?? "").Trim();

            bool valid;
            try
            {
                valid = otpService.IsOtpValid(order.DeliveryOtp.Trim(), otp, order.OtpGeneratedAt.Value);
            }
            catch (Exception ex)
            {
                CreateNotification(driverId, "Error", "OTP validation error: " + ex.Message, "error");
                TempData["DriverActionError"] = "OTP validation error: " + ex.Message;
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            if (!valid)
            {
                CreateNotification(driverId, "Invalid OTP", "Invalid or expired OTP.", "error");
                TempData["DriverActionError"] = "Invalid or expired OTP.";
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            var now = DateTime.UtcNow;

            if (order.OrderType == "Rent")
            {
                // Phase 1: rental delivered
                order.DeliveryStatus = "Delivered"; // ✅ unify status
                order.DeliveryDate = now;

                // Create a return assignment if not already present
                if (order.RentEndDate.HasValue &&
                    !db.DriverAssignments.Any(a => a.OrderId == order.Id && a.Status == "Pending"))
                {
                    var returnAssignment = new DriverAssignment
                    {
                        OrderId = order.Id,
                        DriverId = null,
                        Status = "Pending",
                        AssignedDate = now,
                        // ✅ use RentEndDate to trigger pickup on correct day
                        ExpiryTime = order.RentEndDate.Value,
                        CreatedDate = now
                    };
                    db.DriverAssignments.Add(returnAssignment);
                }
            }
            else
            {
                // Normal buy order
                order.DeliveryStatus = "Delivered";
                order.DeliveryDate = now;
            }

            // Mark current assignment complete
            var assignment = db.DriverAssignments
                .FirstOrDefault(a => a.OrderId == order.Id && a.DriverId == driverId);
            if (assignment != null)
            {
                assignment.Status = "Completed";
                assignment.ResponseDate = now;
            }

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                CreateNotification(driverId, "Error", "Could not complete delivery: " + ex.Message, "error");
                TempData["DriverActionError"] = "DB save failed: " + ex.Message;
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            CreateNotification(driverId, "Delivery Completed", "Delivery completed successfully!", "success");
            CreateAdminNotification($"Driver {User.Identity.Name} completed delivery for Order #{orderId}",
                "Delivery Completed", "success", order.Id);

            TempData["DriverActionSuccess"] = "Delivery completed successfully.";
            return RedirectToAction("Dashboard");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteReturn(int orderId, string otp)
        {
            string driverId = User.Identity.GetUserId();

            var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.DriverId == driverId);
            if (order == null)
            {
                CreateNotification(driverId, "Error", "Order not found or not assigned to you.", "error");
                return RedirectToAction("Dashboard");
            }

            // Validate OTP
            if (!order.OtpGeneratedAt.HasValue || string.IsNullOrWhiteSpace(order.DeliveryOtp))
            {
                CreateNotification(driverId, "Error", "OTP not generated for this return yet.", "error");
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            otp = (otp ?? "").Trim();
            bool valid = otpService.IsOtpValid(order.DeliveryOtp.Trim(), otp, order.OtpGeneratedAt.Value);

            if (!valid)
            {
                CreateNotification(driverId, "Invalid OTP", "Invalid or expired OTP.", "error");
                return RedirectToAction("DeliveryDetails", new { id = orderId });
            }

            // Complete the return
            order.DeliveryStatus = "Returned";
            order.ReturnDate = DateTime.UtcNow;

            db.SaveChanges();

            CreateNotification(driverId, "Return Completed", "Return completed successfully!", "success");
            CreateAdminNotification(
                $"Driver {User.Identity.Name} completed return for Order #{orderId}",
                "Return Completed",
                "success",
                order.Id
            );

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AcceptReturnAssignment(int assignmentId)
        {
            string driverId = User.Identity.GetUserId();

            var assignment = db.DriverAssignments
                .Include("Order")
                .FirstOrDefault(a => a.Id == assignmentId && a.Status == "Pending" && a.DriverId == null);

            if (assignment == null)
            {
                CreateNotification(driverId, "Error", "Return assignment not found or already taken.", "error");
                return RedirectToAction("Dashboard");
            }

            if (assignment.ExpiryTime < DateTime.UtcNow)
            {
                CreateNotification(driverId, "Error", "This return assignment has expired.", "error");
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Mark assignment as accepted for RETURN
                assignment.Status = "ReturnAccepted";
                assignment.DriverId = driverId;
                assignment.ResponseDate = DateTime.UtcNow;

                // Update order to reflect return pickup
                assignment.Order.DriverId = driverId;
                assignment.Order.DeliveryStatus = "ReturnPickupScheduled";
                assignment.Order.DriverAccepted = true;
                assignment.Order.DriverAssignedDate = DateTime.UtcNow;

                db.SaveChanges();

                CreateNotification(driverId, "Return Assignment Accepted",
                    $"You accepted return pickup for Order #{assignment.OrderId}.", "success");

                CreateAdminNotification(
                    $"Driver {User.Identity.Name} accepted return pickup for Order #{assignment.OrderId}",
                    "Return Assignment Accepted",
                    "success"
                );

                return RedirectToAction("DeliveryDetails", new { id = assignment.OrderId });
            }
            catch (Exception ex)
            {
                CreateNotification(driverId, "Error", "Error accepting return assignment: " + ex.Message, "error");
                return RedirectToAction("Dashboard");
            }
        }


        // Notification Management
        public ActionResult Notifications()
        {
            string driverId = User.Identity.GetUserId();

            var notifications = db.DriverNotifications
                .Where(n => n.DriverId == driverId)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();

            return View(notifications);
        }

        [HttpPost]
        public JsonResult MarkNotificationAsRead(int notificationId)
        {
            string driverId = User.Identity.GetUserId();

            var notification = db.DriverNotifications
                .FirstOrDefault(n => n.Id == notificationId && n.DriverId == driverId);

            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        public ActionResult Earnings()
        {
            string driverId = User.Identity.GetUserId();

            var completedOrders = db.Orders
                .Include(o => o.Customer)
                .Where(o => o.DriverId == driverId && o.DeliveryStatus == "Delivered" && o.PaymentStatus == "Paid")
                .OrderByDescending(o => o.DeliveryDate)
                .ToList();

            const decimal deliveryFee = 30.00m;
            int completedCount = completedOrders.Count;
            decimal totalEarnings = completedCount * deliveryFee;

            int thisMonthCount = completedOrders
                .Count(o => o.DeliveryDate.HasValue && o.DeliveryDate.Value.Month == DateTime.Now.Month);
            decimal thisMonthEarnings = thisMonthCount * deliveryFee;

            ViewBag.TotalEarnings = totalEarnings;
            ViewBag.ThisMonthEarnings = thisMonthEarnings;
            ViewBag.CompletedDeliveriesCount = completedCount;
            ViewBag.DeliveryFee = deliveryFee;

            return View(completedOrders);
        }

        [HttpPost]
        public JsonResult MarkAllNotificationsAsRead()
        {
            string driverId = User.Identity.GetUserId();

            var unreadNotifications = db.DriverNotifications
                .Where(n => n.DriverId == driverId && !n.IsRead)
                .ToList();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }

            db.SaveChanges();
            return Json(new { success = true, count = unreadNotifications.Count });
        }

        // Helper Methods
        private void CreateNotification(string driverId, string title, string message, string type)
        {
            var notification = new DriverNotification
            {
                DriverId = driverId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            db.DriverNotifications.Add(notification);
            db.SaveChanges();
        }

        private void CreateAdminNotification(string message, string title, string type, int? orderId = null)
        {
            // ✅ Fetch Admin role Id first
            var adminRoleId = db.Roles
                .Where(ro => ro.Name == "Admin")
                .Select(ro => ro.Id)
                .FirstOrDefault();

            if (adminRoleId == null)
                return; // No Admin role defined in DB

            // ✅ Get all admin users
            var adminUsers = db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == adminRoleId))
                .ToList();

            foreach (var admin in adminUsers)
            {
                var notification = new AdminNotification
                {
                    AdminId = admin.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    RelatedEntity = "Order",     // tells frontend what entity this relates to
                    RelatedEntityId = orderId    // link directly to order if provided
                };

                db.AdminNotifications.Add(notification);
            }

            db.SaveChanges();
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