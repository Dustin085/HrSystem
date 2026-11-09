namespace Organization.Core
{
    internal sealed class Employee
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();
        public string EmployeeNumber { get; private set; }
        public string Name { get; private set; }
        public DateOnly HireDate { get; private set; }

        public Employee(string employeeNumber, string name, DateOnly hireDate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            EmployeeNumber = employeeNumber;
            Name = name;
            HireDate = hireDate;
        }
    }
}
