using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringConfigurator.Models
{
    [NotMapped]
    public class Configuration
    {
        [Range(1, 512)]
        public int CameraCount { get; set; } = 8;

        [Range(1, 32)]
        public double RequiredResolutionMp { get; set; } = 4;

        [Range(1, 60)]
        public int FrameRateFps { get; set; } = 15;

        [Range(1, 365)]
        public int RetentionDays { get; set; } = 14;

        [Range(0, 100)]
        public int PercentOutdoor { get; set; } = 50;

        [Range(0, 200)]
        public int RequiredIrRangeM { get; set; } = 30;

        [Range(0, 1000)]
        public int CableLengthPerCameraM { get; set; } = 30;

        [Range(0, 100)]
        public int ExpectedMotionPercent { get; set; } = 25;

        public bool UseAnalytics { get; set; } = false;
        public bool UseRaid { get; set; } = false;

        [Range(1, 120)]
        public int UptimeMinutesUPS { get; set; } = 15;

        [Range(1, 100)]
        public int PoePerCameraW { get; set; } = 12;
    }
}
