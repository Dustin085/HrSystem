namespace Organization.Core
{
    internal static class TimeProviderExtensions
    {
        /// <summary>
        /// 伺服器時區的「今天」。
        /// 寫成方法而不是屬性，提醒呼叫端「每呼叫一次就讀一次時鐘」：
        /// 同一個操作裡應該只取一次，存成區域變數再傳下去，避免午夜前後前後不一致。
        /// </summary>
        public static DateOnly GetToday(this TimeProvider timeProvider)
        {
            return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        }
    }
}
