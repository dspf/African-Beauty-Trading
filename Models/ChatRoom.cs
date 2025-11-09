using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    // Add to your Models folder
    public class ChatRoom
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } // User ID of the customer
        public string CustomerName { get; set; }
        public string AdminId { get; set; } // User ID of the assigned admin (nullable)
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ApplicationUser Customer { get; set; }
        public virtual ApplicationUser Admin { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ChatRoom ChatRoom { get; set; }
        public virtual ApplicationUser Sender { get; set; }
    }
}