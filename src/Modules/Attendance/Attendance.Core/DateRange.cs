namespace Attendance.Core
{
    internal readonly record struct DateRange
    {
        public DateOnly Start { get; }
        public DateOnly End { get; }

        public int Duration => End.DayNumber - Start.DayNumber + 1; // +1 因為要算上起始日當天

        public DateRange(DateOnly start, DateOnly end)
        {
            if (!IsValid(start, end))
            {
                throw new ArgumentException("日期區間的結束日不能早於開始日。", nameof(end));
            }
            Start = start;
            End = end;
        }

        /// <summary>
        /// 時段視為 [開始, 結束]，所以 1-2 和 2-3 算重疊。
        /// </summary>
        public bool Overlaps(DateRange other)
        {
            return Start <= other.End
                && other.Start <= End;
        }

        public static bool TryCreate(DateOnly start, DateOnly end, out DateRange dateRange)
        {
            if (!IsValid(start, end))
            {
                dateRange = default;
                return false;
            }
            dateRange = new DateRange(start, end);
            return true;
        }

        private static bool IsValid(DateOnly start, DateOnly end)
        {
            return end >= start;
        }
    }
}
