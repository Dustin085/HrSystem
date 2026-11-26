using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organization.Contracts;

namespace Organization.Core
{
    public static class OrganizationModule
    {
        /// <summary>
        /// 註冊 Organization 模組。需要設定連線字串 ConnectionStrings:HrSystem。
        /// </summary>
        public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
        {
            services.AddDbContext<OrganizationDbContext>((serviceProvider, options) =>
                options.UseSqlServer(
                    GetConnectionString(serviceProvider),
                    sqlServer =>
                    {
                        // 每個模組的 migration 紀錄放在自己的 schema，互不干擾
                        sqlServer.MigrationsHistoryTable(HistoryRepository.DefaultTableName, OrganizationDbContext.Schema);
                        // 暫時性錯誤（例如死結、連線中斷）自動重試
                        sqlServer.EnableRetryOnFailure();
                    }));

            // Repository 使用 DbContext，所以生命週期和 DbContext 一樣是 Scoped
            services.AddScoped<EmployeeRepository>();

            // 取消 IService 的做法避免過多只有單實作的模板 interface，代價是 Service 必須改成 public，public 的 service 透過專案參照的限制來避免其他模組的使用
            // MVC(.NET Core) 在啟動時掃描組件找 Controller，只會收錄 public 的類別。這樣就不會因為某個 internal 類別剛好繼承了 ControllerBase，就不小心被公開成 API。
            services.AddScoped<EmployeeService>();
            services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();

            return services;
        }

        private static string GetConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("HrSystem")
                ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:HrSystem。");
        }
    }
}
