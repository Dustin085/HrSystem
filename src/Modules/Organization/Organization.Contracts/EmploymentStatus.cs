namespace Organization.Contracts
{
    /// <summary>
    /// 任職狀態，依生命週期排序。
    /// 數值明確寫出：enum 的 default 是 0，排第一個的值就是「忘了設定」時會得到的值，
    /// 所以第一個不能是 Active，否則漏設時會悄悄變成「在職」。
    /// API 透過 JsonStringEnumConverter 傳名稱，數值不會出現在 JSON 裡。
    /// </summary>
    public enum EmploymentStatus
    {
        NotYetHired = 0, // 還沒到職
        Active = 1,      // 在職
        Terminated = 2,  // 已離職
    }
}
