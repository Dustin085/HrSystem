using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Core
{
    public static class AttendanceModule
    {
        /// <summary>
        /// 註冊 Attendance 模組。
        /// 依賴 Organization.Contracts.IEmployeeDirectory，Host 必須同時呼叫 AddOrganizationModule()。
        /// </summary>
        public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
        {
            // 記憶體儲存必須是 Singleton，資料才會在請求之間保留
            services.AddSingleton<InMemoryShiftAssignmentRepository>();

            // 和 Organization 相同：不為只有單一實作的 Service 建立介面，直接註冊具體類別。
            // Service 因此必須是 public，但 Attendance.Core 只被 Host 參照，其他模組仍然用不到它。
            services.AddScoped<ShiftAssignmentService>();

            return services;
        }
    }
}
