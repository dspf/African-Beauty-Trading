using System;
using System.ComponentModel.DataAnnotations;

namespace African_Beauty_Trading.ViewModels
{
    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        public bool IsRental { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime RentalStartDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime RentalEndDate { get; set; }
    }
}