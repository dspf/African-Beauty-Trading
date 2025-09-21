using African_Beauty_Trading.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Services
{
    public class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Traditional Attire" },
                    new Category { Name = "Modern African Wear" },
                    new Category { Name = "Ankara Prints" },
                    new Category { Name = "Dashikis" },
                    new Category { Name = "Kente Clothing" },
                    new Category { Name = "African Dresses" },
                    new Category { Name = "African Shirts" },
                    new Category { Name = "Headwraps & Gele" },
                    new Category { Name = "African Accessories" },
                    new Category { Name = "African Footwear" },
                    new Category { Name = "Beadwork & Jewelry" },
                    new Category { Name = "African Children's Wear" }
                };
                context.Categories.AddRange(categories);
            }

            // Seed Departments
            if (!context.Departments.Any())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Women's Fashion" },
                    new Department { Name = "Men's Fashion" },
                    new Department { Name = "Children's Fashion" },
                    new Department { Name = "Unisex Collection" },
                    new Department { Name = "Traditional Wear" },
                    new Department { Name = "Contemporary African" },
                    new Department { Name = "Wedding & Special Occasion" },
                    new Department { Name = "Casual Wear" },
                    new Department { Name = "Formal African Wear" },
                    new Department { Name = "Accessories Department" }
                };
                context.Departments.AddRange(departments);
            }

            context.SaveChanges();
        }
    }
}