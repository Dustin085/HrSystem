using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// <see cref="IEmployeeDirectory"/> 的實作，提供給其他模組（例如 Attendance）查詢員工。
    /// </summary>
    internal sealed class EmployeeDirectory : IEmployeeDirectory
    {
        private readonly InMemoryEmployeeRepository _repository;

        public EmployeeDirectory(InMemoryEmployeeRepository repository)
        {
            _repository = repository;
        }

        public Task<bool> ExistsByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Exists(id));
        }

        public Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Find(id)?.ToDto());
        }
    }
}
