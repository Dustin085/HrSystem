namespace Organization.Contracts
{
    public record EmployeeDto(
        Guid Id,
        string EmployeeNumber,
        string Name,
        DateOnly HireDate);
}
