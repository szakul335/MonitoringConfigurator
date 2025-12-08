using System.ComponentModel.DataAnnotations;

namespace MonitoringConfigurator.Models
{
    public enum ProductCategory
    {
        Camera = 0,
        Recorder = 1,
        Switch = 2,
        Cable = 3,
        Disk = 4,
        Ups = 5,
        Accessory = 6
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [Required]
        public ProductCategory Category { get; set; }

        [Range(0, 1_000_000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, 100_000)]
        public int Stock { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public string? LongDescription { get; set; }

        [Url]
        public string? ImageUrl { get; set; }

        public int? ResolutionMp { get; set; }
        public string? Lens { get; set; }
        public int? IrRangeM { get; set; }
        public bool? Outdoor { get; set; }

        public int? Channels { get; set; }
        public int? MaxBandwidthMbps { get; set; }
        public int? Ports { get; set; }
        public int? PoeBudgetW { get; set; }

        public int? DiskBays { get; set; }
        public int? MaxHddTB { get; set; }
        public double? StorageTB { get; set; }
        public bool? SupportsRaid { get; set; }

        public int? RollLengthM { get; set; }
        public int? UpsVA { get; set; }
    }
}
