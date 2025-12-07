using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringConfigurator.Models
{
    public class Product
    {
        public int Id { get; set; }

        // Common
        public string? LongDescription { get; set; }
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public ProductCategory Category { get; set; }

        [NotMapped]
        public string CategoryLabel => Category switch
        {
            ProductCategory.Camera => "Kamery IP",
            ProductCategory.NVR => "Rejestratory NVR",
            ProductCategory.Switch => "Switche PoE",
            ProductCategory.Cabling => "Okablowanie i patchcordy",
            ProductCategory.Power => "Zasilanie i UPS",
            ProductCategory.Accessory => "Akcesoria i montaż",
            _ => Category.ToString()
        };

        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        // Camera
        public int? ResolutionMp { get; set; }
        public int? IrRangeM { get; set; }
        [NotMapped]
        public string? Lens { get; set; }
        public bool? Outdoor { get; set; }

        // NVR
        public int? Channels { get; set; }
        public int? MaxBandwidthMbps { get; set; }
        public int? DiskBays { get; set; }
        public int? MaxHddTB { get; set; }
        public bool? SupportsRaid { get; set; }

        // Switch
        public int? Ports { get; set; }
        public int? PoeBudgetW { get; set; }

        // Cabling
        public int? RollLengthM { get; set; }

        // HDD / Accessory
        public double? StorageTB { get; set; }

        // UPS / Power
        public int? UpsVA { get; set; }
    }
}
