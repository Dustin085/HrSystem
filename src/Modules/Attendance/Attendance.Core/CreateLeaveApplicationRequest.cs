using Attendance.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Attendance.Core
{
    // 全部宣告成 nullable，[Required] 才擋得住「沒傳這個欄位」的情況
    // （理由同 Organization.Core.CreateEmployeeRequest.HireDate）
    public record CreateLeaveApplicationRequest(
        [Required] Guid? EmployeeId,
        [Required] LeaveApplicationType? Type,
        [Required] DateOnly? Start,
        [Required] DateOnly? End,
        [StringLength(LeaveApplicationConstraints.MaxReasonLength)] string? Reason);
}
