using System.ComponentModel.DataAnnotations;

namespace MonitoringConfigurator.Models
{
    // Dane, które klient wpisuje w formularzu
    public class ConfiguratorInputModel
    {
        [Display(Name = "Liczba kamer")]
        [Range(1, 128, ErrorMessage = "Liczba kamer musi być między 1 a 128")]
        public int CameraCount { get; set; } = 4;

        [Display(Name = "Rozdzielczość kamer (Mpix)")]
        public int ResolutionMp { get; set; } = 4;

        [Display(Name = "Czas archiwizacji (dni)")]
        [Range(1, 90)]
        public int RecordingDays { get; set; } = 14;

        [Display(Name = "Wymagane zasilanie PoE?")]
        public bool NeedPoE { get; set; } = true;

        [Display(Name = "Tryb nagrywania")]
        public string RecordingMode { get; set; } = "motion"; // motion / continuous
    }

    // Wynik do wyświetlenia w podsumowaniu
    public class ConfigurationResult
    {
        public ConfiguratorInputModel Input { get; set; }

        public Product? SelectedCamera { get; set; }
        public int CameraQuantity { get; set; }

        public Product? SelectedNvr { get; set; }
        public int NvrQuantity { get; set; } = 1;

        public Product? SelectedSwitch { get; set; }
        public int SwitchQuantity { get; set; }

        public Product? SelectedDisk { get; set; }
        public int DiskQuantity { get; set; }

        // Statystyki
        public double EstimatedBandwidthMbps { get; set; }
        public double EstimatedStorageTB { get; set; }
        public int EstimatedPoEBudgetW { get; set; }

        public decimal TotalPrice =>
            (SelectedCamera?.Price ?? 0) * CameraQuantity +
            (SelectedNvr?.Price ?? 0) * NvrQuantity +
            (SelectedSwitch?.Price ?? 0) * SwitchQuantity +
            (SelectedDisk?.Price ?? 0) * DiskQuantity;
    }
}