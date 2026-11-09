using Attendance.Contracts;

namespace Attendance.Core
{
    /// <summary>
    /// Attendance 模組內部使用（給自己的 Controller 用）。
    /// 和 Organization.Core.IEmployeeService 一樣，介面是 public、實作是 internal。
    /// </summary>
    public interface IShiftAssignmentService
    {
        Task<CreateShiftAssignmentResult> CreateAsync(CreateShiftAssignmentRequest request);
        Task<ShiftAssignmentDto?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<ShiftAssignmentDto>> SearchAsync(Guid? employeeId, DateOnly? from, DateOnly? to);
    }
}
