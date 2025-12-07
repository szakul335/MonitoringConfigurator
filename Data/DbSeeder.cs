using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonitoringConfigurator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringConfigurator.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var ctx = sp.GetRequiredService<AppDbContext>();
            try { await ctx.Database.MigrateAsync(); } catch { await ctx.Database.EnsureCreatedAsync(); }

            // Admin user
            var um = sp.GetRequiredService<UserManager<IdentityUser>>();
            var adminEmail = "admin@demo.pl";
            var adminPass = "Admin123!";
            if (await um.FindByEmailAsync(adminEmail) is null)
            {
                var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await um.CreateAsync(user, adminPass);
            }

            if (await ctx.Products.AnyAsync()) return; 
            string img = "https://via.placeholder.com/300";
            var items = new List<Product>();

            // --- CAMERAS (10) ---
            items.AddRange(new[] {
                new Product { Name="Cam 2MP Indoor", Brand="Acme", Model="C2I-01", Category=ProductCategory.Camera, Price=249, Stock=40, ResolutionMp=2, IrRangeM=20, Outdoor=false, ImageUrl=img },
                new Product { Name="Cam 4MP Outdoor IR30", Brand="Acme", Model="C4O-IR30", Category=ProductCategory.Camera, Price=349, Stock=35, ResolutionMp=4, IrRangeM=30, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 5MP Varifocal", Brand="Opti", Model="V-5MP", Category=ProductCategory.Camera, Price=489, Stock=25, ResolutionMp=5, IrRangeM=40, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 8MP Dome", Brand="Opti", Model="D-8MP", Category=ProductCategory.Camera, Price=699, Stock=20, ResolutionMp=8, IrRangeM=30, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 8MP Color", Brand="NightX", Model="T-8C", Category=ProductCategory.Camera, Price=779, Stock=18, ResolutionMp=8, IrRangeM=60, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 4MP Mini", Brand="LiteCam", Model="MINI-4", Category=ProductCategory.Camera, Price=299, Stock=50, ResolutionMp=4, IrRangeM=15, Outdoor=false, ImageUrl=img },
                new Product { Name="Cam 2MP Bullet IR50", Brand="NightX", Model="B-2IR50", Category=ProductCategory.Camera, Price=299, Stock=30, ResolutionMp=2, IrRangeM=50, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 5MP ColorVu", Brand="Acme", Model="C-5CV", Category=ProductCategory.Camera, Price=549, Stock=24, ResolutionMp=5, IrRangeM=0, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 8MP Fisheye", Brand="Opti", Model="F-8MP", Category=ProductCategory.Camera, Price=999, Stock=10, ResolutionMp=8, IrRangeM=15, Outdoor=true, ImageUrl=img },
                new Product { Name="Cam 4MP PTZ 25x", Brand="PTZPro", Model="PTZ-4-25X", Category=ProductCategory.Camera, Price=1899, Stock=6, ResolutionMp=4, IrRangeM=150, Outdoor=true, ImageUrl=img },
            });

            // --- NVR (10) ---
            items.AddRange(new[] {
                new Product { Name="NVR 4ch 40Mbps", Brand="Videx", Model="NVR4-40", Category=ProductCategory.NVR, Price=499, Stock=15, Channels=4,  MaxBandwidthMbps=40, DiskBays=1, ImageUrl=img },
                new Product { Name="NVR 8ch 80Mbps", Brand="Videx", Model="NVR8-80", Category=ProductCategory.NVR, Price=699, Stock=12, Channels=8,  MaxBandwidthMbps=80, DiskBays=1, ImageUrl=img },
                new Product { Name="NVR 8ch 128Mbps", Brand="SafeRec", Model="NVR8-128", Category=ProductCategory.NVR, Price=799, Stock=10, Channels=8,  MaxBandwidthMbps=128, DiskBays=2, ImageUrl=img },
                new Product { Name="NVR 16ch 160Mbps", Brand="SafeRec", Model="NVR16-160", Category=ProductCategory.NVR, Price=999, Stock=10, Channels=16, MaxBandwidthMbps=160, DiskBays=2, ImageUrl=img },
                new Product { Name="NVR 16ch 256Mbps", Brand="OptiRec", Model="NVR16-256", Category=ProductCategory.NVR, Price=1199, Stock=8, Channels=16, MaxBandwidthMbps=256, DiskBays=2, ImageUrl=img },
                new Product { Name="NVR 32ch 320Mbps", Brand="OptiRec", Model="NVR32-320", Category=ProductCategory.NVR, Price=1799, Stock=6, Channels=32, MaxBandwidthMbps=320, DiskBays=4, ImageUrl=img },
                new Product { Name="NVR 32ch 512Mbps", Brand="UltraRec", Model="NVR32-512", Category=ProductCategory.NVR, Price=2299, Stock=5, Channels=32, MaxBandwidthMbps=512, DiskBays=4, SupportsRaid=true, ImageUrl=img },
                new Product { Name="NVR 64ch 640Mbps", Brand="UltraRec", Model="NVR64-640", Category=ProductCategory.NVR, Price=2999, Stock=3, Channels=64, MaxBandwidthMbps=640, DiskBays=8, SupportsRaid=true, ImageUrl=img },
                new Product { Name="NVR 64ch 768Mbps RAID", Brand="UltraRec", Model="NVR64-768R", Category=ProductCategory.NVR, Price=3899, Stock=2, Channels=64, MaxBandwidthMbps=768, DiskBays=8, SupportsRaid=true, ImageUrl=img },
                new Product { Name="NVR 128ch 1024Mbps", Brand="ProRec", Model="NVR128-1024", Category=ProductCategory.NVR, Price=6999, Stock=1, Channels=128, MaxBandwidthMbps=1024, DiskBays=16, SupportsRaid=true, ImageUrl=img },
            });

            // --- SWITCHES PoE (10) ---
            items.AddRange(new[] {
                new Product { Name="Switch 4p PoE 65W", Brand="NetCo", Model="SW4P65", Category=ProductCategory.Switch, Price=199, Stock=25, Ports=4, PoeBudgetW=65, ImageUrl=img },
                new Product { Name="Switch 8p PoE 120W", Brand="NetCo", Model="SW8P120", Category=ProductCategory.Switch, Price=399, Stock=20, Ports=8, PoeBudgetW=120, ImageUrl=img },
                new Product { Name="Switch 8p PoE 150W", Brand="NetCo", Model="SW8P150", Category=ProductCategory.Switch, Price=449, Stock=18, Ports=8, PoeBudgetW=150, ImageUrl=img },
                new Product { Name="Switch 16p PoE 150W", Brand="EdgeNet", Model="SW16P150", Category=ProductCategory.Switch, Price=699, Stock=15, Ports=16, PoeBudgetW=150, ImageUrl=img },
                new Product { Name="Switch 16p PoE 250W", Brand="EdgeNet", Model="SW16P250", Category=ProductCategory.Switch, Price=899, Stock=12, Ports=16, PoeBudgetW=250, ImageUrl=img },
                new Product { Name="Switch 24p PoE 250W", Brand="EdgeNet", Model="SW24P250", Category=ProductCategory.Switch, Price=1199, Stock=10, Ports=24, PoeBudgetW=250, ImageUrl=img },
                new Product { Name="Switch 24p PoE 370W", Brand="EdgeNet", Model="SW24P370", Category=ProductCategory.Switch, Price=1499, Stock=8, Ports=24, PoeBudgetW=370, ImageUrl=img },
                new Product { Name="Switch 24p PoE+ 480W", Brand="ProNet", Model="SW24P480", Category=ProductCategory.Switch, Price=1899, Stock=6, Ports=24, PoeBudgetW=480, ImageUrl=img },
                new Product { Name="Switch 48p PoE 740W", Brand="ProNet", Model="SW48P740", Category=ProductCategory.Switch, Price=3499, Stock=3, Ports=48, PoeBudgetW=740, ImageUrl=img },
                new Product { Name="Switch 8p PoE + 2SFP 120W", Brand="NetCo", Model="SW8P2SFP120", Category=ProductCategory.Switch, Price=599, Stock=14, Ports=8, PoeBudgetW=120, ImageUrl=img },
            });

            // --- CABLING (8) ---
            items.AddRange(new[] {
                new Product { Name="UTP Cat5e 305m", Brand="WireX", Model="UTP5e-305", Category=ProductCategory.Cabling, Price=229, Stock=20, RollLengthM=305, ImageUrl=img },
                new Product { Name="UTP Cat6 305m", Brand="WireX", Model="UTP6-305", Category=ProductCategory.Cabling, Price=349, Stock=20, RollLengthM=305, ImageUrl=img },
                new Product { Name="F/UTP Cat6 305m", Brand="WireX", Model="FUTP6-305", Category=ProductCategory.Cabling, Price=449, Stock=15, RollLengthM=305, ImageUrl=img },
                new Product { Name="S/FTP Cat6A 305m", Brand="WireX", Model="SFTP6A-305", Category=ProductCategory.Cabling, Price=799, Stock=10, RollLengthM=305, ImageUrl=img },
                new Product { Name="Patchcord Cat6 1m", Brand="WireX", Model="PC6-1", Category=ProductCategory.Cabling, Price=8, Stock=200, ImageUrl=img },
                new Product { Name="Patchcord Cat6 3m", Brand="WireX", Model="PC6-3", Category=ProductCategory.Cabling, Price=12, Stock=200, ImageUrl=img },
                new Product { Name="Patchcord Cat6 5m", Brand="WireX", Model="PC6-5", Category=ProductCategory.Cabling, Price=16, Stock=180, ImageUrl=img },
                new Product { Name="Peszel 25mm 25m", Brand="Flexi", Model="P25-25", Category=ProductCategory.Cabling, Price=49, Stock=40, ImageUrl=img },
            });

            // --- POWER & ACCESSORIES (12) ---
            items.AddRange(new[] {
                new Product { Name="Zasilacz 12V 2A", Brand="PSU", Model="12V2A", Category=ProductCategory.Power, Price=29, Stock=80, ImageUrl=img },
                new Product { Name="Zasilacz 12V 5A", Brand="PSU", Model="12V5A", Category=ProductCategory.Power, Price=49, Stock=60, ImageUrl=img },
                new Product { Name="UPS 600VA", Brand="PowerX", Model="UPS600", Category=ProductCategory.Power, Price=269, Stock=20, UpsVA=600, ImageUrl=img },
                new Product { Name="UPS 1000VA", Brand="PowerX", Model="UPS1000", Category=ProductCategory.Power, Price=469, Stock=15, UpsVA=1000, ImageUrl=img },
                new Product { Name="UPS 1500VA", Brand="PowerX", Model="UPS1500", Category=ProductCategory.Power, Price=699, Stock=10, UpsVA=1500, ImageUrl=img },
                new Product { Name="Obudowa IP66", Brand="BoxPro", Model="BOX-IP66", Category=ProductCategory.Accessory, Price=99, Stock=30, ImageUrl=img },
                new Product { Name="Uchwyty kamer (zestaw)", Brand="MountIt", Model="U-SET", Category=ProductCategory.Accessory, Price=39, Stock=50, ImageUrl=img },
                new Product { Name="Dysk 2TB surveillance", Brand="StorageX", Model="HDD-2TB", Category=ProductCategory.Accessory, Price=299, Stock=20, StorageTB=2, ImageUrl=img },
                new Product { Name="Dysk 4TB surveillance", Brand="StorageX", Model="HDD-4TB", Category=ProductCategory.Accessory, Price=449, Stock=15, StorageTB=4, ImageUrl=img },
                new Product { Name="Dysk 6TB surveillance", Brand="StorageX", Model="HDD-6TB", Category=ProductCategory.Accessory, Price=599, Stock=12, StorageTB=6, ImageUrl=img },
                new Product { Name="Dysk 8TB surveillance", Brand="StorageX", Model="HDD-8TB", Category=ProductCategory.Accessory, Price=799, Stock=10, StorageTB=8, ImageUrl=img },
                new Product { Name="Patch panel 24p", Brand="RackOne", Model="PP-24", Category=ProductCategory.Accessory, Price=149, Stock=20, ImageUrl=img },
            });

            
            foreach (var p in items)
            {
                if (string.IsNullOrWhiteSpace(p.LongDescription) && string.IsNullOrWhiteSpace(p.Description))
                {
                    p.LongDescription = p.Category switch
                    {
                        ProductCategory.Camera => $"{p.Name} – kamera IP przeznaczona do systemów monitoringu CCTV. Idealna do małych i średnich instalacji, z możliwością pracy całodobowej oraz wsparciem dla typowych rejestratorów NVR.",
                        ProductCategory.NVR => $"{p.Name} – rejestrator sieciowy do obsługi wielu kamer IP. Zapewnia stabilne nagrywanie, podgląd na żywo oraz prostą konfigurację z poziomu przeglądarki.",
                        ProductCategory.Switch => $"{p.Name} – przełącznik sieciowy z zasilaniem PoE, ułatwiający okablowanie i zasilanie kamer z jednego punktu.",
                        ProductCategory.Cabling => $"{p.Name} – przewód lub osprzęt instalacyjny przeznaczony do prowadzenia okablowania strukturalnego w systemach monitoringu.",
                        ProductCategory.Power => $"{p.Name} – zasilacz lub UPS zapewniający stabilne zasilanie dla rejestratorów, kamer oraz urządzeń sieciowych.",
                        ProductCategory.Accessory => $"{p.Name} – akcesorium uzupełniające instalację CCTV (uchwyty, obudowy, dyski, patchpanele itd.).",
                        _ => $"{p.Name} – element systemu monitoringu."
                    };
                }
            }

foreach (var p in items)
            {
                if (!await ctx.Products.AnyAsync(x => x.Name == p.Name && x.Model == p.Model))
                    ctx.Products.Add(p);
            }

            await ctx.SaveChangesAsync();
        }
    }
}
