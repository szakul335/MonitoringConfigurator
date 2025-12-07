using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;
using MonitoringConfigurator.Services;

// PDF
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

// WORD
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using PdfDocument = QuestPDF.Fluent.Document;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace MonitoringConfigurator.Controllers
{
    [Authorize] // Konfigurator tylko dla zalogowanych
    public class ConfiguratorController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _ctx;
        private readonly IConfiguratorService _svc;

        public ConfiguratorController(AppDbContext ctx, IConfiguratorService svc, IWebHostEnvironment env)
        {
            _env = env;
            _ctx = ctx;
            _svc = svc;
        }

        [HttpGet]
        public IActionResult Index() => View(new Configuration());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Propose(Configuration input)
        {
            if (!ModelState.IsValid) return View("Index", input);

            var products = await _ctx.Products.AsNoTracking().ToListAsync();
            var result = _svc.Propose(input, products);

            if (result is null)
            {
                TempData["Error"] = "Nie znaleziono pasujących komponentów dla podanych parametrów.";
                return View("Index", input);
            }

            TempData["LastInput"] = JsonSerializer.Serialize(input);
            TempData["LastResult"] = JsonSerializer.Serialize(result);
            TempData["LastGeneratedAt"] = DateTime.UtcNow.ToString("o");

            ViewBag.Result = result;
            return View("Result");
        }

        // ============ EKSPORT: PDF ============
        [HttpGet]
        public IActionResult ExportPdf()
        {
            if (!TryLoad(out var input, out var result, out var generatedAt))
            {
                TempData["Error"] = "Brak danych do eksportu. Wykonaj konfigurację ponownie.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = BuildPdf(input, result, generatedAt);
            SaveToUserDocuments(bytes, "Specyfikacja.pdf");
            return File(bytes, "application/pdf", "Specyfikacja.pdf");
        }

        // ============ EKSPORT: DOCX ============
        [HttpGet]
        public IActionResult ExportDocx()
        {
            if (!TryLoad(out var input, out var result, out var generatedAt))
            {
                TempData["Error"] = "Brak danych do eksportu. Wykonaj konfigurację ponownie.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = BuildDocx(input, result, generatedAt);
            SaveToUserDocuments(bytes, "Specyfikacja.docx");
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Specyfikacja.docx");
        }

        private void SaveToUserDocuments(byte[] bytes, string fileName)
        {
            try
            {
                // Zapis na dysk (folder użytkownika)
                var userName = User?.Identity?.Name ?? "anonymous";
                var safe = System.Text.RegularExpressions.Regex.Replace(userName, @"[^a-zA-Z0-9_\-\.@]", "_");
                var basePath = System.IO.Path.Combine(_env.WebRootPath, "documents", safe);
                System.IO.Directory.CreateDirectory(basePath);
                var dest = System.IO.Path.Combine(basePath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}");
                System.IO.File.WriteAllBytes(dest, bytes);

                // Opcjonalnie: zapis w bazie jako UserDocument (jeśli użytkownik jest zalogowany)
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var format = System.IO.Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(format))
                        format = "bin";

                    var doc = new UserDocument
                    {
                        UserId = userId,
                        Title = fileName,
                        Format = format,
                        Content = bytes,
                        CreatedUtc = DateTime.UtcNow
                    };

                    _ctx.UserDocuments.Add(doc);
                    _ctx.SaveChanges();
                }
            }
            catch
            {
                // ciche pominięcie błędów IO/DB – nie blokujemy pobrania pliku
            }
        }

        // ================== Helpers ==================

        private bool TryLoad(out Configuration input, out ConfigResult result, out DateTime generatedAt)
        {
            input = default!;
            result = default!;
            generatedAt = DateTime.UtcNow;

            if (TempData["LastInput"] is not string si || TempData["LastResult"] is not string sr)
                return false;

            // utrzymaj dane do kolejnego requestu (eksport)
            TempData.Keep("LastInput");
            TempData.Keep("LastResult");
            TempData.Keep("LastGeneratedAt");

            input = JsonSerializer.Deserialize<Configuration>(si)!;
            result = JsonSerializer.Deserialize<ConfigResult>(sr)!;

            if (TempData["LastGeneratedAt"] is string sg && DateTime.TryParse(sg, out var dt))
                generatedAt = dt;

            return true;
        }

        private static decimal SafeDec(object? v)
        {
            if (v == null) return 0m;
            if (v is decimal d) return d;
            if (v is double dd) return (decimal)dd;
            if (decimal.TryParse(v.ToString(), out var r)) return r;
            return 0m;
        }

        private static int SafeInt(object? v)
        {
            if (v == null) return 0;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var r)) return r;
            return 0;
        }

        // ================== PDF (QuestPDF) ==================
        private byte[] BuildPdf(Configuration input, ConfigResult res, DateTime generatedAt)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = PdfDocument.Create(doc =>
            {
                doc.Page(p =>
                {
                    p.Margin(36);
                    p.Size(PageSizes.A4);

                    p.Header().Text("Specyfikacja konfiguracji").FontSize(18).SemiBold();
                    p.Footer().AlignRight().Text($"Wygenerowano: {generatedAt:yyyy-MM-dd HH:mm}");

                    p.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("Zestaw produktów").FontSize(12).SemiBold();

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); // Nazwa
                                c.RelativeColumn(2); // Kategoria
                                c.RelativeColumn(1); // Ilość
                                c.RelativeColumn(3); // Parametry
                            });

                            // Nagłówek
                            t.Header(h =>
                            {
                                h.Cell().Text("Nazwa").SemiBold();
                                h.Cell().Text("Kategoria").SemiBold();
                                h.Cell().AlignRight().Text("Ilość").SemiBold();
                                h.Cell().AlignRight().Text("Parametry").SemiBold();
                            });

                            // Kamera
                            t.Cell().Text(res.Camera?.Name ?? "-");
                            t.Cell().Text("Kamera");
                            t.Cell().AlignRight().Text(res.CameraCount.ToString());
                            t.Cell().AlignRight().Text(
                                $"{res.Camera?.Brand} {res.Camera?.Model}, {res.Camera?.ResolutionMp} MP, IR {res.Camera?.IrRangeM} m");

                            // NVR
                            t.Cell().Text(res.Nvr?.Name ?? "-");
                            t.Cell().Text("Rejestrator (NVR)");
                            t.Cell().AlignRight().Text("-");
                            t.Cell().AlignRight().Text(
                                $"Kanały {res.Nvr?.Channels}, Przepustowość {res.Nvr?.MaxBandwidthMbps} Mbps");

                            // Switch
                            t.Cell().Text(res.Switch?.Name ?? "-");
                            t.Cell().Text("Switch PoE");
                            t.Cell().AlignRight().Text("-");
                            t.Cell().AlignRight().Text(
                                $"Porty {res.Switch?.Ports}, Budżet PoE {res.Switch?.PoeBudgetW} W");

                            // HDD
                            if (res.Hdds != null && res.Hdds.Any())
                            {
                                foreach (var h in res.Hdds)
                                {
                                    t.Cell().Text(h.Product?.Name ?? "-");
                                    t.Cell().Text("HDD");
                                    t.Cell().AlignRight().Text(h.Qty.ToString());
                                    t.Cell().AlignRight().Text($"{h.Product?.StorageTB} TB/szt.");
                                }
                            }

                            // Kabel
                            if (res.CableRolls != null)
                            {
                                t.Cell().Text(res.CableRolls.Product?.Name ?? "-");
                                t.Cell().Text("Kabel");
                                t.Cell().AlignRight().Text(res.CableRolls.Qty.ToString());
                                t.Cell().AlignRight().Text(
                                    $"{res.CableRolls.Product?.RollLengthM} m / rolka");
                            }

                            // UPS
                            if (res.Ups != null)
                            {
                                t.Cell().Text(res.Ups?.Name ?? "-");
                                t.Cell().Text("UPS");
                                t.Cell().AlignRight().Text("1");
                                t.Cell().AlignRight().Text($"{res.Ups?.UpsVA} VA");
                            }
                        });

                        col.Item().Text("Podsumowanie").FontSize(12).SemiBold();
                        col.Item().Text(
                            $"Bitrate: {res.TotalBandwidthMbps} Mbps | " +
                            $"Pojemność: {res.TotalStorageTB} TB | " +
                            $"Budżet PoE: {res.TotalPoeW} W | " +
                            $"Cena orient.: {res.TotalPrice:C}"
                        );

                        // KLAUZULA O CENACH – PDF
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("Informacja: ").SemiBold();
                            t.Span("Wszystkie podane ceny mają charakter poglądowy i nie stanowią oferty handlowej. Ostateczna wycena może ulec zmianie w zależności od dostępności produktów, aktualnych cen dostawców oraz szczegółowych warunków realizacji.");
                        });
                        
                    });
                });
            }).GeneratePdf();

            return pdf;
        }

        // ================== DOCX (OpenXML) ==================
        private byte[] BuildDocx(Configuration input, ConfigResult res, DateTime generatedAt)
        {
            using var ms = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new W.Document(new W.Body());
                var body = main.Document.Body;

                // Tytuł i data
                body.Append(new W.Paragraph(new W.Run(new W.Text("Specyfikacja konfiguracji"))));
                body.Append(new W.Paragraph(new W.Run(new W.Text($"Wygenerowano: {generatedAt:yyyy-MM-dd HH:mm}"))));
                body.Append(new W.Paragraph(new W.Run(new W.Text("Zestaw produktów"))));

                // Pomocnicza funkcja do wierszy tabeli (zwraca W.TableRow)
                W.TableRow Row(params string[] cells)
                {
                    var tr = new W.TableRow();
                    foreach (var c in cells)
                        tr.Append(new W.TableCell(new W.Paragraph(new W.Run(new W.Text(c ?? "")))));
                    return tr;
                }

                var table = new W.Table(
                    new W.TableProperties(
                        new W.TableBorders(
                            new W.TopBorder { Val = W.BorderValues.Single, Size = 4 },
                            new W.BottomBorder { Val = W.BorderValues.Single, Size = 4 },
                            new W.LeftBorder { Val = W.BorderValues.Single, Size = 4 },
                            new W.RightBorder { Val = W.BorderValues.Single, Size = 4 },
                            new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4 },
                            new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4 }
                        )
                    )
                );

                table.Append(Row("Nazwa", "Kategoria", "Ilość", "Parametry"));

                // Kamera
                table.Append(Row(
                    res.Camera?.Name ?? "-", "Kamera", res.CameraCount.ToString(),
                    $"{res.Camera?.Brand} {res.Camera?.Model}, {res.Camera?.ResolutionMp} MP, IR {res.Camera?.IrRangeM} m"));

                // NVR
                table.Append(Row(
                    res.Nvr?.Name ?? "-", "Rejestrator (NVR)", "-",
                    $"Kanały {res.Nvr?.Channels}, Przepustowość {res.Nvr?.MaxBandwidthMbps} Mbps"));

                // Switch
                table.Append(Row(
                    res.Switch?.Name ?? "-", "Switch PoE", "-",
                    $"Porty {res.Switch?.Ports}, Budżet PoE {res.Switch?.PoeBudgetW} W"));

                // HDD
                if (res.Hdds != null && res.Hdds.Any())
                {
                    foreach (var h in res.Hdds)
                        table.Append(Row(
                            h.Product?.Name ?? "-", "HDD", h.Qty.ToString(),
                            $"{h.Product?.StorageTB} TB/szt."));
                }

                // Kabel
                if (res.CableRolls != null)
                {
                    table.Append(Row(
                        res.CableRolls.Product?.Name ?? "-", "Kabel", res.CableRolls.Qty.ToString(),
                        $"{res.CableRolls.Product?.RollLengthM} m / rolka"));
                }

                // UPS
                if (res.Ups != null)
                {
                    table.Append(Row(
                        res.Ups?.Name ?? "-", "UPS", "1", $"{res.Ups?.UpsVA} VA"));
                }

                body.Append(table);

                // Podsumowanie
                body.Append(new W.Paragraph(new W.Run(new W.Text(
                    $"Bitrate: {res.TotalBandwidthMbps} Mbps | Pojemność: {res.TotalStorageTB} TB | Budżet PoE: {res.TotalPoeW} W | Cena orient.: {res.TotalPrice:C}"
                ))));

                // KLAUZULA O CENACH – DOCX
                var disclaimerParagraph = new W.Paragraph();
                disclaimerParagraph.Append(
                    new W.Run(
                        new W.RunProperties(new W.Bold()),
                        new W.Text("Informacja: ")
                    ),
                    new W.Run(
                        new W.Text(
                            "Wszystkie podane ceny mają charakter poglądowy i nie stanowią oferty handlowej. Ostateczna wycena może ulec zmianie w zależności od dostępności produktów, aktualnych cen dostawców oraz szczegółowych warunków realizacji."
                        )
                    )
                );
                body.Append(disclaimerParagraph);

                main.Document.Save();
            }

            return ms.ToArray();
        }
    }
}

