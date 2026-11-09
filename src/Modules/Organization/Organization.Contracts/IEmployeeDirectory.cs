namespace Organization.Contracts
{
    /// <summary>
    /// 提供給其他模組使用的唯讀員工查詢介面。
    /// 新增、修改員工屬於 Organization 模組內部的責任，不放在這裡。
    /// </summary>
    public interface IEmployeeDirectory
    {
        Task<bool> ExistsByIdAsync(Guid id);
        Task<EmployeeDto?> GetByIdAsync(Guid id);
    }
}
