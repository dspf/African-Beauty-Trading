
namespace African_Beauty_Trading.Migrations
{
    using African_Beauty_Trading.Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<African_Beauty_Trading.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(African_Beauty_Trading.Models.ApplicationDbContext context)
        {
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
                context.Categories.AddOrUpdate(c => c.Name, categories.ToArray());
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
                context.Departments.AddOrUpdate(d => d.Name, departments.ToArray());
            }

            // Save changes
            context.SaveChanges();
        }
    }
}
