using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        public string DriverId { get; set; }
        public virtual ApplicationUser Driver { get; set; }


        public string DeliveryStatus { get; set; }
        public string DeliveryOtp { get; set; }
        public string DeliveryAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime? OtpGeneratedAt { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool? DriverAccepted { get; set; }
        public string DeclineReason { get; set; }
        public int? DriverRating { get; set; }  // rating from 1–5
        public string DriverFeedback { get; set; } // optional comment

        public DateTime? OrderDate { get; set; }   // <-- nullable
        public decimal TotalPrice { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderType { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public Product Products { get; set; }

        public DateTime? AssignedDate { get; set; }
        public DateTime? DriverAssignedDate { get; set; }
        public DateTime? DriverArrivedDate { get; set; }

        // In Order.DeliveryStatus
        // For rentals we’ll use these too:
        public string RentalStatus { get; set; }
        public DateTime? RentStartDate { get; set; }
        public DateTime? RentEndDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        // Priority field for driver assignment flags
        public string Priority { get; set; } // "Urgent" for agent-assigned (red flag), "Normal" for customer-selected (green flag)

        // Values: "Pending", "OutForDelivery", "Rented", "AwaitingReturn", "Returned", "Completed"

    }


}