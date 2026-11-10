using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// Organization 模組內部使用的員工操作（給自己的 Controller 用）。
    /// 必須是 public，因為 public 的 Controller 建構子不能接收 internal 型別；
    /// 其他模組只參照 Organization.Contracts，所以拿不到這個 class。
    /// </summary>
    public sealed class EmployeeService
    {
        private readonly InMemoryEmployeeRepository _repository;

        public EmployeeService(InMemoryEmployeeRepository repository)
        {
            _repository = repository;
        }

        public Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
        {
            // HireDate 已由 [Required] 在進到這裡之前驗證過
            var employee = new Employee(
                _repository.NextEmployeeNumber(),
                request.Name,
                request.HireDate!.Value);

            _repository.Add(employee);

            return Task.FromResult(employee.ToDto());
        }

        public Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Find(id)?.ToDto());
        }

        public Task<IReadOnlyList<EmployeeDto>> ListAsync()
        {
            IReadOnlyList<EmployeeDto> result = _repository.List().Select(e => e.ToDto()).ToList();
            return Task.FromResult(result);
        }
    }
}
