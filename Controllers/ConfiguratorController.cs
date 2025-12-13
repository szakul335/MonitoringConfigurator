using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MonitoringConfigurator.Controllers
{
    public class ConfiguratorController : Controller
    {
        private readonly AppDbContext _ctx;

        public ConfiguratorController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ConfiguratorInputModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(ConfiguratorInputModel input)
        {
            if (input.TotalCameras <= 0)
                ModelState.AddModelError("", "Wybierz przynajmniej jedną kamerę.");

            if (!ModelState.IsValid) return View("Index", input);

            var result = new ConfigurationResult { Input = input };

            // 1. KAMERY
            if (input.OutdoorCamCount > 0)
            {
                result.SelectedOutdoorCam = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Camera && p.ResolutionMp >= input.ResolutionMp && p.Outdoor == true)
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();
            }
            if (input.IndoorCamCount > 0)
            {
                result.SelectedIndoorCam = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Camera && p.ResolutionMp >= input.ResolutionMp)
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();
            }

            // 2. PARAMETRY
            double bitrate = input.ResolutionMp * 1.5;
            result.EstimatedBandwidthMbps = Math.Round(bitrate * input.TotalCameras, 2);

            int powerW = (input.OutdoorCamCount * (result.SelectedOutdoorCam?.PoeBudgetW ?? 12)) +
                         (input.IndoorCamCount * (result.SelectedIndoorCam?.PoeBudgetW ?? 8));
            result.EstimatedPoEBudgetW = powerW;

            double dailyGB = (bitrate / 8) * 3600 * 24 / 1024;
            // Współczynnik ruchu (mniej w magazynie, więcej na parkingu)
            double activity = input.Building == BuildingType.Warehouse ? 0.2 : 0.4;
            result.EstimatedStorageTB = Math.Round(dailyGB * (0.3 + activity) * input.RecordingDays * input.TotalCameras / 1024, 2);

            // 3. REJESTRATOR & DYSKI
            result.SelectedNvr = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Recorder && p.Channels >= input.TotalCameras && p.MaxBandwidthMbps >= result.EstimatedBandwidthMbps)
                .OrderBy(p => p.Price).FirstOrDefaultAsync();

            var maxDisk = await _ctx.Products.Where(p => p.Category == ProductCategory.Disk).OrderByDescending(p => p.StorageTB).FirstOrDefaultAsync();
            if (maxDisk != null && maxDisk.StorageTB > 0)
            {
                int needed = (int)Math.Ceiling(result.EstimatedStorageTB / maxDisk.StorageTB.Value);
                int slots = result.SelectedNvr?.DiskBays ?? 1;
                result.DiskQuantity = Math.Min(needed, slots);
                result.SelectedDisk = maxDisk;
            }

            // 4. SWITCH POE
            if (input.NeedPoE)
            {
                result.SelectedSwitch = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Switch && p.Ports >= input.TotalCameras && p.PoeBudgetW >= result.EstimatedPoEBudgetW)
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();
                if (result.SelectedSwitch != null) result.SwitchQuantity = 1;
            }

            // 5. OKABLOWANIE (Szacowanie długości)
            // Przyjmujemy: sqrt(powierzchnia) * 2 * liczba kamer (zapas na prowadzenie po ścianach)
            double avgDist = Math.Sqrt(input.AreaM2) * 2.0;
            int totalMeters = (int)(avgDist * input.TotalCameras);
            result.EstimatedCableMeters = totalMeters;

            if (input.NeedCabling)
            {
                var cable = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Cable && p.RollLengthM > 0)
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();

                if (cable != null)
                {
                    result.SelectedCable = cable;
                    result.CableQuantity = (int)Math.Ceiling((double)totalMeters / cable.RollLengthM.Value);
                }
            }

            // 6. UPS (Czas podtrzymania)
            if (input.NeedUps)
            {
                int loadW = result.EstimatedPoEBudgetW + 40; // Kamery + NVR + Switch
                // Uproszczona fizyka: Aby podtrzymać X watów przez Y minut, potrzebujemy większej pojemności (VA).
                // Standardowe UPSy przy pełnym obciążeniu trzymają ok 5-10 min. 
                // Mnożnik "przewymiarowania" mocy, aby uzyskać czas na baterii:
                double timeFactor = input.UpsRuntimeMinutes <= 15 ? 1.5 : (input.UpsRuntimeMinutes / 10.0);
                int neededVA = (int)((loadW / 0.6) * timeFactor);

                result.SelectedUps = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Ups && p.UpsVA >= neededVA)
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();

                if (result.SelectedUps != null) result.UpsQuantity = 1;
            }

            // 7. AKCESORIA MONTAŻOWE (Auto-dobór z bazy)
            // Korytka (jeśli natynkowa)
            if (input.InstallType == InstallationType.Surface)
            {
                // Szukamy produktu "Koryto" lub "Listwa"
                result.SelectedTray = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Accessory && (p.Name.Contains("Koryt") || p.Name.Contains("Listwa")))
                    .OrderBy(p => p.Price).FirstOrDefaultAsync();
                if (result.SelectedTray != null) result.TrayMeters = totalMeters; // Metr za metr kabla
            }

            // Uchwyty do kabli (zawsze się przydadzą, jeśli nie ma koryt lub w korytach)
            // Przyjmijmy 2 uchwyty na metr
            result.SelectedClips = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Accessory && (p.Name.Contains("Uchwyt") || p.Name.Contains("Klips")))
                .OrderBy(p => p.Price).FirstOrDefaultAsync();
            if (result.SelectedClips != null) result.ClipsQuantity = (int)Math.Ceiling((totalMeters * 2) / 100.0); // Zakładamy paczki po 100 szt (uproszczenie - 1 produkt = 1 paczka)

            // Wkręty/Kołki (4 na kamerę)
            result.SelectedScrews = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Accessory && (p.Name.Contains("Wkręt") || p.Name.Contains("Koł")))
                .OrderBy(p => p.Price).FirstOrDefaultAsync();
            if (result.SelectedScrews != null) result.ScrewsQuantity = (int)Math.Ceiling((input.TotalCameras * 4) / 50.0); // Paczki np. po 50

            // 8. USŁUGA MONTAŻU
            if (input.NeedAssembly)
            {
                // Wycena: 200 zł za punkt (kamerę) + 300 zł konfiguracja centrali + kable (5 zł/m)
                // Uproszczona: 250 zł * kamera
                result.AssemblyCost = input.TotalCameras * 250.00m + 300.00m;
            }

            return View("Summary", result);
        }

        [HttpPost]
        public IActionResult GeneratePdf(string jsonResult)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var result = System.Text.Json.JsonSerializer.Deserialize<ConfigurationResult>(jsonResult);
            if (result == null) return RedirectToAction("Index");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Kosztorys Systemu CCTV").FontSize(20).SemiBold().FontColor(Colors.Red.Medium);
                            col.Item().Text($"Obiekt: {result.Input.Building}, {result.Input.AreaM2} m2, Instalacja: {result.Input.InstallType}");
                            col.Item().Text($"Data: {DateTime.Now:yyyy-MM-dd HH:mm}");
                        });
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header => {
                                header.Cell().Text("#").Bold();
                                header.Cell().Text("Pozycja").Bold();
                                header.Cell().AlignRight().Text("Ilość").Bold();
                                header.Cell().AlignRight().Text("Wartość").Bold();
                            });

                            int i = 1;
                            void AddRow(string name, string desc, int qty, decimal price)
                            {
                                if (qty <= 0) return;
                                table.Cell().Text($"{i++}.");
                                table.Cell().Column(c => {
                                    c.Item().Text(name).Bold();
                                    if (!string.IsNullOrEmpty(desc)) c.Item().Text(desc).FontSize(9).FontColor(Colors.Grey.Medium);
                                });
                                table.Cell().AlignRight().Text(qty.ToString());
                                table.Cell().AlignRight().Text($"{(price * qty):N2}");
                            }

                            // Produkty
                            if (result.SelectedOutdoorCam != null) AddRow(result.SelectedOutdoorCam.Name, "Kamera zewn.", result.Input.OutdoorCamCount, result.SelectedOutdoorCam.Price);
                            if (result.SelectedIndoorCam != null) AddRow(result.SelectedIndoorCam.Name, "Kamera wewn.", result.Input.IndoorCamCount, result.SelectedIndoorCam.Price);
                            if (result.SelectedNvr != null) AddRow(result.SelectedNvr.Name, "Rejestrator", result.NvrQuantity, result.SelectedNvr.Price);
                            if (result.SelectedSwitch != null) AddRow(result.SelectedSwitch.Name, "Switch PoE", result.SwitchQuantity, result.SelectedSwitch.Price);
                            if (result.SelectedDisk != null) AddRow(result.SelectedDisk.Name, "Dysk HDD", result.DiskQuantity, result.SelectedDisk.Price);
                            if (result.SelectedUps != null) AddRow(result.SelectedUps.Name, $"UPS (podtrzymanie ~{result.Input.UpsRuntimeMinutes} min)", result.UpsQuantity, result.SelectedUps.Price);

                            // Kable i akcesoria
                            if (result.SelectedCable != null) AddRow(result.SelectedCable.Name, $"Przewód (szac. {result.EstimatedCableMeters}m)", result.CableQuantity, result.SelectedCable.Price);
                            if (result.SelectedTray != null) AddRow(result.SelectedTray.Name, "Korytka/Listwy", result.TrayMeters, result.SelectedTray.Price);
                            if (result.SelectedClips != null) AddRow(result.SelectedClips.Name, "Akcesoria montażowe kabli", result.ClipsQuantity, result.SelectedClips.Price);
                            if (result.SelectedScrews != null) AddRow(result.SelectedScrews.Name, "Elementy złączne", result.ScrewsQuantity, result.SelectedScrews.Price);

                            // Usługa
                            if (result.AssemblyCost > 0)
                            {
                                table.Cell().Text($"{i++}.");
                                table.Cell().Text("Usługa montażu i konfiguracji").Bold();
                                table.Cell().AlignRight().Text("1");
                                table.Cell().AlignRight().Text($"{result.AssemblyCost:N2}");
                            }

                            table.Footer(f => f.Cell().ColumnSpan(4).PaddingTop(10).AlignRight().Text($"SUMA BRUTTO: {result.TotalPrice:N2} PLN").FontSize(14).Bold().FontColor(Colors.Red.Medium));
                        });

                        col.Item().PaddingTop(20).Text("Informacje dodatkowe:").Bold();
                        col.Item().Text($"• Szacowana długość okablowania: {result.EstimatedCableMeters} m");
                        col.Item().Text($"• Zapotrzebowanie na dysk: {result.EstimatedStorageTB} TB");
                        if (result.SelectedUps == null && result.Input.NeedUps)
                            col.Item().Text("• UWAGA: Brak odpowiedniego zasilacza UPS w aktualnej ofercie.").FontColor(Colors.Red.Medium);
                    });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", "Kosztorys.pdf");
        }
    }
}