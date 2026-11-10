using System.ComponentModel.DataAnnotations;

namespace Organization.Contracts
{
    public record TerminateEmployeeRequest(
        [Required] DateOnly? TerminationDate);
}
