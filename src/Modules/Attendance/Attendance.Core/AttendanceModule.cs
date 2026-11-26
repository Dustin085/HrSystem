using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Core
{
    public static class AttendanceModule
    {
        /// <summary>
        /// 註冊 Attendance 模組。需要設定連線字串 ConnectionStrings:HrSystem。
        /// 依賴 Organization.Contracts.IEmployeeDirectory，Host 必須同時呼叫 AddOrganizationModule()。
        /// </summary>
        public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
        {
            services.AddDbContext<AttendanceDbContext>((serviceProvider, options) =>
                options.UseSqlServer(
                    GetConnectionString(serviceProvider),
                    sqlServer =>
                    {
                        // 每個模組的 migration 紀錄放在自己的 schema，互不干擾
                        sqlServer.MigrationsHistoryTable(HistoryRepository.DefaultTableName, AttendanceDbContext.Schema);
                        // 暫時性錯誤（例如死結、連線中斷）自動重試，Serializable 交易發生死結時也靠它重試
                        sqlServer.EnableRetryOnFailure();
                    }));

            // Repository 使用 DbContext，所以生命週期和 DbContext 一樣是 Scoped
            services.AddScoped<ShiftAssignmentRepository>();
            services.AddScoped<LeaveApplicationRepository>();

            // 和 Organization 相同：不為只有單一實作的 Service 建立介面，直接註冊具體類別。
            // Service 因此必須是 public，但 Attendance.Core 只被 Host 參照，其他模組仍然用不到它。
            services.AddScoped<ShiftAssignmentService>();
            services.AddScoped<LeaveApplicationService>();

            services.AddOptions<AttendanceOptions>()
                .BindConfiguration(AttendanceOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        private static string GetConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("HrSystem")
                ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:HrSystem。");
        }
    }
}
