using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Core
{
    /// <summary>
    /// 排班的資料存取，使用 EF Core。註冊為 Scoped，和 DbContext 的生命週期相同。
    ///
    /// 類別是 public、成員是 internal，這是刻意的：
    /// public 的 ShiftAssignmentService 建構子要接收它，所以型別必須是 public（CS0051）；
    /// 但它的方法只給模組內部使用，而且用到 internal 的 ShiftAssignment，所以成員一律 internal。
    /// 新增方法時請維持 internal。
    /// </summary>
    public sealed class ShiftAssignmentRepository
    {
        private readonly AttendanceDbContext _db;

        public ShiftAssignmentRepository(AttendanceDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 「檢查重疊」和「新增」在同一個 Serializable 交易裡完成，
        /// 否則兩個同時進來的請求可能都通過檢查，結果存進兩筆重疊的班。
        ///
        /// Serializable 交易同時執行時，SQL Server 可能判定死結並中止其中一個；
        /// 被中止的交易會由 execution strategy（EnableRetryOnFailure）整段重試，
        /// 重試時就會看到另一個請求已經新增的班，回傳重疊。
        /// </summary>
        /// <returns>有重疊時回傳 false，且不會新增。</returns>
        internal Task<bool> TryAddWithoutOverlapAsync(ShiftAssignment assignment)
        {
            // 使用會重試的 execution strategy 時，自己開的交易必須包在 ExecuteAsync 裡，重試時才能整段重來
            var strategy = _db.Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // 寫成查詢條件，不能直接用 ShiftAssignment.OverlapsWith()：EF Core 無法把 C# 方法轉成 SQL。
                // 規則和 OverlapsWith 相同：同一位員工、同一天、時段 [開始, 結束) 有交集。
                var start = assignment.TimeRange.Start;
                var end = assignment.TimeRange.End;
                bool overlaps = await _db.ShiftAssignments.AnyAsync(a =>
                    a.EmployeeId == assignment.EmployeeId
                    && a.Date == assignment.Date
                    && a.TimeRange.Start < end
                    && start < a.TimeRange.End);
                if (overlaps)
                {
                    return false;
                }

                _db.ShiftAssignments.Add(assignment);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            });
        }

        internal async Task<ShiftAssignment?> FindAsync(Guid id)
        {
            return await _db.ShiftAssignments.FindAsync(id);
        }

        internal async Task<IReadOnlyList<ShiftAssignment>> SearchAsync(Guid? employeeId, DateOnly? from, DateOnly? to)
        {
            var query = _db.ShiftAssignments.AsNoTracking();

            // 只加上有指定的條件，產生的 SQL 比較簡潔，也比較容易使用索引
            if (employeeId is not null)
            {
                query = query.Where(a => a.EmployeeId == employeeId);
            }
            if (from is not null)
            {
                query = query.Where(a => a.Date >= from);
            }
            if (to is not null)
            {
                query = query.Where(a => a.Date <= to);
            }

            return await query
                .OrderBy(a => a.Date)
                .ThenBy(a => a.TimeRange.Start)
                .ToListAsync();
        }
    }
}
