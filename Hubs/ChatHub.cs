using African_Beauty_Trading.Models;
using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;

namespace African_Beauty_Trading.Hubs
{
    public class ChatHub : Hub
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public override Task OnConnected()
        {
            Console.WriteLine($"=== CLIENT CONNECTED: {Context.ConnectionId} ===");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine($"=== CLIENT DISCONNECTED: {Context.ConnectionId} ===");
            return base.OnDisconnected(stopCalled);
        }

        public async Task JoinRoom(int roomId)
        {
            try
            {
                Console.WriteLine($"Joining room: {roomId} for connection: {Context.ConnectionId}");

                // Join the chat room group
                await Groups.Add(Context.ConnectionId, roomId.ToString());

                // Get room info and join customer's personal group
                var room = db.ChatRooms.Find(roomId);
                if (room != null)
                {
                    await Groups.Add(Context.ConnectionId, $"User_{room.CustomerId}");
                    Console.WriteLine($"Also joined personal group: User_{room.CustomerId}");
                }

                await Clients.Caller.roomJoined(roomId);
                Console.WriteLine($"Successfully joined room: {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining room: {ex}");
                await Clients.Caller.showError($"Error joining room: {ex.Message}");
            }
        }

        public async Task JoinAdminGroup()
        {
            try
            {
                Console.WriteLine($"Joining admin group for connection: {Context.ConnectionId}");
                await Groups.Add(Context.ConnectionId, "Admins");
                await Clients.Caller.adminJoined();
                Console.WriteLine($"Successfully joined admin group");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining admin group: {ex}");
                await Clients.Caller.showError($"Error joining admin group: {ex.Message}");
            }
        }

        public async Task JoinUserGroup(string userId)
        {
            try
            {
                Console.WriteLine($"Joining user group for: {userId}, connection: {Context.ConnectionId}");
                await Groups.Add(Context.ConnectionId, $"User_{userId}");
                await Clients.Caller.userGroupJoined();
                Console.WriteLine($"Successfully joined user group: User_{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining user group: {ex}");
                await Clients.Caller.showError($"Error joining user group: {ex.Message}");
            }
        }

        public async Task SendMessage(int roomId, string message, string senderName, string senderId, bool isAdmin = false)
        {
            try
            {
                Console.WriteLine($"Sending message to room {roomId} from {(isAdmin ? "Admin" : "Customer")}: {message}");
                Console.WriteLine($"Sender: {senderName} ({senderId}), IsAdmin: {isAdmin}");

                // Save to database
                var chatMessage = new ChatMessage
                {
                    ChatRoomId = roomId,
                    SenderId = senderId,
                    SenderName = senderName,
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

                    // Broadcast to room group (both admin and customer will receive if they joined the room)
                    await Clients.Group(roomId.ToString()).receiveMessage(senderName, message, DateTime.Now.ToString("HH:mm"));
                    Console.WriteLine($"Message broadcast to room group: {roomId}");

                    if (isAdmin)
                    {
                        // Admin sent message - also send to customer's personal group
                        await Clients.Group($"User_{room.CustomerId}").receiveMessage(senderName, message, DateTime.Now.ToString("HH:mm"));
                        Console.WriteLine($"Message also sent to customer's personal group: User_{room.CustomerId}");
                    }
                    else
                    {
                        // Customer sent message - notify all admins
                        await Clients.Group("Admins").receiveMessage("Customer: " + senderName, message, DateTime.Now.ToString("HH:mm"));
                        Console.WriteLine("Notification sent to Admins group");
                    }

                    await Clients.Caller.messageSent();
                }
                else
                {
                    await Clients.Caller.showError("Chat room not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex}");
                await Clients.Caller.showError($"Error: {ex.Message}");
            }
        }

        // Test method
        public async Task TestConnection()
        {
            await Clients.Caller.testResponse($"Hub is working! Connection ID: {Context.ConnectionId}");
        }

        protected override void Dispose(bool disposing)
        {
            db?.Dispose();
            base.Dispose(disposing);
        }
    }
}