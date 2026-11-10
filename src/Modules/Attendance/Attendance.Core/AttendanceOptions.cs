using System.ComponentModel.DataAnnotations;

namespace Attendance.Core
{
    public sealed class AttendanceOptions
    {
        public const string SectionName = "Attendance";

        [Range(typeof(TimeSpan), "00:30:00", "1.00:00:00")] // 30 mins ~ 1 day
        public TimeSpan MaxShiftDuration { get; init; } = new TimeSpan(12, 0, 0);
    }
}
