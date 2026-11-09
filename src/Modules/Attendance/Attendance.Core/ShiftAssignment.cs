namespace Attendance.Core
{
    /// <summary>
    /// 一筆排班：某位員工在某一天的上班時段。
    /// 目前不支援跨夜班（下班時間必須晚於上班時間）。
    /// </summary>
    internal sealed class ShiftAssignment
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();
        public Guid EmployeeId { get; private set; }
        public DateOnly Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }

        public ShiftAssignment(Guid employeeId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            if (employeeId == Guid.Empty)
            {
                throw new ArgumentException("必須指定員工。", nameof(employeeId));
            }

            if (!IsValidTimeRange(startTime, endTime))
            {
                throw new ArgumentException("下班時間必須晚於上班時間。", nameof(endTime));
            }

            EmployeeId = employeeId;
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
        }

        public static bool IsValidTimeRange(TimeOnly startTime, TimeOnly endTime)
        {
            return endTime > startTime;
        }

        /// <summary>
        /// 同一位員工、同一天、時段有交集就算重疊。
        /// 時段視為 [開始, 結束)，所以 08:00-12:00 和 12:00-17:00 不算重疊。
        /// </summary>
        public bool OverlapsWith(ShiftAssignment other)
        {
            return EmployeeId == other.EmployeeId
                && Date == other.Date
                && StartTime < other.EndTime
                && other.StartTime < EndTime;
        }
    }
}
