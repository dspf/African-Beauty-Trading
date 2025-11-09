using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        public string DeliveryAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public DateTime? OrderDate { get; set; }   // <-- nullable
        public decimal TotalPrice { get; set; }
        public string PaymentStatus { get; set; }
        

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public Product Products { get; set; }

        // Courier information (PEP)
        public string CourierName { get; set; } = "PEP";
        public string CourierTrackingNumber { get; set; }
        public string CourierStatus { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        // Driver information (optional)



        // Priority field for shipping
        [Required]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Normal"; // Default value

        // Delivery status tracking
        public string DeliveryStatus { get; set; } // Values: "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
        public string DeliveryOtp { get; set; } // OTP for delivery verification
        public DateTime? OtpGeneratedAt { get; set; } // When the OTP was generated
    }
}
