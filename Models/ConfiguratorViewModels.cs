using System.ComponentModel.DataAnnotations;

namespace MonitoringConfigurator.Models
{
    public enum BuildingType
    {
        [Display(Name = "Dom jednorodzinny")] Home,
        [Display(Name = "Biuro / Sklep")] Office,
        [Display(Name = "Magazyn / Hala")] Warehouse,
        [Display(Name = "Parking / Plac")] Parking
    }

    public enum CameraEnvironment
    {
        [Display(Name = "Tylko wewnątrz")] Indoor,
        [Display(Name = "Tylko na zewnątrz")] Outdoor,
        [Display(Name = "Mieszane (Wew/Zew)")] Mixed
    }

    public enum InstallationType
    {
        [Display(Name = "Natynkowa (korytka)")] Surface,
        [Display(Name = "Podtynkowa (bruzdy/sufity)")] Flush
    }

    public class ConfiguratorInputModel
    {
        // --- O OBIEKCIE ---
        [Display(Name = "Rodzaj obiektu")]
        public BuildingType Building { get; set; }

        [Display(Name = "Powierzchnia (m²)")]
        [Range(10, 100000)]
        public int AreaM2 { get; set; } = 150;

        [Display(Name = "Typ instalacji")]
        public InstallationType InstallType { get; set; }

        // --- KAMERY ---
        [Display(Name = "Kamery zewnętrzne")]
        [Range(0, 128)]
        public int OutdoorCamCount { get; set; } = 4;

        [Display(Name = "Kamery wewnętrzne")]
        [Range(0, 128)]
        public int IndoorCamCount { get; set; } = 2;

        public int TotalCameras => OutdoorCamCount + IndoorCamCount;

        [Display(Name = "Jakość obrazu")]
        public int ResolutionMp { get; set; } = 4;

        [Display(Name = "Archiwizacja (dni)")]
        [Range(1, 90)]
        public int RecordingDays { get; set; } = 14;

        // --- DODATKI ---
        [Display(Name = "Zasilanie PoE")]
        public bool NeedPoE { get; set; } = true;

        [Display(Name = "Okablowanie")]
        public bool NeedCabling { get; set; } = true;

        [Display(Name = "Zasilanie awaryjne (UPS)")]
        public bool NeedUps { get; set; } = false;

        [Display(Name = "Czas podtrzymania (min)")]
        public int UpsRuntimeMinutes { get; set; } = 15;

        [Display(Name = "Usługa montażu")]
        public bool NeedAssembly { get; set; } = false;
    }

    public class ConfigurationResult
    {
        public ConfiguratorInputModel Input { get; set; }

        // Sprzęt główny
        public Product? SelectedOutdoorCam { get; set; }
        public Product? SelectedIndoorCam { get; set; }
        public Product? SelectedNvr { get; set; }
        public int NvrQuantity { get; set; } = 1;
        public Product? SelectedSwitch { get; set; }
        public int SwitchQuantity { get; set; }
        public Product? SelectedDisk { get; set; }
        public int DiskQuantity { get; set; }
        public Product? SelectedCable { get; set; }
        public int CableQuantity { get; set; }
        public Product? SelectedUps { get; set; }
        public int UpsQuantity { get; set; }

        // Akcesoria montażowe (automatyczne)
        public Product? SelectedTray { get; set; } // Korytka (dla natynkowej)
        public int TrayMeters { get; set; }

        public Product? SelectedClips { get; set; } // Uchwyty
        public int ClipsQuantity { get; set; } // Paczki

        public Product? SelectedScrews { get; set; } // Wkręty
        public int ScrewsQuantity { get; set; } // Paczki

        // Usługa
        public decimal AssemblyCost { get; set; }

        // Statystyki
        public double EstimatedBandwidthMbps { get; set; }
        public double EstimatedStorageTB { get; set; }
        public int EstimatedPoEBudgetW { get; set; }
        public int EstimatedCableMeters { get; set; }

        public decimal TotalPrice =>
            ((SelectedOutdoorCam?.Price ?? 0) * Input.OutdoorCamCount) +
            ((SelectedIndoorCam?.Price ?? 0) * Input.IndoorCamCount) +
            ((SelectedNvr?.Price ?? 0) * NvrQuantity) +
            ((SelectedSwitch?.Price ?? 0) * SwitchQuantity) +
            ((SelectedDisk?.Price ?? 0) * DiskQuantity) +
            ((SelectedCable?.Price ?? 0) * CableQuantity) +
            ((SelectedUps?.Price ?? 0) * UpsQuantity) +
            ((SelectedTray?.Price ?? 0) * TrayMeters) +
            ((SelectedClips?.Price ?? 0) * ClipsQuantity) +
            ((SelectedScrews?.Price ?? 0) * ScrewsQuantity) +
            AssemblyCost;
    }
}