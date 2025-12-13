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
            if (!ModelState.IsValid) return View("Index", input);

            var result = new ConfigurationResult { Input = input, CameraQuantity = input.CameraCount };

            // 1. Dobór Kamery (najtańsza spełniająca kryteria rozdzielczości)
            result.SelectedCamera = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Camera && p.ResolutionMp >= input.ResolutionMp)
                .OrderBy(p => p.Price) // Bierzemy najkorzystniejszą cenowo
                .FirstOrDefaultAsync();

            if (result.SelectedCamera == null)
            {
                ModelState.AddModelError("", "Nie znaleziono kamer o zadanych parametrach w bazie.");
                return View("Index", input);
            }

            // 2. Obliczenia techniczne
            // Szacujemy: Bitrate = Rozdzielczość * 1.5 Mbps (H.265). Jeśli ciągłe nagrywanie to 100%, detekcja 40%.
            double baseBitratePerCam = (result.SelectedCamera.ResolutionMp ?? 2) * 1.5;
            double activityFactor = input.RecordingMode == "continuous" ? 1.0 : 0.4;

            result.EstimatedBandwidthMbps = Math.Round(baseBitratePerCam * input.CameraCount, 2);

            // Pojemność: (Mbps / 8) * 3600s * 24h * Dni * activity
            double dailyGB = (baseBitratePerCam / 8) * 3600 * 24 / 1024 * activityFactor;
            result.EstimatedStorageTB = Math.Round((dailyGB * input.RecordingDays * input.CameraCount) / 1024, 2);

            // PoE
            int wattsPerCam = result.SelectedCamera.PoeBudgetW ?? 10; // domyślnie 10W jeśli brak danych
            result.EstimatedPoEBudgetW = wattsPerCam * input.CameraCount;

            // 3. Dobór Rejestratora (NVR)
            // Musi mieć: wystarczająco kanałów, wystarczający bitrate i zatoki na dyski
            result.SelectedNvr = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Recorder
                       && p.Channels >= input.CameraCount
                       && p.MaxBandwidthMbps >= result.EstimatedBandwidthMbps)
                .OrderBy(p => p.Price)
                .FirstOrDefaultAsync();

            // 4. Dobór Switcha (jeśli wymagany i NVR nie ma wbudowanego switcha na tyle portów)
            // Upraszczamy: zawsze dobieramy switch, jeśli zaznaczono PoE (chyba że NVR ma PoE - tu zakładamy oddzielny switch dla elastyczności)
            if (input.NeedPoE)
            {
                result.SelectedSwitch = await _ctx.Products
                    .Where(p => p.Category == ProductCategory.Switch
                           && p.Ports >= input.CameraCount
                           && p.PoeBudgetW >= result.EstimatedPoEBudgetW)
                    .OrderBy(p => p.Price)
                    .FirstOrDefaultAsync();

                if (result.SelectedSwitch != null) result.SwitchQuantity = 1;
                // Jeśli 1 switch za mały, w realnym scenariuszu dodalibyśmy pętlę, tu uproszczenie
            }

            // 5. Dobór Dysku
            // Szukamy dysku, który jest większy niż wymagana pojemność / liczba zatok w NVR
            int bays = result.SelectedNvr?.DiskBays ?? 1;
            double neededPerDisk = result.EstimatedStorageTB / bays;

            result.SelectedDisk = await _ctx.Products
                .Where(p => p.Category == ProductCategory.Disk && p.StorageTB >= neededPerDisk)
                .OrderBy(p => p.Price)
                .FirstOrDefaultAsync();

            if (result.SelectedDisk != null)
            {
                // Jeśli jeden dysk wystarczy na całość
                if (result.SelectedDisk.StorageTB >= result.EstimatedStorageTB)
                    result.DiskQuantity = 1;
                else
                    result.DiskQuantity = bays; // Wypełniamy zatoki
            }

            return View("Summary", result);
        }

        [HttpPost]
        public IActionResult GeneratePdf(string jsonResult)
        {
            // Odtwórz obiekt z JSON (uproszczone przekazywanie danych między widokami)
            // W produkcji lepiej zapisać konfigurację w bazie i przekazać ID.
            // Tutaj dla demonstracji użyjemy TempData lub ponownie przeliczymy, 
            // ale najprościej: pobierzemy dane z formularza ukrytego w widoku Summary.

            // UWAGA: QuestPDF wymaga licencji Community (darmowa dla firm <1M przychodu)
            QuestPDF.Settings.License = LicenseType.Community;

            var result = System.Text.Json.JsonSerializer.Deserialize<ConfigurationResult>(jsonResult);
            if (result == null) return RedirectToAction("Index");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("MonitoringConfigurator").FontSize(20).SemiBold().FontColor(Colors.Red.Medium);
                            col.Item().Text($"Data wyceny: {DateTime.Now:yyyy-MM-dd}");
                        });
                        row.ConstantItem(100).Height(50).Placeholder(); // Tutaj mogłoby być logo
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text("Zestawienie produktów").FontSize(16).SemiBold();
                      

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Lp.");
                                header.Cell().Text("Produkt");
                                header.Cell().Text("Model");
                                header.Cell().AlignRight().Text("Ilość");
                                header.Cell().AlignRight().Text("Cena jedn.");
                            });

                            int i = 1;
                            // Helper do dodawania wierszy
                            void AddRow(Product? p, int qty)
                            {
                                if (p == null || qty == 0) return;
                                table.Cell().Text($"{i++}.");
                                table.Cell().Text(p.Name);
                                table.Cell().Text($"{p.Brand} {p.Model}");
                                table.Cell().AlignRight().Text(qty.ToString());
                                table.Cell().AlignRight().Text($"{p.Price:N2} zł");
                            }

                            AddRow(result.SelectedCamera, result.CameraQuantity);
                            AddRow(result.SelectedNvr, result.NvrQuantity);
                            AddRow(result.SelectedSwitch, result.SwitchQuantity);
                            AddRow(result.SelectedDisk, result.DiskQuantity);

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(5).PaddingTop(10).AlignRight().Text($"SUMA: {result.TotalPrice:N2} PLN").FontSize(14).Bold();
                            });
                        });

                        col.Item().PaddingTop(20).Text("Parametry systemu:").FontSize(14).SemiBold();
                        col.Item().Text($"Przepustowość: {result.EstimatedBandwidthMbps} Mbps");
                        col.Item().Text($"Wymagana przestrzeń: {result.EstimatedStorageTB} TB");
                        col.Item().Text($"Budżet PoE: {result.EstimatedPoEBudgetW} W");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Wygenerowano automatycznie przez System MonitoringConfigurator.");
                    });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Wycena_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}