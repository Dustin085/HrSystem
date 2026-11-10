namespace Organization.Contracts
{
    /// <summary>
    /// 員工資料的限制。Request 的驗證 attribute 和 Employee entity 共用同一份數值，
    /// 放在 Contracts 是因為 Core 參考得到 Contracts，反過來不行。
    /// </summary>
    public static class EmployeeConstraints
    {
        public const int NameMaxLength = 50;
    }
}
