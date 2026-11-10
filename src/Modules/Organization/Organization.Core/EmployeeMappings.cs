using Organization.Contracts;

namespace Organization.Core
{
    internal static class EmployeeMappings
    {
        public static EmployeeDto ToDto(this Employee employee)
        {
            return new EmployeeDto(
                employee.Id,
                employee.EmployeeNumber,
                employee.Name,
                employee.HireDate,
                employee.TerminationDate);
        }
    }
}
