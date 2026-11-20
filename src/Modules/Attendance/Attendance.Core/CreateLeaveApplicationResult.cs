using Attendance.Contracts;

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

    public sealed class CreateLeaveApplicationResult
    {
        public LeaveApplicationDto? Value { get; }
        public CreateLeaveApplicationError? Error { get; }

        public static CreateLeaveApplicationResult Success(LeaveApplicationDto value) => new(value, null);
        public static CreateLeaveApplicationResult Failure(CreateLeaveApplicationError error) => new(null, error);

        private CreateLeaveApplicationResult(LeaveApplicationDto? value, CreateLeaveApplicationError? error)
        {
            Value = value;
            Error = error;
        }
    }
}
