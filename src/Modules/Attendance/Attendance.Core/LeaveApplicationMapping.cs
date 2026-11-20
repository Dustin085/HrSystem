using Attendance.Contracts;

namespace Attendance.Core
{
    internal static class LeaveApplicationMapping
    {
        public static LeaveApplicationDto ToDto(this LeaveApplication leave)
        {
            return new(
                leave.Id,
                leave.EmployeeId,
                leave.Type,
                leave.Status,
                leave.Reason,
                leave.DateRange.Start,
                leave.DateRange.End,
                leave.DateRange.Duration,
                leave.ReviewerId,
                leave.ReviewedAt,
                leave.RejectReason);
        }
    }
}
