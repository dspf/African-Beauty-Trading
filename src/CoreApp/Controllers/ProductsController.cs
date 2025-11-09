#if NET7_0
using Microsoft.AspNetCore.Mvc;
using African_Beauty_Trading.CoreApp;
using African_Beauty_Trading.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Products
        public IActionResult Index()
        {
            var products = _db.Products.ToList();
            return View(products);
        }

        // GET: /Products/Details/5
        public IActionResult Details(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            // populate any viewbags needed by the view (categories, sizes)
            ViewBag.AllSizes = new[] { "XS", "S", "M", "L", "XL", "XXL" };
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_db.Categories, "Id", "Name");
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile, List<string> SelectedSizes)
        {
            if (SelectedSizes != null && SelectedSizes.Any())
            {
                product.AvailableSizes = string.Join(",", SelectedSizes);
            }
            else
            {
                product.AvailableSizes = "One Size";
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                var ext = Path.GetExtension(ImageFile.FileName);
                var finalName = fileName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ext;
                var filePath = Path.Combine(uploads, finalName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = "/uploads/" + finalName;
            }

            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _db.Products.Add(product);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Product created successfully";
                return RedirectToAction("Index");
            }

            ViewBag.AllSizes = new[] { "XS", "S", "M", "L", "XL", "XXL" };
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_db.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: /Products/Edit/5
        public IActionResult Edit(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();
            ViewBag.AllSizes = new[] { "XS", "S", "M", "L", "XL", "XXL" };
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_db.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile ImageFile, List<string> SelectedSizes)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();

            if (SelectedSizes != null && SelectedSizes.Any())
            {
                product.AvailableSizes = string.Join(",", SelectedSizes);
            }
            else
            {
                product.AvailableSizes = "One Size";
            }

            // Update basic fields
            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.Featured = model.Featured;
            product.CategoryId = model.CategoryId;
            product.UpdatedAt = DateTime.Now;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                // delete old image
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldPath = product.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullOld = Path.Combine(_env.ContentRootPath, oldPath);
                    if (System.IO.File.Exists(fullOld))
                    {
                        System.IO.File.Delete(fullOld);
                    }
                }

                var fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                var ext = Path.GetExtension(ImageFile.FileName);
                var finalName = fileName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ext;
                var filePath = Path.Combine(uploads, finalName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = "/uploads/" + finalName;
            }

            if (ModelState.IsValid)
            {
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Product updated successfully";
                return RedirectToAction("Index");
            }

            ViewBag.AllSizes = new[] { "XS", "S", "M", "L", "XL", "XXL" };
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_db.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();

            // delete image file
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldPath = product.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullOld = Path.Combine(_env.ContentRootPath, oldPath);
                if (System.IO.File.Exists(fullOld)) System.IO.File.Delete(fullOld);
            }

            _db.Products.Remove(product);
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Product deleted";
            return RedirectToAction("Index");
        }
    }
}
#endif