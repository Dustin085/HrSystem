using Microsoft.EntityFrameworkCore;

namespace Organization.Core
{
    /// <summary>
    /// 員工的資料存取，使用 EF Core。註冊為 Scoped，和 DbContext 的生命週期相同。
    ///
    /// 類別是 public、成員是 internal，這是刻意的：
    /// public 的 EmployeeService 建構子要接收它，所以型別必須是 public（CS0051）；
    /// 但它的方法只給模組內部使用，而且用到 internal 的 Employee，所以成員一律 internal。
    /// 新增方法時請維持 internal。
    /// </summary>
    public sealed class EmployeeRepository
    {
        private readonly OrganizationDbContext _db;

        public EmployeeRepository(OrganizationDbContext db)
        {
            _db = db;
        }

        internal async Task<string> NextEmployeeNumberAsync()
        {
            // 不能用 SingleAsync()：EF Core 會把 SQL 包成子查詢，而 NEXT VALUE FOR 不能放在子查詢裡。
            // ToListAsync() 不會改寫 SQL，所以先取回清單再取唯一的一筆。
            var values = await _db.Database
                .SqlQuery<int>($"SELECT NEXT VALUE FOR [organization].[EmployeeNumbers] AS [Value]")
                .ToListAsync();

            return values.Single().ToString("D4");
        }

        /// <summary>新增員工並立即儲存。</summary>
        internal async Task AddAsync(Employee employee)
        {
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
        }

        /// <summary>取得員工。回傳的物件會被追蹤，修改後呼叫 <see cref="SaveChangesAsync"/> 就會寫回資料庫。</summary>
        internal async Task<Employee?> FindAsync(Guid id)
        {
            return await _db.Employees.FindAsync(id);
        }

        internal Task<bool> ExistsAsync(Guid id)
        {
            return _db.Employees.AnyAsync(e => e.Id == id);
        }

        internal async Task<IReadOnlyList<Employee>> ListAsync()
        {
            // 只用來顯示，不會修改，所以不需要追蹤
            return await _db.Employees
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();
        }

        internal Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}
