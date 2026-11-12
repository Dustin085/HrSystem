using System.ComponentModel.DataAnnotations;
using Organization.Contracts;

namespace Organization.Core
{
    public record RenameEmployeeRequest(
        [Required, StringLength(EmployeeConstraints.NameMaxLength)] string Name);
}
