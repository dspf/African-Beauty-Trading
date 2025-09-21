using African_Beauty_Trading.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Browse all products
        [AllowAnonymous] // Allow browsing without login
        public ActionResult Browse(int? categoryId, int? departmentId)
        {
            var products = db.Products.Include("Category").Include("Department").AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            if (departmentId.HasValue)
                products = products.Where(p => p.DepartmentId == departmentId.Value);

            return View(products.ToList());
        }

        // Product details
        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            var product = db.Products.Include("Category").Include("Department")
                                     .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // Dashboard: Show all orders for logged-in customer
        public ActionResult Dashboard()
        {
            var userId = User.Identity.GetUserId();
            var orders = db.Orders
                           .Include("OrderItems.Product")
                           .Include("Driver")
                           .Where(o => o.CustomerId == userId)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }

        // Track specific order
        public ActionResult Track(int id)
        {
            var userId = User.Identity.GetUserId();
            var order = db.Orders
                          .Include("Driver")
                          .FirstOrDefault(o => o.Id == id && o.CustomerId == userId);

            if (order == null) return HttpNotFound();

            return View(order);
        }
    }
}