using MonitoringConfigurator.Models;

namespace MonitoringConfigurator.Services
{
    public class ConfiguratorService : IConfiguratorService
    {
        public ConfigResult? Propose(Configuration cfg, IEnumerable<Product> products)
        {
            var list = products.ToList();

            // 1) Camera
            var camera = list
                .Where(p => p.Category == ProductCategory.Camera)
                .Where(p => (p.ResolutionMp ?? 0) >= (int)Math.Ceiling(cfg.RequiredResolutionMp))
                .Where(p => (p.IrRangeM ?? 0) >= cfg.RequiredIrRangeM)
                .OrderBy(p => p.Price)
                .FirstOrDefault();
            if (camera is null) return null;

            // 2) Bandwidth estimate
            double baseMbpsPerMPAt15 = 1.6; 
            double fpsFactor = cfg.FrameRateFps / 15.0;
            double motionFactor = 0.6 + (cfg.ExpectedMotionPercent / 100.0); 
            double analyticsFactor = cfg.UseAnalytics ? 1.1 : 1.0;
            double perCamMbps = Math.Max(0.5, baseMbpsPerMPAt15 * camera.ResolutionMp.GetValueOrDefault(2) * fpsFactor * motionFactor * analyticsFactor);
            double totalMbps = perCamMbps * cfg.CameraCount;

            // 3) NVR
            var nvr = list
                .Where(p => p.Category == ProductCategory.NVR)
                .Where(p => (p.Channels ?? 0) >= cfg.CameraCount)
                .Where(p => (p.MaxBandwidthMbps ?? 0) >= (int)Math.Ceiling(totalMbps * 1.2))
                .OrderBy(p => p.Price)
                .FirstOrDefault();
            if (nvr is null) return null;

            // 4) Storage sizing
            double totalBytesPerSec = totalMbps * 1_000_000 / 8.0;
            double totalBytes = totalBytesPerSec * 86400 * cfg.RetentionDays * 1.1;
            double totalTB = totalBytes / 1_000_000_000_000.0;

            var hdds = list.Where(p => p.Category == ProductCategory.Accessory && (p.StorageTB ?? 0) > 0)
                           .OrderBy(p => (p.Price / (decimal)(p.StorageTB ?? 1)))
                           .ToList();
            var bays = nvr.DiskBays ?? 2;
            var pickedHdds = new List<ItemQty>();
            double needTB = totalTB;
            foreach (var h in hdds)
            {
                if (pickedHdds.Sum(x => x.Qty) >= bays) break;
                var eachTB = h.StorageTB!.Value;
                int qty = Math.Min((int)Math.Ceiling(needTB / eachTB), bays - pickedHdds.Sum(x => x.Qty));
                if (qty <= 0) continue;
                pickedHdds.Add(new ItemQty(h, qty));
                needTB -= eachTB * qty;
                if (needTB <= 0) break;
            }
            if (needTB > 0 && pickedHdds.Sum(x => x.Qty) < bays && hdds.Any())
            {
                pickedHdds.Add(new ItemQty(hdds.First(), 1));
            }

            // 5) Switch PoE
            int totalPoe = cfg.CameraCount * cfg.PoePerCameraW;
            var sw = list.Where(p => p.Category == ProductCategory.Switch)
                         .Where(p => (p.Ports ?? 0) >= cfg.CameraCount)
                         .Where(p => (p.PoeBudgetW ?? 0) >= totalPoe)
                         .OrderBy(p => p.Price)
                         .FirstOrDefault();
            if (sw is null) return null;

            // 6) Cable rolls
            var cable = list.Where(p => p.Category == ProductCategory.Cabling && (p.RollLengthM ?? 0) > 0)
                            .OrderBy(p => p.Price / (p.RollLengthM ?? 1))
                            .FirstOrDefault();
            ItemQty? rolls = null;
            if (cable is not null)
            {
                int totalMeters = cfg.CameraCount * cfg.CableLengthPerCameraM;
                int rollLen = cable.RollLengthM!.Value;
                int qty = Math.Max(1, (int)Math.Ceiling(totalMeters / (double)rollLen));
                rolls = new ItemQty(cable, qty);
            }

            // 7) UPS
            int totalWatts = (int)(cfg.CameraCount * cfg.PoePerCameraW * 0.8) + 30 + (int)(totalPoe * 0.1);
            int needVA = (int)(totalWatts * 1.6);
            var ups = list.Where(p => p.Category == ProductCategory.Power && (p.UpsVA ?? 0) > 0)
                          .Where(p => (p.UpsVA ?? 0) >= needVA)
                          .OrderBy(p => p.Price)
                          .FirstOrDefault();

            return new ConfigResult
            {
                Camera = camera,
                Nvr = nvr,
                Switch = sw,
                Hdds = pickedHdds,
                CableRolls = rolls,
                Ups = ups,
                CameraCount = cfg.CameraCount,
                TotalBandwidthMbps = Math.Round(totalMbps, 1),
                TotalStorageTB = Math.Round(totalTB, 2),
                TotalPoeW = totalPoe
            };
        }
    }
}
