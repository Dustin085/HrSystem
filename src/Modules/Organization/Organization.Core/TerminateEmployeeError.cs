namespace Organization.Core
{
    public enum TerminateEmployeeError
    {
        EmployeeNotFound,
        AlreadyTerminated,
        TerminationDateBeforeHireDate,
    }
}
