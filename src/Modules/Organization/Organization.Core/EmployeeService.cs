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
        private readonly EmployeeRepository _repository;
        private readonly TimeProvider _timeProvider;

        public EmployeeService(EmployeeRepository repository,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
        {
            // HireDate 已由 [Required] 在進到這裡之前驗證過
            var employee = new Employee(
                await _repository.NextEmployeeNumberAsync(),
                request.Name,
                request.HireDate!.Value);

            await _repository.AddAsync(employee);

            return employee.ToDto(_timeProvider.GetToday());
        }

        public async Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            var employee = await _repository.FindAsync(id);
            return employee?.ToDto(_timeProvider.GetToday());
        }

        public async Task<IReadOnlyList<EmployeeDto>> ListAsync()
        {
            // 「今天」只取一次：寫在 lambda 裡的話，每個員工都會重新讀一次時鐘
            var today = _timeProvider.GetToday();
            var employees = await _repository.ListAsync();
            return employees.Select(e => e.ToDto(today)).ToList();
        }

        /// <returns>找不到員工時回傳 null。姓名格式已由 request 的 [Required, StringLength] 驗證過。</returns>
        public async Task<EmployeeDto?> RenameAsync(Guid id, string name)
        {
            var employee = await _repository.FindAsync(id);
            if (employee is null)
            {
                return null;
            }

            employee.Rename(name);
            await _repository.SaveChangesAsync();

            return employee.ToDto(_timeProvider.GetToday());
        }

        /// <summary>
        /// 業務規則不通過時用 Result 回傳，不讓 entity 的例外傳到 controller：
        /// 先在這裡檢查，全部通過才呼叫 employee.Terminate()；entity 的例外只是最後一道防線。
        /// TODO: 「檢查 → 離職」不是 atomic，兩個同時進來的請求理論上可能都通過檢查，後寫入的會覆蓋先寫入的離職日。
        /// 之後可以加上 rowversion 欄位（EF Core 的 IsRowVersion()）做樂觀並行控制。
        /// </summary>
        public async Task<Result<EmployeeDto, TerminateEmployeeError>> TerminateAsync(Guid id, DateOnly terminationDate)
        {
            var employee = await _repository.FindAsync(id);
            if (employee is null)
            {
                return TerminateEmployeeError.EmployeeNotFound;
            }

            if (employee.TerminationDate is not null)
            {
                return TerminateEmployeeError.AlreadyTerminated;
            }

            if (terminationDate < employee.HireDate)
            {
                return TerminateEmployeeError.TerminationDateBeforeHireDate;
            }

            employee.Terminate(terminationDate);
            await _repository.SaveChangesAsync();

            return employee.ToDto(_timeProvider.GetToday());
        }
    }
}
