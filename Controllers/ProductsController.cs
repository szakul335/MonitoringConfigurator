using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringConfigurator.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // --- STRONA KATALOGU (Dla wszystkich) ---
        public async Task<IActionResult> Index(ProductCatalogViewModel vm)
        {
            // 1. Pobranie bazowego zapytania (bez śledzenia zmian dla wydajności)
            var query = _context.Products.AsNoTracking().AsQueryable();

            // 2. Filtrowanie podstawowe (Kategoria, Tekst)
            if (vm.Category.HasValue)
            {
                query = query.Where(p => p.Category == vm.Category.Value);
            }

            if (!string.IsNullOrWhiteSpace(vm.Query))
            {
                var q = vm.Query.Trim();
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    (p.Brand != null && p.Brand.Contains(q)) ||
                    (p.Model != null && p.Model.Contains(q)));
            }

            // 3. Filtrowanie zaawansowane
            if (vm.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= vm.MinPrice.Value);
            }

            if (vm.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= vm.MaxPrice.Value);
            }

            if (vm.MinResolution.HasValue)
            {
                query = query.Where(p => p.ResolutionMp >= vm.MinResolution.Value);
            }

            if (vm.OutdoorOnly)
            {
                query = query.Where(p => p.Outdoor == true);
            }

            // 4. Sortowanie
            query = vm.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderBy(p => p.Name) // Domyślne sortowanie (A-Z)
            };

            // 5. Wykonanie zapytania i przekazanie wyników do modelu
            vm.Products = await query.ToListAsync();

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // --- PANEL ADMINISTRATORA (Wymaga roli Admin) ---

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(int? id, ProductCategory? category, string? query)
        {
            var products = await BuildFilteredQuery(category, query)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var editableProduct = id.HasValue
                ? await _context.Products.FindAsync(id.Value)
                : new Product();

            if (id.HasValue && editableProduct == null)
            {
                return NotFound();
            }

            var vm = new ProductManagementViewModel
            {
                Category = category,
                Query = query,
                Products = products,
                EditableProduct = editableProduct
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ProductManagementViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Products = await BuildFilteredQuery(viewModel.Category, viewModel.Query)
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return View(viewModel);
            }

            var isEdit = viewModel.EditableProduct.Id != 0;

            if (isEdit)
            {
                var exists = await _context.Products.AnyAsync(p => p.Id == viewModel.EditableProduct.Id);
                if (!exists) return NotFound();

                _context.Entry(viewModel.EditableProduct).State = EntityState.Modified;
                TempData["Toast"] = "Zmiany zostały zapisane.";
            }
            else
            {
                _context.Products.Add(viewModel.EditableProduct);
                TempData["Toast"] = "Produkt został dodany.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage), new { category = viewModel.Category, query = viewModel.Query });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Produkt został usunięty.";

            return RedirectToAction(nameof(Manage));
        }

        // --- Metody pomocnicze ---
        private IQueryable<Product> BuildFilteredQuery(ProductCategory? category, string? query)
        {
            var products = _context.Products.AsQueryable();

            if (category.HasValue)
                products = products.Where(p => p.Category == category.Value);

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim();
                products = products.Where(p =>
                    (p.Name != null && p.Name.Contains(query)) ||
                    (p.Brand != null && p.Brand.Contains(query)) ||
                    (p.Model != null && p.Model.Contains(query)));
            }
            return products;
        }
    }
}