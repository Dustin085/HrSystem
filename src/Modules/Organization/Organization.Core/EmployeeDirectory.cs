using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// <see cref="IEmployeeDirectory"/> 的實作，提供給其他模組（例如 Attendance）查詢員工。
    /// </summary>
    internal sealed class EmployeeDirectory : IEmployeeDirectory
    {
        private readonly InMemoryEmployeeRepository _repository;
        private readonly TimeProvider _timeProvider;

        public EmployeeDirectory(InMemoryEmployeeRepository repository,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        public Task<bool> ExistsByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Exists(id));
        }

        public Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Find(id)?.ToDto(_timeProvider.GetToday()));
        }

        public Task<EmploymentStatus?> GetEmploymentStatusOnAsync(Guid id, DateOnly date)
        {
            var employee = _repository.Find(id);
            if (employee is null)
            {
                return Task.FromResult<EmploymentStatus?>(null);
            }
            return Task.FromResult<EmploymentStatus?>(employee.GetEmploymentStatusOn(date));
        }
    }
}
