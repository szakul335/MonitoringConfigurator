using MonitoringConfigurator.Models;

namespace MonitoringConfigurator.Services
{
    public record ItemQty(Product Product, int Qty);

    public class ConfigResult
    {
        public required Product Camera { get; set; }
        public required Product Nvr { get; set; }
        public required Product Switch { get; set; }
        public List<ItemQty> Hdds { get; set; } = new();
        public ItemQty? CableRolls { get; set; }
        public Product? Ups { get; set; }

        public int CameraCount { get; set; }
        public double TotalBandwidthMbps { get; set; }
        public double TotalStorageTB { get; set; }
        public int TotalPoeW { get; set; }
        public decimal TotalPrice =>
            (Camera.Price * CameraCount)
          + Nvr.Price
          + Switch.Price
          + Hdds.Sum(h => h.Product.Price * h.Qty)
          + (CableRolls is null ? 0 : CableRolls.Product.Price * CableRolls.Qty)
          + (Ups?.Price ?? 0);
    }

    public interface IConfiguratorService
    {
        ConfigResult? Propose(Configuration cfg, IEnumerable<Product> products);
    }
}
