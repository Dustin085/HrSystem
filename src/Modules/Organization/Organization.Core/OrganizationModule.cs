using Microsoft.Extensions.DependencyInjection;
using Organization.Contracts;

namespace Organization.Core
{
    public static class OrganizationModule
    {
        public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
        {
            // 記憶體儲存必須是 Singleton，資料才會在請求之間保留
            services.AddSingleton<InMemoryEmployeeRepository>();

            // 取消 IService 的做法避免過多只有單實作的模板 interface，代價是 Service 必須改成 public，public 的 service 透過專案參照的限制來避免其他模組的使用
            // MVC(.NET Core) 在啟動時掃描組件找 Controller，只會收錄 public 的類別。這樣就不會因為某個 internal 類別剛好繼承了 ControllerBase，就不小心被公開成 API。
            services.AddScoped<EmployeeService>();
            services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();

            return services;
        }
    }
}
