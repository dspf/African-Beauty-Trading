using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }       // S, M, L, XL or Kids Age
        public decimal Price { get; set; }     // Buy Price
        public decimal RentalFee { get; set; } // Rental cost
        public string ProductType { get; set; } // "Buy", "Rent", or "Both"
        public int Stock { get; set; }
        public string ImagePath { get; set; }  // File path for product photo
        public string AgeGroup { get; set; } // For kids
        [StringLength(500)]
        public string Description { get; set; }
        public string EthnicGroup { get; set; } // e.g., Zulu, Xhosa, Sotho, etc.
        public string Occasion { get; set; } // e.g., Wedding, Initiation, Festival// e.g. Men, Women, Kids
        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }

}