using Organization.Contracts;

namespace Organization.Core
{
    public enum TerminateEmployeeError
    {
        EmployeeNotFound,
        AlreadyTerminated,
        TerminationDateBeforeHireDate,
    }

    /// <summary>
    /// 員工離職的結果。業務規則不通過屬於正常情況，用回傳值表達，不丟例外。
    /// </summary>
    public sealed class TerminateEmployeeResult
    {
        public EmployeeDto? Value { get; }
        public TerminateEmployeeError? Error { get; }

        private TerminateEmployeeResult(EmployeeDto? value, TerminateEmployeeError? error)
        {
            Value = value;
            Error = error;
        }

        public static TerminateEmployeeResult Success(EmployeeDto value) => new(value, null);

        public static TerminateEmployeeResult Failure(TerminateEmployeeError error) => new(null, error);
    }
}
