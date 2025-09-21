using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Rental
    {
        public int Id { get; set; }

        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public DateTime EventDate { get; set; }   // When customer needs the attire
        public DateTime ReturnDate { get; set; }  // When item should be returned
        public decimal Deposit { get; set; }
        public string Status { get; set; }        // Booked, Returned, Late
    }

}