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
                if (!exists)
                {
                    return NotFound();
                }
            }

            if (!isEdit)
            {
                _context.Products.Add(viewModel.EditableProduct);
                TempData["Toast"] = "Produkt został dodany.";
            }
            else
            {
                _context.Entry(viewModel.EditableProduct).State = EntityState.Modified;
                TempData["Toast"] = "Zmiany zapisane.";
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
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Produkt został usunięty.";

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Product());
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
            EnsureProductColumns();

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

        private static bool _productColumnsEnsured;
        private static readonly object _ensureColumnsLock = new();

        private void EnsureProductColumns()
        {
            if (_productColumnsEnsured)
            {
                return;
            }

            lock (_ensureColumnsLock)
            {
                if (_productColumnsEnsured)
                {
                    return;
                }

                try
                {
                    _context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE Name = 'ShortDescription' AND Object_ID = OBJECT_ID('Products')
                        )
                        BEGIN
                            ALTER TABLE [Products] ADD [ShortDescription] NVARCHAR(300) NULL;
                        END

                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE Name = 'Price' AND Object_ID = OBJECT_ID('Products')
                        )
                        BEGIN
                            ALTER TABLE [Products] ADD [Price] DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Products_Price DEFAULT 0;
                            ALTER TABLE [Products] DROP CONSTRAINT DF_Products_Price;
                        END
                    ");
                }
                catch
                {
                    // If we cannot enforce the columns here, let the subsequent query surface the issue.
                }

                _productColumnsEnsured = true;
            }
        }
    }
}
