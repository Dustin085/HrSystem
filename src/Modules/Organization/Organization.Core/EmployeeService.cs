using Organization.Contracts;

namespace Organization.Core
{
    internal sealed class EmployeeService : IEmployeeService
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
