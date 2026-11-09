using Organization.Contracts;

namespace Organization.Core
{
    /// <summary>
    /// Organization 模組內部使用的員工操作（給自己的 Controller 用）。
    /// 必須是 public，因為 public 的 Controller 建構子不能接收 internal 型別；
    /// 實作 <see cref="EmployeeService"/> 仍然是 internal。
    /// 其他模組只參照 Organization.Contracts，所以拿不到這個介面。
    /// </summary>
    public interface IEmployeeService
    {
        Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request);
        Task<EmployeeDto?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<EmployeeDto>> ListAsync();
    }
}
