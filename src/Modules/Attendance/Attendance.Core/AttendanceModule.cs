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
            services.AddSingleton<InMemoryShiftAssignmentRepository>();
            services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();

            return services;
        }
    }
}
