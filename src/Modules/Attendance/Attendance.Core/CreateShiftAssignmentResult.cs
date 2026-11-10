using Attendance.Contracts;

namespace Attendance.Core
{
    public enum CreateShiftAssignmentError
    {
        InvalidTimeRange,
        EmployeeNotFound,
        Overlapping,
        ExceedMaxShiftDuration,
    }

    /// <summary>
    /// 新增排班的結果。業務規則不通過屬於正常情況，用回傳值表達，不丟例外。
    /// </summary>
    public sealed class CreateShiftAssignmentResult
    {
        public ShiftAssignmentDto? Value { get; }
        public CreateShiftAssignmentError? Error { get; }

        private CreateShiftAssignmentResult(ShiftAssignmentDto? value, CreateShiftAssignmentError? error)
        {
            Value = value;
            Error = error;
        }

        public static CreateShiftAssignmentResult Success(ShiftAssignmentDto value) => new(value, null);

        public static CreateShiftAssignmentResult Failure(CreateShiftAssignmentError error) => new(null, error);
    }
}
