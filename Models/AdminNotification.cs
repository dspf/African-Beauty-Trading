using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class AdminNotification
    {
        public int Id { get; set; }
        public string AdminId { get; set; }
        public virtual ApplicationUser Admin { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // success, error, warning, info

        public string RelatedEntity { get; set; } // Order, Driver, etc.
        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadDate { get; set; }

    }
}