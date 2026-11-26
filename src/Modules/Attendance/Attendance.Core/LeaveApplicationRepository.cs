using System.Data;
using Attendance.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Core
{
    internal enum TrySubmitOutcome
    {
        Submitted,
        InvalidStatus,
        Overlapping,
    }

    /// <summary>
    /// 請假申請的資料存取，使用 EF Core。註冊為 Scoped，和 DbContext 的生命週期相同。
    ///
    /// 和 ShiftAssignmentRepository 相同：
    /// 類別是 public（public 的 LeaveApplicationService 建構子要接收它，CS0051），
    /// 成員是 internal（用到 internal 的 LeaveApplication）。新增方法時請維持 internal。
    /// </summary>
    public sealed class LeaveApplicationRepository
    {
        private readonly AttendanceDbContext _db;

        public LeaveApplicationRepository(AttendanceDbContext db)
        {
            _db = db;
        }

        /// <summary>新增申請並立即儲存。</summary>
        internal async Task AddAsync(LeaveApplication application)
        {
            _db.LeaveApplications.Add(application);
            await _db.SaveChangesAsync();
        }

        /// <summary>取得申請。回傳的物件會被追蹤，<see cref="TrySubmitWithoutOverlapAsync"/> 會使用同一個物件。</summary>
        internal async Task<LeaveApplication?> FindAsync(Guid id)
        {
            return await _db.LeaveApplications.FindAsync(id);
        }

        /// <summary>
        /// 在同一個 Serializable 交易裡完成「檢查狀態、檢查重疊、送出」。
        ///
        /// 一般來說 Repository 不執行業務操作，這裡呼叫 leave.Submit() 是為了讓三個步驟在同一個交易中完成：
        /// 如果分開，兩個同時進來的請求可能都通過檢查，結果送出兩筆重疊的申請。
        ///
        /// Serializable 交易同時執行時，SQL Server 可能判定死結並中止其中一個；
        /// 被中止的交易會由 execution strategy（EnableRetryOnFailure）整段重試。
        /// </summary>
        /// <param name="leave">由 <see cref="FindAsync"/> 取得、正在被追蹤的申請。</param>
        /// <returns>
        /// Submitted：已送出；InvalidStatus：目前狀態不能送出（例如同時有另一個請求先送出了）；
        /// Overlapping：和同一員工另一筆待審核或已核准的申請重疊。後兩種情況都不會修改申請。
        /// </returns>
        internal Task<TrySubmitOutcome> TrySubmitWithoutOverlapAsync(LeaveApplication leave)
        {
            // 使用會重試的 execution strategy 時，自己開的交易必須包在 ExecuteAsync 裡，重試時才能整段重來
            var strategy = _db.Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // 在交易中重新讀取，取得最新的狀態。
                // Service 檢查狀態時還沒有開始交易，那時讀到的資料可能已經被其他請求修改。
                await _db.Entry(leave).ReloadAsync();
                if (!leave.CanSubmit)
                {
                    return TrySubmitOutcome.InvalidStatus;
                }

                // 寫成查詢條件，不能直接用 LeaveApplication.OverlapsWith()：EF Core 無法把 C# 方法轉成 SQL。
                // 規則：同一位員工、排除自己、只看待審核和已核准、日期 [開始, 結束] 有交集。
                var id = leave.Id;
                var employeeId = leave.EmployeeId;
                var start = leave.DateRange.Start;
                var end = leave.DateRange.End;
                bool overlaps = await _db.LeaveApplications.AnyAsync(a =>
                    a.Id != id
                    && a.EmployeeId == employeeId
                    && (a.Status == LeaveApplicationStatus.Submitted || a.Status == LeaveApplicationStatus.Approved)
                    && a.DateRange.Start <= end
                    && start <= a.DateRange.End);
                if (overlaps)
                {
                    return TrySubmitOutcome.Overlapping;
                }

                leave.Submit();
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return TrySubmitOutcome.Submitted;
            });
        }
    }
}
