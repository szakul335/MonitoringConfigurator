using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringConfigurator.Models
{
    public class ProductsListVm
    {
        public List<Product> Items { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();

        public string[] Tabs { get; set; } = new string[0];
        public string ActiveTab { get; set; } = "all";

        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public string? Q { get; set; }
        public ProductCategory? Category { get; set; }
        public string? SelectedCategory { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public bool Only { get; set; }
        public string Sort { get; set; } = "price_asc";
    }
}