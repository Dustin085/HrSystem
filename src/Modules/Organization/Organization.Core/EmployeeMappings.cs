using Organization.Contracts;

namespace Organization.Core
{
    internal static class EmployeeMappings
    {
        public static EmployeeDto ToDto(this Employee employee, DateOnly date)
        {
            return new EmployeeDto(
                employee.Id,
                employee.EmployeeNumber,
                employee.Name,
                employee.GetEmploymentStatusOn(date),
                employee.HireDate,
                employee.TerminationDate);
        }
    }
}
