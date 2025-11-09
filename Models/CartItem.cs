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
        
        public int Quantity { get; set; }
        // Adding rental properties back to fix compilation errors
       
        public decimal? Deposit { get; set; }
        public string ImagePath { get; set; }
    }

}