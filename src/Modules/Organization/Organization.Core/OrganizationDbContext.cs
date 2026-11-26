using Microsoft.EntityFrameworkCore;
using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// Organization 模組自己的 DbContext。
    ///
    /// 每個模組各有一個 DbContext、使用自己的 schema（organization）和自己的 migration，
    /// 不和其他模組共用資料表，也不建立跨模組的外鍵。之後拆成獨立服務時，可以連同資料表一起帶走。
    ///
    /// 類別是 public（public 的 EmployeeRepository 建構子要接收它，dotnet ef 工具也需要建立它），
    /// 但 DbSet 是 internal，因為 Employee 是 internal。
    /// </summary>
    public sealed class OrganizationDbContext : DbContext
    {
        public const string Schema = "organization";
        internal const string EmployeeNumberSequence = "EmployeeNumbers";

        public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
            : base(options)
        {
        }

        internal DbSet<Employee> Employees => Set<Employee>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            // 員工編號用資料庫的 sequence 產生，多個請求同時新增員工也不會拿到相同的號碼
            modelBuilder.HasSequence<int>(EmployeeNumberSequence).StartsAt(1);

            modelBuilder.Entity<Employee>(builder =>
            {
                builder.ToTable("Employees");

                builder.HasKey(e => e.Id);
                // Id 由 entity 自己產生（Guid.CreateVersion7()），不讓資料庫或 EF Core 產生
                builder.Property(e => e.Id).ValueGeneratedNever();

                builder.Property(e => e.EmployeeNumber)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                builder.HasIndex(e => e.EmployeeNumber).IsUnique();

                builder.Property(e => e.Name)
                    .HasMaxLength(EmployeeConstraints.NameMaxLength);
            });
        }
    }
}
