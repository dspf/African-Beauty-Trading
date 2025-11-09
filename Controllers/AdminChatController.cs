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
    [System.Web.Mvc.Authorize(Roles = "Admin")]
    public class AdminChatController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            try
            {
                var activeChatRooms = db.ChatRooms
                    .Include(cr => cr.Messages)
                    .Where(cr => cr.IsActive)
                    .OrderByDescending(cr => cr.LastActivity)
                    .ToList();

                ViewBag.ActiveChatCount = activeChatRooms.Count;
                return View(activeChatRooms);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading chats: {ex.Message}";
                return View(Enumerable.Empty<ChatRoom>().ToList());
            }
        }

        public ActionResult ChatRoom(int id)
        {
            try
            {
                var chatRoom = db.ChatRooms
                    .Include(cr => cr.Messages)
                    .FirstOrDefault(cr => cr.Id == id);

                if (chatRoom == null)
                {
                    TempData["ErrorMessage"] = "Chat room not found.";
                    return RedirectToAction("Index");
                }

                // Mark customer messages as read when admin opens the chat
                var userId = User.Identity.GetUserId();
                var unreadMessages = chatRoom.Messages
                    .Where(m => !m.IsRead && m.SenderId != userId)
                    .ToList();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                db.SaveChanges();

                // Set ViewBag values for JavaScript
                ViewBag.RoomId = chatRoom.Id;
                ViewBag.UserId = userId;
                ViewBag.UserName = User.Identity.Name;
                ViewBag.IsAdmin = true;
                ViewBag.CustomerId = chatRoom.CustomerId; // Important for group joining

                return View(chatRoom);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading chat room: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendMessage(int roomId, string message)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var userName = User.Identity.Name;

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, error = "Message cannot be empty" });
                }

                var chatMessage = new ChatMessage
                {
                    ChatRoomId = roomId,
                    SenderId = userId,
                    SenderName = userName,
                    Message = message.Trim(),
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

                    // Send to the room group (customer should be in this group)
                    hubContext.Clients.Group(roomId.ToString()).receiveMessage(userName, message, DateTime.Now.ToString("HH:mm"));

                    // ALSO send to customer's personal group (backup method)
                    hubContext.Clients.Group($"User_{room.CustomerId}").receiveMessage(userName, message, DateTime.Now.ToString("HH:mm"));

                    Console.WriteLine($"Admin message sent to room {roomId} and customer {room.CustomerId}");

                    return Json(new { success = true, messageId = chatMessage.Id });
                }

                return Json(new { success = false, error = "Chat room not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Rest of your methods remain the same...
        [HttpPost]
        public JsonResult MarkMessagesAsRead(int roomId)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var unreadMessages = db.ChatMessages
                    .Where(m => m.ChatRoomId == roomId && !m.IsRead && m.SenderId != userId)
                    .ToList();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                db.SaveChanges();

                return Json(new { success = true, markedCount = unreadMessages.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public JsonResult GetUnreadCount()
        {
            try
            {
                var unreadCount = db.ChatMessages
                    .Count(m => !m.IsRead && m.SenderId != User.Identity.GetUserId());

                return Json(new { unreadCount = unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { unreadCount = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}