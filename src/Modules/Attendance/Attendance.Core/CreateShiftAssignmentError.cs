namespace Attendance.Core
{
    public enum CreateShiftAssignmentError
    {
        InvalidTimeRange,
        EmployeeNotFound,
        Overlapping,
        ExceedMaxShiftDuration,
        EmployeeNotEmployedOnDate, // 此員工「排班日當天」不在職(已離職或尚未到職)
    }
}
