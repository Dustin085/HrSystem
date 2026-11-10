namespace Attendance.Core
{
    internal readonly record struct TimeRange
    {
        public TimeOnly Start { get; }
        public TimeOnly End { get; }

        public TimeSpan Duration => End - Start;

        public TimeRange(TimeOnly start, TimeOnly end)
        {
            if (!IsValid(start, end))
            {
                throw new ArgumentException("時間區間的結束時間必須晚於開始時間", nameof(end));
            }
            Start = start;
            End = end;
        }

        /// <summary>
        /// 時段視為 [開始, 結束)，所以 08:00-12:00 和 12:00-17:00 不算重疊。
        /// </summary>
        public bool Overlaps(TimeRange other)
        {
            return Start < other.End
                && other.Start < End;
        }

        public static bool TryCreate(TimeOnly start, TimeOnly end, out TimeRange timeRange)
        {
            if (!IsValid(start, end))
            {
                timeRange = default;
                return false;
            }
            timeRange = new TimeRange(start, end);
            return true;
        }

        private static bool IsValid(TimeOnly start, TimeOnly end)
        {
            return end > start;
        }
    }
}
