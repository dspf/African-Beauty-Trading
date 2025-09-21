using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class DriverNotification
    {
        public int Id { get; set; }
        public string DriverId { get; set; }
        public virtual ApplicationUser Driver { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // success, error, warning, info

        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}