using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }  // Price per unit

        // 🔹 Rental-specific fields
        public bool IsRental { get; set; }
        public DateTime? RentalStartDate { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public decimal? RentalFeePerDay { get; set; }
        public decimal? Deposit { get; set; }
    }


}