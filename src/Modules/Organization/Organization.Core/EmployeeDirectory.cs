using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// <see cref="IEmployeeDirectory"/> 的實作，提供給其他模組（例如 Attendance）查詢員工。
    /// </summary>
    internal sealed class EmployeeDirectory : IEmployeeDirectory
    {
        private readonly EmployeeRepository _repository;
        private readonly TimeProvider _timeProvider;

        public EmployeeDirectory(EmployeeRepository repository,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        public Task<bool> ExistsByIdAsync(Guid id)
        {
            return _repository.ExistsAsync(id);
        }

        public async Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            var employee = await _repository.FindAsync(id);
            return employee?.ToDto(_timeProvider.GetToday());
        }

        public async Task<EmploymentStatus?> GetEmploymentStatusOnAsync(Guid id, DateOnly date)
        {
            var employee = await _repository.FindAsync(id);
            return employee?.GetEmploymentStatusOn(date);
        }

        public async Task<bool?> IsEmployedThroughoutAsync(Guid id, DateOnly start, DateOnly end)
        {
            if (end < start)
            {
                throw new ArgumentException("結束日不能早於開始日。", nameof(end));
            }
            var employee = await _repository.FindAsync(id);
            return employee?.IsEmployedThroughout(start, end);
        }
    }
}
