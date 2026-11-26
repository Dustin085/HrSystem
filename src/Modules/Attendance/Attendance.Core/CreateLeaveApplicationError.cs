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
        // 沒有 Overlapping：建立草稿時不檢查重疊，送出時才檢查（見 SubmitLeaveApplicationError）
    }
}
