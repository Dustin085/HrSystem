using System.ComponentModel.DataAnnotations;

namespace Attendance.Contracts
{
    // 全部宣告成 nullable，[Required] 才擋得住「沒傳這個欄位」的情況
    // （理由同 Organization.Contracts.CreateEmployeeRequest.HireDate）
    public record CreateShiftAssignmentRequest(
        [Required] Guid? EmployeeId,
        [Required] DateOnly? Date,
        [Required] TimeOnly? StartTime,
        [Required] TimeOnly? EndTime);
}
