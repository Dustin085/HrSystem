using Attendance.Contracts;

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

    public sealed class SubmitLeaveApplicationResult
    {
        public LeaveApplicationDto? Value { get; }
        public SubmitLeaveApplicationError? Error { get; }

        public static SubmitLeaveApplicationResult Success(LeaveApplicationDto value) => new(value, null);
        public static SubmitLeaveApplicationResult Failure(SubmitLeaveApplicationError error) => new(null, error);

        private SubmitLeaveApplicationResult(LeaveApplicationDto? value, SubmitLeaveApplicationError? error)
        {
            Value = value;
            Error = error;
        }
    }
}
