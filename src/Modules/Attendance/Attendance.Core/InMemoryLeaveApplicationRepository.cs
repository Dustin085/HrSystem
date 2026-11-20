using Attendance.Contracts;

namespace Attendance.Core
{
    internal enum TrySubmitOutcome
    {
        Submitted,
        InvalidStatus,
        Overlapping,
    }

    /// <summary>
    /// 暫時的記憶體儲存，註冊為 Singleton。
    ///
    /// 和 InMemoryShiftAssignmentRepository 相同：
    /// 類別是 public（public 的 LeaveApplicationService 建構子要接收它，CS0051），
    /// 成員是 internal（用到 internal 的 LeaveApplication）。新增方法時請維持 internal。
    /// </summary>
    public sealed class InMemoryLeaveApplicationRepository
    {
        private readonly List<LeaveApplication> _applications = [];
        private readonly Lock _lock = new();

        internal void Add(LeaveApplication application)
        {
            lock (_lock)
            {
                _applications.Add(application);
            }
        }

        internal LeaveApplication? Find(Guid id)
        {
            lock (_lock)
            {
                return _applications.Find(a => a.Id == id);
            }
        }

        /// <summary>
        /// 在同一個 lock 裡完成「檢查狀態、檢查重疊、送出」。
        ///
        /// 一般來說 Repository 不執行業務操作，這裡呼叫 leave.Submit() 是為了在記憶體儲存中保持原子性：
        /// 如果檢查和送出分開，兩個同時進來的請求可能都通過檢查，結果送出兩筆重疊的申請。
        /// 改用資料庫時，會改成由 Service 在交易中完成這些步驟。
        /// </summary>
        /// <returns>
        /// Submitted：已送出；InvalidStatus：目前狀態不能送出（例如同時有另一個請求先送出了）；
        /// Overlapping：和同一員工另一筆待審核或已核准的申請重疊。後兩種情況都不會修改申請。
        /// </returns>
        internal TrySubmitOutcome TrySubmitWithoutOverlap(LeaveApplication leave)
        {
            lock (_lock)
            {
                // Service 已經檢查過一次，這裡再檢查是為了處理同一筆申請被同時送出的情況
                if (!leave.CanSubmit)
                {
                    return TrySubmitOutcome.InvalidStatus;
                }

                bool overlaps = _applications.Exists(a =>
                    a.Id != leave.Id
                    && a.Status is LeaveApplicationStatus.Submitted or LeaveApplicationStatus.Approved
                    && a.OverlapsWith(leave));
                if (overlaps)
                {
                    return TrySubmitOutcome.Overlapping;
                }

                leave.Submit();
                return TrySubmitOutcome.Submitted;
            }
        }
    }
}
