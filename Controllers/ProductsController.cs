using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;

namespace MonitoringConfigurator.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(ProductCategory? category, string? query)
        {
            var products = await BuildFilteredQuery(category, query)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var vm = new ProductCatalogViewModel
            {
                Category = category,
                Query = query,
                Products = products
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(ProductCategory? category, string? query)
        {
            var products = await BuildFilteredQuery(category, query)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var vm = new ProductCatalogViewModel
            {
                Category = category,
                Query = query,
                Products = products
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Product { Stock = 1, Price = 0 });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Produkt został dodany.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Zmiany zapisane.";
            return RedirectToAction(nameof(Manage));
        }

        private IQueryable<Product> BuildFilteredQuery(ProductCategory? category, string? query)
        {
            var products = _context.Products.AsQueryable();

            if (category.HasValue)
            {
                products = products.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                products = products.Where(p =>
                    (p.Name != null && p.Name.Contains(query)) ||
                    (p.Brand != null && p.Brand.Contains(query)) ||
                    (p.Model != null && p.Model.Contains(query)));
            }

            return products;
        }
    }
}
