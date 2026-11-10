using System.ComponentModel.DataAnnotations;

namespace Organization.Contracts
{
    public record RenameEmployeeRequest(
        [Required, StringLength(EmployeeConstraints.NameMaxLength)] string Name);
}
