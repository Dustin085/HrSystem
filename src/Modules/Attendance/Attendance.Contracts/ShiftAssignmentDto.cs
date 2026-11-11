namespace Attendance.Contracts
{
    public record ShiftAssignmentDto(
        Guid Id,
        Guid EmployeeId,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        TimeSpan Duration);
}
