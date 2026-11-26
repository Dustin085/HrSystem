using Attendance.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Core
{
    /// <summary>
    /// Attendance 模組自己的 DbContext。
    ///
    /// 每個模組各有一個 DbContext、使用自己的 schema（attendance）和自己的 migration。
    /// EmployeeId 只存 Guid，不建立指向 organization.Employees 的外鍵：
    /// 跨模組的資料一律透過 IEmployeeDirectory 查詢，之後拆成獨立服務時才不會被資料庫綁在一起。
    ///
    /// 類別是 public（public 的 Repository 建構子要接收它，dotnet ef 工具也需要建立它），
    /// 但 DbSet 是 internal，因為 entity 是 internal。
    /// </summary>
    public sealed class AttendanceDbContext : DbContext
    {
        public const string Schema = "attendance";

        public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
            : base(options)
        {
        }

        internal DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
        internal DbSet<LeaveApplication> LeaveApplications => Set<LeaveApplication>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // 所有 enum 都以英文名稱存成 varchar，資料庫裡看得懂，也和 API 的 JSON 一致。
            // 注意：enum 成員改名就等於改變存進資料庫的值，需要另外寫 migration 轉換舊資料。
            // [Flags] enum 不適合存字串，遇到時請在 OnModelCreating 個別設定 HasConversion<int>()。
            configurationBuilder.Properties<Enum>()
                .HaveConversion<string>()
                .HaveMaxLength(32)
                .AreUnicode(false);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<ShiftAssignment>(builder =>
            {
                builder.ToTable("ShiftAssignments");

                builder.HasKey(a => a.Id);
                // Id 由 entity 自己產生（Guid.CreateVersion7()）
                builder.Property(a => a.Id).ValueGeneratedNever();

                // 欄位順序：沒指定的欄位會排在有指定的後面，complex property 的欄位又固定照字母排，
                // 所以每個欄位都要指定，Start 才會在 End 前面。新增欄位時記得一起編號。
                // 只影響建立資料表時的順序，資料表已經存在時，修改順序不會產生 migration。
                builder.Property(a => a.Id).HasColumnOrder(0);
                builder.Property(a => a.EmployeeId).HasColumnOrder(1);
                builder.Property(a => a.Date).HasColumnOrder(2);

                // TimeRange 是值物件，存成同一張表的兩個欄位（complex property）
                builder.ComplexProperty(a => a.TimeRange, timeRange =>
                {
                    timeRange.Property(t => t.Start).HasColumnName("StartTime").HasColumnOrder(3);
                    timeRange.Property(t => t.End).HasColumnName("EndTime").HasColumnOrder(4);
                });

                // 重疊檢查和查詢都會用「員工 + 日期」篩選
                builder.HasIndex(a => new { a.EmployeeId, a.Date });
            });

            modelBuilder.Entity<LeaveApplication>(builder =>
            {
                builder.ToTable("LeaveApplications");

                builder.HasKey(l => l.Id);
                builder.Property(l => l.Id).ValueGeneratedNever();

                // 欄位順序的說明見 ShiftAssignment
                builder.Property(l => l.Id).HasColumnOrder(0);
                builder.Property(l => l.EmployeeId).HasColumnOrder(1);
                builder.Property(l => l.Type).HasColumnOrder(2);

                builder.ComplexProperty(l => l.DateRange, dateRange =>
                {
                    dateRange.Property(d => d.Start).HasColumnName("StartDate").HasColumnOrder(3);
                    dateRange.Property(d => d.End).HasColumnName("EndDate").HasColumnOrder(4);
                });

                builder.Property(l => l.Reason)
                    .HasMaxLength(LeaveApplicationConstraints.MaxReasonLength)
                    .HasColumnOrder(5);
                builder.Property(l => l.Status).HasColumnOrder(6);
                builder.Property(l => l.ReviewerId).HasColumnOrder(7);
                builder.Property(l => l.ReviewedAt).HasColumnOrder(8);
                builder.Property(l => l.RejectReason)
                    .HasMaxLength(LeaveApplicationConstraints.MaxRejectReasonLength)
                    .HasColumnOrder(9);

                // 送出時的重疊檢查會用「員工 + 狀態」篩選
                builder.HasIndex(l => new { l.EmployeeId, l.Status });
            });
        }
    }
}
