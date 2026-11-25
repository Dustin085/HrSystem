namespace Attendance.Core
{
    public enum SubmitLeaveApplicationError
    {
        LeaveApplicationNotFound,
        InvalidStatus, // 只有草稿或已退回的申請可以送出
        EmployeeNotFound,
        EmployeeNotEmployedOnDateRange, // 請假的某一日員工不在職
        Overlapping, // 和同一員工另一筆待審核或已核准的申請重疊
    }
}
