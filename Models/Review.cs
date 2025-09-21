using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        public int Rating { get; set; }  // 1-5 stars
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

}