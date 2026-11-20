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
        /// <summary>
        /// 查詢 <paramref name="date"/> 當天此 <paramref name="id"/> 的員工的在職狀態
        /// </summary>
        /// <param name="id">員工 id</param>
        /// <param name="date">想查詢的日子</param>
        /// <returns>此員工當天的在職狀態，null 代表找不到員工</returns>
        Task<EmploymentStatus?> GetEmploymentStatusOnAsync(Guid id, DateOnly date);

        /// <summary>
        /// 查詢此 <paramref name="id"/> 的員工在 <paramref name="start"/> 到 <paramref name="end"/>（包含頭尾）的每一天是否都在職
        /// </summary>
        /// <param name="id">員工 id</param>
        /// <param name="start">開始日</param>
        /// <param name="end">結束日，不能早於開始日</param>
        /// <returns>true：每一天都在職；false：至少有一天不在職；null：找不到員工</returns>
        Task<bool?> IsEmployedThroughoutAsync(Guid id, DateOnly start, DateOnly end);
    }
}
