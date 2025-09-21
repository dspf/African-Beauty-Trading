using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Delivery
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string DriverId { get; set; }
        public virtual ApplicationUser Driver { get; set; }

        public string Address { get; set; }
        public string DeliveryType { get; set; }  // Standard, Same Day
        public string Status { get; set; }        // Pending, In Progress, Completed
        public DateTime DeliveryDate { get; set; }
    }

}