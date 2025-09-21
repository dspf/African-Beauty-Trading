using African_Beauty_Trading.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using African_Beauty_Trading.ViewModels;
using Microsoft.AspNet.Identity;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;



namespace African_Beauty_Trading.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public static class SizeHelper
        {
            public static Dictionary<string, string> GetAdultSizeChart()
            {
                return new Dictionary<string, string>
        {
            {"XS", "Chest: 32-34\", Waist: 26-28\""},
            {"S", "Chest: 35-37\", Waist: 29-31\""},
            // Add other sizes
        };
            }

            public static Dictionary<string, string> GetKidsSizeChart()
            {
                return new Dictionary<string, string>
        {
            {"0-6 months", "Height: 24-27\", Weight: 12-17 lbs"},
            {"6-12 months", "Height: 27-30\", Weight: 17-22 lbs"},
            // Add other age groups
        };
            }
        }

        // GET: Products
        public ActionResult Index(string genderFilter = "", string categoryFilter = "", string sizeFilter = "", string searchString = "")
        {
            IQueryable<Product> products = db.Products
                .Include(p => p.Category)
                .Include(p => p.Department);

            // Apply filters if specified
            if (!string.IsNullOrEmpty(genderFilter))
            {
                products = products.Where(p => p.Department.Name == genderFilter);
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                products = products.Where(p => p.Category.Name == categoryFilter);
            }

            if (!string.IsNullOrEmpty(sizeFilter))
            {
                products = products.Where(p => p.Size == sizeFilter || p.AgeGroup == sizeFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) ||
                                           p.Description.Contains(searchString) ||
                                           p.EthnicGroup.Contains(searchString) ||
                                           p.Occasion.Contains(searchString));
            }

            // Populate filter dropdowns
            ViewBag.GenderFilters = new SelectList(db.Departments, "Name", "Name", genderFilter);
            ViewBag.CategoryFilters = new SelectList(db.Categories, "Name", "Name", categoryFilter);

            // Combine size options for adults and kids
            var sizeOptions = GetSizeOptions().Select(s => s.Value).ToList();
            sizeOptions.AddRange(GetAgeGroupOptions().Select(a => a.Value));
            ViewBag.SizeFilters = new SelectList(sizeOptions.Distinct(), sizeFilter);

            ViewBag.CurrentGenderFilter = genderFilter;
            ViewBag.CurrentCategoryFilter = categoryFilter;
            ViewBag.CurrentSizeFilter = sizeFilter;
            ViewBag.CurrentSearchString = searchString;

            return View(products.OrderBy(p => p.Name).ToList());
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products
                .Include(p => p.Category)
                .Include(p => p.Department)
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        // GET: Products/Create
        public ActionResult Create()
        {
            // Initialize new product with default values
            var product = new Product
            {
                DateCreated = DateTime.Now,
                LastUpdated = DateTime.Now,
                IsActive = true
            };

            // Populate ViewBag with dropdown options
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name");
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name");
            ViewBag.Sizes = new SelectList(GetSizeOptions(), "Value", "Text");
            ViewBag.AgeGroups = new SelectList(GetAgeGroupOptions(), "Value", "Text");

            return View(product);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product, HttpPostedFileBase ImageFile)
        {
            // Debug: Log the received data
            System.Diagnostics.Debug.WriteLine($"Product Name: {product.Name}");
            System.Diagnostics.Debug.WriteLine($"CategoryId: {product.CategoryId}");
            System.Diagnostics.Debug.WriteLine($"DepartmentId: {product.DepartmentId}");
            System.Diagnostics.Debug.WriteLine($"ProductType: {product.ProductType}");
            
            // Custom validation based on ProductType
            if (string.IsNullOrEmpty(product.ProductType))
            {
                ModelState.AddModelError("ProductType", "Please select a product type.");
            }
            else
            {
                if (product.ProductType == "Buy" || product.ProductType == "Both")
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price is required for products available for purchase.");
                    }
                }
                if (product.ProductType == "Rent" || product.ProductType == "Both")
                {
                    if (product.RentalFee <= 0)
                    {
                        ModelState.AddModelError("RentalFee", "Rental fee is required for products available for rent.");
                    }
                }
                
                // Set unused price fields to 0 based on ProductType
                if (product.ProductType == "Buy")
                {
                    product.RentalFee = 0;
                }
                else if (product.ProductType == "Rent")
                {
                    product.Price = 0;
                }
            }
            
            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error - {modelError.Key}: {error.ErrorMessage}");
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                    string extension = Path.GetExtension(ImageFile.FileName);
                    fileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    string path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);

                    if (!Directory.Exists(Server.MapPath("~/Uploads/")))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Uploads/"));
                    }

                    ImageFile.SaveAs(path);
                    product.ImagePath = "/Uploads/" + fileName;
                }

                // Set additional properties
                product.DateCreated = DateTime.Now;
                product.LastUpdated = DateTime.Now;

                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // If we got this far, something failed; redisplay form
            // Repopulate dropdowns
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", product.DepartmentId);
            ViewBag.Sizes = new SelectList(GetSizeOptions(), "Value", "Text", product.Size);
            ViewBag.AgeGroups = new SelectList(GetAgeGroupOptions(), "Value", "Text", product.AgeGroup);

            return View(product);
        }
        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Populate ViewBag with dropdown options
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", product.DepartmentId);
            ViewBag.Sizes = new SelectList(GetSizeOptions(), "Value", "Text", product.Size);
            ViewBag.AgeGroups = new SelectList(GetAgeGroupOptions(), "Value", "Text", product.AgeGroup);

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, HttpPostedFileBase ImageFile)
        {
            // Custom validation based on ProductType
            if (string.IsNullOrEmpty(product.ProductType))
            {
                ModelState.AddModelError("ProductType", "Please select a product type.");
            }
            else
            {
                if (product.ProductType == "Buy" || product.ProductType == "Both")
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price is required for products available for purchase.");
                    }
                }
                if (product.ProductType == "Rent" || product.ProductType == "Both")
                {
                    if (product.RentalFee <= 0)
                    {
                        ModelState.AddModelError("RentalFee", "Rental fee is required for products available for rent.");
                    }
                }
                
                // Set unused price fields to 0 based on ProductType
                if (product.ProductType == "Buy")
                {
                    product.RentalFee = 0;
                }
                else if (product.ProductType == "Rent")
                {
                    product.Price = 0;
                }
            }
            
            if (ModelState.IsValid)
            {
                // Handle image update
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImagePath))
                    {
                        string oldPath = Server.MapPath(product.ImagePath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Save new image
                    string fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                    string extension = Path.GetExtension(ImageFile.FileName);
                    fileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    string path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);

                    ImageFile.SaveAs(path);
                    product.ImagePath = "/Uploads/" + fileName;
                }

                // Update tracking
                product.LastUpdated = DateTime.Now;

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // If we got this far, something failed; redisplay form
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", product.DepartmentId);
            ViewBag.Sizes = new SelectList(GetSizeOptions(), "Value", "Text", product.Size);
            ViewBag.AgeGroups = new SelectList(GetAgeGroupOptions(), "Value", "Text", product.AgeGroup);

            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            // Delete associated image
            if (!string.IsNullOrEmpty(product.ImagePath))
            {
                string path = Server.MapPath(product.ImagePath);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Helper methods for dropdown options
        private List<SelectListItem> GetSizeOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "XS", Text = "Extra Small (XS)" },
                new SelectListItem { Value = "S", Text = "Small (S)" },
                new SelectListItem { Value = "M", Text = "Medium (M)" },
                new SelectListItem { Value = "L", Text = "Large (L)" },
                new SelectListItem { Value = "XL", Text = "Extra Large (XL)" },
                new SelectListItem { Value = "XXL", Text = "Double Extra Large (XXL)" },
                new SelectListItem { Value = "XXXL", Text = "Triple Extra Large (XXXL)" },
                new SelectListItem { Value = "Custom", Text = "Custom Size" }
            };
        }

        private List<SelectListItem> GetAgeGroupOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0-6 months", Text = "0-6 months" },
                new SelectListItem { Value = "6-12 months", Text = "6-12 months" },
                new SelectListItem { Value = "1-2 years", Text = "1-2 years" },
                new SelectListItem { Value = "2-4 years", Text = "2-4 years" },
                new SelectListItem { Value = "4-6 years", Text = "4-6 years" },
                new SelectListItem { Value = "6-8 years", Text = "6-8 years" },
                new SelectListItem { Value = "8-10 years", Text = "8-10 years" },
                new SelectListItem { Value = "10-12 years", Text = "10-12 years" },
                new SelectListItem { Value = "12+ years", Text = "12+ years" }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
