namespace Attendance.Core
{
    /// <summary>
    /// 暫時的記憶體儲存，註冊為 Singleton。
    /// 「檢查重疊」和「新增」必須在同一個 lock 裡完成，
    /// 否則兩個同時進來的請求可能都通過檢查，結果存進兩筆重疊的班。
    /// </summary>
    internal sealed class InMemoryShiftAssignmentRepository
    {
        private readonly List<ShiftAssignment> _assignments = [];
        private readonly Lock _lock = new();

        /// <returns>有重疊時回傳 false，且不會新增。</returns>
        public bool TryAddWithoutOverlap(ShiftAssignment assignment)
        {
            lock (_lock)
            {
                if (_assignments.Exists(a => a.OverlapsWith(assignment)))
                {
                    return false;
                }

                _assignments.Add(assignment);
                return true;
            }
        }

        public ShiftAssignment? Find(Guid id)
        {
            lock (_lock)
            {
                return _assignments.Find(a => a.Id == id);
            }
        }

        public IReadOnlyList<ShiftAssignment> Search(Guid? employeeId, DateOnly? from, DateOnly? to)
        {
            lock (_lock)
            {
                return _assignments
                    .Where(a => employeeId is null || a.EmployeeId == employeeId)
                    .Where(a => from is null || a.Date >= from)
                    .Where(a => to is null || a.Date <= to)
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.StartTime)
                    .ToList();
            }
        }
    }
}
