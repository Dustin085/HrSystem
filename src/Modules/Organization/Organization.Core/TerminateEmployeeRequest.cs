using System.ComponentModel.DataAnnotations;

namespace Organization.Core
{
    public record TerminateEmployeeRequest(
        [Required] DateOnly? TerminationDate);
}
