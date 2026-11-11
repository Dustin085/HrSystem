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
        private readonly TimeProvider _timeProvider;

        public EmployeeService(InMemoryEmployeeRepository repository,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        public Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
        {
            // HireDate 已由 [Required] 在進到這裡之前驗證過
            var employee = new Employee(
                _repository.NextEmployeeNumber(),
                request.Name,
                request.HireDate!.Value);

            _repository.Add(employee);

            return Task.FromResult(employee.ToDto(_timeProvider.GetToday()));
        }

        public Task<EmployeeDto?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Find(id)?.ToDto(_timeProvider.GetToday()));
        }

        public Task<IReadOnlyList<EmployeeDto>> ListAsync()
        {
            // 「今天」只取一次：寫在 lambda 裡的話，每個員工都會重新讀一次時鐘
            var today = _timeProvider.GetToday();
            IReadOnlyList<EmployeeDto> result = _repository.List().Select(e => e.ToDto(today)).ToList();
            return Task.FromResult(result);
        }

        /// <returns>找不到員工時回傳 null。姓名格式已由 request 的 [Required, StringLength] 驗證過。</returns>
        public Task<EmployeeDto?> RenameAsync(Guid id, string name)
        {
            var employee = _repository.Find(id);
            if (employee is null)
            {
                return Task.FromResult<EmployeeDto?>(null);
            }

            employee.Rename(name);
            return Task.FromResult<EmployeeDto?>(employee.ToDto(_timeProvider.GetToday()));
        }

        /// <summary>
        /// 業務規則不通過時用 Result 回傳，不讓 entity 的例外傳到 controller：
        /// 先在這裡檢查，全部通過才呼叫 employee.Terminate()；entity 的例外只是最後一道防線。
        /// 注意：記憶體儲存的 Employee 是共用物件，「檢查 → 離職」不是 atomic，
        /// 兩個同時進來的請求理論上可能都通過檢查。階段 3 換成 EF Core 時用樂觀並行控制處理。
        /// </summary>
        public Task<TerminateEmployeeResult> TerminateAsync(Guid id, DateOnly terminationDate)
        {
            var employee = _repository.Find(id);
            if (employee is null)
            {
                return Task.FromResult(TerminateEmployeeResult.Failure(TerminateEmployeeError.EmployeeNotFound));
            }

            if (employee.TerminationDate is not null)
            {
                return Task.FromResult(TerminateEmployeeResult.Failure(TerminateEmployeeError.AlreadyTerminated));
            }

            if (terminationDate < employee.HireDate)
            {
                return Task.FromResult(TerminateEmployeeResult.Failure(TerminateEmployeeError.TerminationDateBeforeHireDate));
            }

            employee.Terminate(terminationDate);
            return Task.FromResult(TerminateEmployeeResult.Success(employee.ToDto(_timeProvider.GetToday())));
        }
    }
}
