using African_Beauty_Trading.Models;
using African_Beauty_Trading.Hubs;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace African_Beauty_Trading.Controllers
{
    [System.Web.Mvc.Authorize]
    public class CustomerChatController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var userName = User.Identity.Name;

            // Check if user exists in AspNetUsers table
            var userExists = db.Users.Any(u => u.Id == userId);
            if (!userExists)
            {
                TempData["ErrorMessage"] = "User account not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var chatRoom = db.ChatRooms
                .Include("Admin")
                .Include("Messages")
                .FirstOrDefault(c => c.CustomerId == userId && c.IsActive);

            if (chatRoom == null)
            {
                // Create new chat room if none exists
                chatRoom = new ChatRoom
                {
                    CustomerId = userId,
                    CustomerName = userName,
                    CreatedAt = DateTime.Now,
                    LastActivity = DateTime.Now
                };
                db.ChatRooms.Add(chatRoom);
                db.SaveChanges();
            }

            return View(chatRoom);
        }

        [HttpPost]
        public JsonResult SendMessage(int roomId, string message)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var userName = User.Identity.Name;

                var chatMessage = new ChatMessage
                {
                    ChatRoomId = roomId,
                    SenderId = userId,
                    SenderName = userName,
                    Message = message,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                db.ChatMessages.Add(chatMessage);

                var room = db.ChatRooms.Find(roomId);
                if (room != null)
                {
                    room.LastActivity = DateTime.Now;
                    db.SaveChanges();

                    // Send real-time message via SignalR
                    var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                    
                    // Send to the specific chat room
                    hubContext.Clients.Group(roomId.ToString()).receiveMessage(userName, message, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    
                    // Send to all admins
                    hubContext.Clients.Group("Admins").receiveMessage("Customer: " + userName, message, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    return Json(new { success = true });
                }

                return Json(new { success = false, error = "Chat room not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public JsonResult GetMessages(int roomId)
        {
            var userId = User.Identity.GetUserId();

            var messages = db.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    sender = m.SenderName,
                    message = m.Message,
                    time = m.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    isMe = m.SenderId == userId
                })
                .ToList();

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnreadCount()
        {
            var userId = User.Identity.GetUserId();
            var unreadCount = db.ChatRooms
                .Where(cr => cr.CustomerId == userId && cr.IsActive)
                .SelectMany(cr => cr.Messages)
                .Count(m => !m.IsRead && m.SenderId != userId);

            return Json(new { unreadCount = unreadCount }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null)
                {
                    db.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}