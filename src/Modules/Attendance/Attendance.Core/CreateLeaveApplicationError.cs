namespace Attendance.Core
{
    public enum CreateLeaveApplicationError
    {
        InvalidDateRange,
        EmployeeNotFound,
        ExceedMaxPersonalLeaveDays,
        EmployeeNotEmployedOnDateRange, // 請假的某一日員工不在職
        UnknownLeaveType,
        PersonalLeaveWithoutReason,
    }
}
