using System.ComponentModel.DataAnnotations;

namespace Organization.Contracts
{
    public record CreateEmployeeRequest(
        [Required, StringLength(EmployeeConstraints.NameMaxLength)] string Name,
        // 宣告成 nullable 才能讓 [Required] 擋下「沒傳到職日」的情況，
        // 否則 JSON 少了這個欄位時會默默變成 0001-01-01。
        [Required] DateOnly? HireDate);
}
