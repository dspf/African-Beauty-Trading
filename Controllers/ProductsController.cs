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
                {"M", "Chest: 38-40\", Waist: 32-34\""},
                {"L", "Chest: 41-43\", Waist: 35-37\""},
                {"XL", "Chest: 44-46\", Waist: 38-40\""},
                {"XXL", "Chest: 47-49\", Waist: 41-43\""},
                {"XXXL", "Chest: 50-52\", Waist: 44-46\""}
            };
            }

            public static Dictionary<string, string> GetKidsSizeChart()
            {
                return new Dictionary<string, string>
            {
                {"0-6 months", "Height: 24-27\", Weight: 12-17 lbs"},
                {"6-12 months", "Height: 27-30\", Weight: 17-22 lbs"},
                {"1-2 years", "Height: 30-34\", Weight: 22-28 lbs"},
                {"2-4 years", "Height: 34-40\", Weight: 28-36 lbs"},
                {"4-6 years", "Height: 40-45\", Weight: 36-46 lbs"},
                {"6-8 years", "Height: 45-50\", Weight: 46-56 lbs"},
                {"8-10 years", "Height: 50-55\", Weight: 56-68 lbs"},
                {"10-12 years", "Height: 55-60\", Weight: 68-82 lbs"},
                {"12+ years", "Height: 60+\", Weight: 82+ lbs"}
            };
            }

            public static List<string> GetAllSizes()
            {
                var sizes = new List<string>();

                // Adult sizes
                sizes.AddRange(GetAdultSizeChart().Keys);

                // Kids sizes  
                sizes.AddRange(GetKidsSizeChart().Keys);

                // Additional common sizes
                sizes.AddRange(new List<string>
            {
                "One Size", "28", "30", "32", "34", "36", "38", "40",
                "42", "44", "46", "48", "50", "52", "54"
            });

                return sizes.Distinct().OrderBy(s => s).ToList();
            }
        }

        // GET: Products
        public ActionResult Index(string categoryFilter = "", string sizeFilter = "", string searchString = "")
        {
            IQueryable<Product> products = db.Products
                .Include(p => p.Category);

            // Apply filters if specified
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                products = products.Where(p => p.Category.Name == categoryFilter);
            }

            if (!string.IsNullOrEmpty(sizeFilter))
            {
                products = products.Where(p => p.AvailableSizes.Contains(sizeFilter));
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) ||
                                           p.Description.Contains(searchString));
            }

            // Populate filter dropdowns
            ViewBag.CategoryFilters = new SelectList(db.Categories, "Name", "Name", categoryFilter);
            ViewBag.SizeFilters = new SelectList(SizeHelper.GetAllSizes(), sizeFilter);
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
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.AvailableSizes = product.AvailableSizes?.Split(',').ToList() ?? new List<string>();
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            // Initialize new product with default values
            var product = new Product
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Featured = false
            };

            // Populate dropdowns
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name");
            ViewBag.AllSizes = SizeHelper.GetAllSizes();

            return View(product);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product, HttpPostedFileBase ImageFile, List<string> SelectedSizes)
        {
            // Debug: Log the received data
            System.Diagnostics.Debug.WriteLine($"Product Name: {product.Name}");
            System.Diagnostics.Debug.WriteLine($"CategoryId: {product.CategoryId}");
            System.Diagnostics.Debug.WriteLine($"Price: {product.Price}");
            System.Diagnostics.Debug.WriteLine($"Selected Sizes: {string.Join(", ", SelectedSizes ?? new List<string>())}");

            // Process selected sizes
            if (SelectedSizes != null && SelectedSizes.Any())
            {
                product.AvailableSizes = string.Join(",", SelectedSizes);
            }
            else
            {
                product.AvailableSizes = "One Size"; // Default if no sizes selected
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
                    product.ImageUrl = "/Uploads/" + fileName;
                }

                // Set additional properties
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;

                db.Products.Add(product);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("Index");
            }

            // If we got this far, something failed; redisplay form
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
            ViewBag.AllSizes = SizeHelper.GetAllSizes();
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
            ViewBag.AllSizes = SizeHelper.GetAllSizes();
            ViewBag.SelectedSizes = product.AvailableSizes?.Split(',').ToList() ?? new List<string>();

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, HttpPostedFileBase ImageFile, List<string> SelectedSizes)
        {
            // Process selected sizes
            if (SelectedSizes != null && SelectedSizes.Any())
            {
                product.AvailableSizes = string.Join(",", SelectedSizes);
            }
            else
            {
                product.AvailableSizes = "One Size";
            }

            if (ModelState.IsValid)
            {
                // Handle image update
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string oldPath = Server.MapPath(product.ImageUrl);
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
                    product.ImageUrl = "/Uploads/" + fileName;
                }

                // Update tracking
                product.UpdatedAt = DateTime.Now;

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }

            // If we got this far, something failed; redisplay form
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
            ViewBag.AllSizes = SizeHelper.GetAllSizes();
            ViewBag.SelectedSizes = SelectedSizes;
            return View(product);
        }

        // Rest of your methods remain the same...
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
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                string path = Server.MapPath(product.ImageUrl);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            db.Products.Remove(product);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("Index");
        }

        // Toggle featured status
        [HttpPost]
        public ActionResult ToggleFeatured(int id)
        {
            var product = db.Products.Find(id);
            if (product != null)
            {
                product.Featured = !product.Featured;
                product.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                return Json(new { success = true, featured = product.Featured });
            }
            return Json(new { success = false });
        }

        // Get featured products
        public ActionResult Featured()
        {
            var featuredProducts = db.Products
                .Include(p => p.Category)
                .Where(p => p.Featured && p.Stock > 0)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(featuredProducts);
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
