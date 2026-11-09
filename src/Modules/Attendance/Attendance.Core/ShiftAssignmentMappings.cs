using Attendance.Contracts;

namespace Attendance.Core
{
    internal static class ShiftAssignmentMappings
    {
        public static ShiftAssignmentDto ToDto(this ShiftAssignment assignment)
        {
            return new ShiftAssignmentDto(
                assignment.Id,
                assignment.EmployeeId,
                assignment.Date,
                assignment.StartTime,
                assignment.EndTime);
        }
    }
}
