using African_Beauty_Trading.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading.ViewModels
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Departments { get; set; }
        public SelectList Sizes { get; set; }
        public SelectList AgeGroups { get; set; }
    }
}