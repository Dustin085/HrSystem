namespace Organization.Contracts
{
    public record EmployeeDto(
        Guid Id,
        string EmployeeNumber,
        string Name,
        EmploymentStatus Status, // 查詢當下（伺服器時區的今天）的任職狀態，每次查詢時計算，不是儲存的值
        DateOnly HireDate,
        DateOnly? TerminationDate);
}
