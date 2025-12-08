using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonitoringConfigurator.Models
{
    public class ProductCatalogViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();

        [Display(Name = "Kategoria")]
        public ProductCategory? Category { get; set; }

        [Display(Name = "Szukaj")]
        public string? Query { get; set; }
    }
}
