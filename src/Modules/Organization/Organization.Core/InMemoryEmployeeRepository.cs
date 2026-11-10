using System.Collections.Concurrent;

namespace Organization.Core
{
    /// <summary>
    /// 暫時的記憶體儲存，之後換成 EF Core 時只需替換這個類別。
    /// 註冊為 Singleton，所以所有操作都必須是執行緒安全的。
    ///
    /// 類別是 public、成員是 internal，這是刻意的：
    /// public 的 EmployeeService 建構子要接收它，所以型別必須是 public（CS0051）；
    /// 但它的方法只給模組內部使用，而且用到 internal 的 Employee，所以成員一律 internal。
    /// 新增方法時請維持 internal。
    /// </summary>
    public sealed class InMemoryEmployeeRepository
    {
        private readonly ConcurrentDictionary<Guid, Employee> _employees = new();
        private int _lastEmployeeNumber;

        internal string NextEmployeeNumber()
        {
            return Interlocked.Increment(ref _lastEmployeeNumber).ToString("D4");
        }

        internal void Add(Employee employee)
        {
            if (!_employees.TryAdd(employee.Id, employee))
            {
                throw new InvalidOperationException($"員工 {employee.Id} 已存在。");
            }
        }

        internal Employee? Find(Guid id)
        {
            return _employees.GetValueOrDefault(id);
        }

        internal bool Exists(Guid id)
        {
            return _employees.ContainsKey(id);
        }

        internal IReadOnlyList<Employee> List()
        {
            return _employees.Values.OrderBy(e => e.EmployeeNumber).ToList();
        }
    }
}
