namespace Attendance.Contracts
{
    public record LeaveApplicationDto(
        Guid Id,
        Guid EmployeeId,
        LeaveApplicationType Type,
        LeaveApplicationStatus Status,
        string? Reason,
        DateOnly StartDate,
        DateOnly EndDate,
        int Days, // 包含開始日和結束日
        Guid? ReviewerId,
        DateTimeOffset? ReviewedAt,
        string? RejectReason);
}
