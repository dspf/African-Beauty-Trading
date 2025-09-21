using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class DriverResponse
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string DriverId { get; set; }
        public DateTime ResponseDate { get; set; }
        public bool Accepted { get; set; }
        public string Reason { get; set; }
    }
}