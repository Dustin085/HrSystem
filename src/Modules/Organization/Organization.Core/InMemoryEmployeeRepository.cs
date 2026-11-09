using System.Collections.Concurrent;

namespace Organization.Core
{
    /// <summary>
    /// 暫時的記憶體儲存，之後換成 EF Core 時只需替換這個類別。
    /// 註冊為 Singleton，所以所有操作都必須是執行緒安全的。
    /// </summary>
    internal sealed class InMemoryEmployeeRepository
    {
        private readonly ConcurrentDictionary<Guid, Employee> _employees = new();
        private int _lastEmployeeNumber;

        public string NextEmployeeNumber()
        {
            return Interlocked.Increment(ref _lastEmployeeNumber).ToString("D4");
        }

        public void Add(Employee employee)
        {
            if (!_employees.TryAdd(employee.Id, employee))
            {
                throw new InvalidOperationException($"員工 {employee.Id} 已存在。");
            }
        }

        public Employee? Find(Guid id)
        {
            return _employees.GetValueOrDefault(id);
        }

        public bool Exists(Guid id)
        {
            return _employees.ContainsKey(id);
        }

        public IReadOnlyList<Employee> List()
        {
            return _employees.Values.OrderBy(e => e.EmployeeNumber).ToList();
        }
    }
}
