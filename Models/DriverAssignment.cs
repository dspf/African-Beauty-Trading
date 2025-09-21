using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class DriverAssignment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string DriverId { get; set; }
        public virtual ApplicationUser Driver { get; set; }

        public string AssignedBy { get; set; }
        public virtual ApplicationUser AssignedByUser { get; set; }

        public DateTime AssignedDate { get; set; }   // keep required
        public DateTime? ResponseDate { get; set; }
        public DateTime ExpiryTime { get; set; }

        public string Status { get; set; }
        public string DeclineReason { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}