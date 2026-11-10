using Organization.Contracts;

namespace Organization.Core
{
    internal sealed class Employee
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();
        public string EmployeeNumber { get; private set; }
        public string Name { get; private set; }
        public DateOnly HireDate { get; private set; }
        public DateOnly? TerminationDate { get; private set; } // 離職日，null 代表在職

        public Employee(string employeeNumber, string name, DateOnly hireDate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);
            EnsureValidName(name);

            EmployeeNumber = employeeNumber;
            Name = name;
            HireDate = hireDate;
        }

        public void Rename(string name)
        {
            EnsureValidName(name);

            Name = name;
        }

        public void Terminate(DateOnly terminationDate)
        {
            // 物件「目前的狀態」不允許這個操作 → InvalidOperationException
            if (TerminationDate is not null)
            {
                throw new InvalidOperationException("已離職員工不可再次離職。");
            }

            // 「傳進來的參數」不合法 → ArgumentException
            if (terminationDate < HireDate)
            {
                throw new ArgumentException("離職日不能早於到職日。", nameof(terminationDate));
            }

            TerminationDate = terminationDate;
        }

        /// <summary>
        /// 建構子和 Rename 共用的姓名規則。
        /// Request 上的 [Required, StringLength] 只保護 API 輸入；這裡保護的是 entity 本身，
        /// 不論是 controller、背景工作或事件處理呼叫，都一定會檢查。
        /// </summary>
        private static void EnsureValidName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (name.Length > EmployeeConstraints.NameMaxLength)
            {
                throw new ArgumentException($"姓名最多 {EmployeeConstraints.NameMaxLength} 字。", nameof(name));
            }
        }
    }
}
