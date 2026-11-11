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
        public TimeRange TimeRange { get; private set; }

        public TimeSpan Duration => TimeRange.Duration;

        public ShiftAssignment(Guid employeeId, DateOnly date, TimeRange timeRange)
        {
            if (employeeId == Guid.Empty)
            {
                throw new ArgumentException("必須指定員工。", nameof(employeeId));
            }

            if (timeRange == default)
            {
                throw new ArgumentException("建立排班時，必須使用有效的時間區間", nameof(timeRange));
            }

            EmployeeId = employeeId;
            Date = date;
            TimeRange = timeRange;
        }

        /// <summary>
        /// 同一位員工、同一天、時段有交集就算重疊。
        /// 時段視為 [開始, 結束)，所以 08:00-12:00 和 12:00-17:00 不算重疊。
        /// </summary>
        public bool OverlapsWith(ShiftAssignment other)
        {
            return EmployeeId == other.EmployeeId
                && Date == other.Date
                && TimeRange.Overlaps(other.TimeRange);
        }
    }
}
