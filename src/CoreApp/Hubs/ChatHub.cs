#if NET7_0
using African_Beauty_Trading.CoreApp;
using African_Beauty_Trading.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace African_Beauty_Trading.CoreApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;

        public ChatHub(ApplicationDbContext db)
        {
            _db = db;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"=== CLIENT CONNECTED: {Context.ConnectionId} ===");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"=== CLIENT DISCONNECTED: {Context.ConnectionId} ===");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(int roomId)
        {
            try
            {
                Console.WriteLine($"Joining room: {roomId} for connection: {Context.ConnectionId}");

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

                var room = await _db.ChatRooms.FindAsync(roomId);
                if (room != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{room.CustomerId}");
                }

                await Clients.Caller.SendAsync("roomJoined", roomId);
                Console.WriteLine($"Successfully joined room: {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining room: {ex}");
                await Clients.Caller.SendAsync("showError", $"Error joining room: {ex.Message}");
            }
        }

        public async Task JoinAdminGroup()
        {
            try
            {
                Console.WriteLine($"Joining admin group for connection: {Context.ConnectionId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                await Clients.Caller.SendAsync("adminJoined");
                Console.WriteLine($"Successfully joined admin group");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining admin group: {ex}");
                await Clients.Caller.SendAsync("showError", $"Error joining admin group: {ex.Message}");
            }
        }

        public async Task JoinUserGroup(string userId)
        {
            try
            {
                Console.WriteLine($"Joining user group for: {userId}, connection: {Context.ConnectionId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                await Clients.Caller.SendAsync("userGroupJoined");
                Console.WriteLine($"Successfully joined user group: User_{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining user group: {ex}");
                await Clients.Caller.SendAsync("showError", $"Error joining user group: {ex.Message}");
            }
        }

        public async Task SendMessage(int roomId, string message, string senderName, string senderId, bool isAdmin = false)
        {
            try
            {
                Console.WriteLine($"Sending message to room {roomId} from {(isAdmin ? "Admin" : "Customer")}: {message}");

                var chatMessage = new ChatMessage
                {
                    ChatRoomId = roomId,
                    SenderId = senderId,
                    SenderName = senderName,
                    Message = message,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                await _db.ChatMessages.AddAsync(chatMessage);

                var room = await _db.ChatRooms.FindAsync(roomId);
                if (room != null)
                {
                    room.LastActivity = DateTime.Now;
                    await _db.SaveChangesAsync();

                    await Clients.Group(roomId.ToString()).SendAsync("receiveMessage", senderName, message, DateTime.Now.ToString("HH:mm"));

                    if (isAdmin)
                    {
                        await Clients.Group($"User_{room.CustomerId}").SendAsync("receiveMessage", senderName, message, DateTime.Now.ToString("HH:mm"));
                    }
                    else
                    {
                        await Clients.Group("Admins").SendAsync("receiveMessage", "Customer: " + senderName, message, DateTime.Now.ToString("HH:mm"));
                    }

                    await Clients.Caller.SendAsync("messageSent");
                }
                else
                {
                    await Clients.Caller.SendAsync("showError", "Chat room not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex}");
                await Clients.Caller.SendAsync("showError", $"Error: {ex.Message}");
            }
        }

        public async Task TestConnection()
        {
            await Clients.Caller.SendAsync("testResponse", $"Hub is working! Connection ID: {Context.ConnectionId}");
        }
    }
}
#endif
