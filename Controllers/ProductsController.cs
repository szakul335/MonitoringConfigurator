using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;

namespace MonitoringConfigurator.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _db;
        public ProductsController(AppDbContext db) => _db = db;

        // PUBLIC LISTA PRODUKTÓW (sklep)
        [HttpGet]
        public async Task<IActionResult> Index(
            string? q,
            ProductCategory? cat,
            decimal? min,
            decimal? max,
            bool only = false,
            string sort = "price_asc",
            int page = 1,
            int pageSize = 24)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();
                query = query.Where(p =>
                    (p.Name ?? string.Empty).Contains(qq) ||
                    (p.Brand ?? string.Empty).Contains(qq) ||
                    (p.Model ?? string.Empty).Contains(qq) ||
                    (p.Description ?? string.Empty).Contains(qq) ||
                    (p.LongDescription ?? string.Empty).Contains(qq));
            }

            // Category
            if (cat.HasValue)
            {
                query = query.Where(p => p.Category == cat.Value);
            }

            // Price
            if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
            if (max.HasValue) query = query.Where(p => p.Price <= max.Value);

            // Stock
            if (only) query = query.Where(p => p.Stock > 0);

            // Sorting
            query = sort switch
            {
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc"   => query.OrderBy(p => p.Name),
                "name_desc"  => query.OrderByDescending(p => p.Name),
                _            => query.OrderBy(p => p.Price)
            };

            // Paging
            if (page < 1) page = 1;
            if (pageSize < 6) pageSize = 6;

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ProductsListVm
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
                Q = q,
                Category = cat,
                SelectedCategory = cat?.ToString(),
                Min = min,
                Max = max,
                Only = only,
                Sort = sort,
                Tabs = new[]
                {
                    "Wszystkie",
                    "Kamery IP",
                    "Rejestratory NVR",
                    "Switche PoE",
                    "Okablowanie",
                    "Zasilanie i UPS",
                    "Akcesoria"
                },
                Categories = Enum.GetValues(typeof(ProductCategory))
                                 .Cast<ProductCategory>()
                                 .Select(c => new SelectListItem
                                 {
                                     Text = GetCategoryLabel(c),
                                     Value = c.ToString(),
                                     Selected = cat == c
                                 })
                                 .ToList()
            };

            return View(vm);
        }

        // PANEL ADMINA – LISTA
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage(string? search)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p =>
                    (p.Name ?? string.Empty).Contains(s) ||
                    (p.Brand ?? string.Empty).Contains(s) ||
                    (p.Model ?? string.Empty).Contains(s));
            }

            var items = await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.Search = search;
            return View(items);
        }

        
        
        // CREATE
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new Product
            {
                Stock = 10
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.Products.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        // EDIT
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (existing == null)
            {
                return NotFound();
            }

            _db.Entry(existing).CurrentValues.SetValues(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        // DELETE – z Manage i z widoku Delete
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Manage));
        }

        // DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // QUICK JSON do podglądu
        [HttpGet]
        public async Task<IActionResult> Quick(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            return Json(new
            {
                product.Id,
                product.Name,
                product.Brand,
                product.Model,
                product.Price,
                product.Category,
                product.Description,
                product.LongDescription,
                product.ImageUrl
            });
        }

        private static string GetCategoryLabel(ProductCategory c) => c switch
        {
            ProductCategory.Camera    => "Kamery IP",
            ProductCategory.NVR       => "Rejestratory NVR",
            ProductCategory.Switch    => "Switche PoE",
            ProductCategory.Cabling   => "Okablowanie i patchcordy",
            ProductCategory.Power     => "Zasilanie i UPS",
            ProductCategory.Accessory => "Akcesoria i montaż",
            _                         => c.ToString()
        };
    }
}
