using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal RentalFee { get; set; }
        public int Quantity { get; set; }
        public bool IsRental { get; set; }
        public string ImagePath { get; set; }
        public int? Deposit { get; internal set; }
    }

}