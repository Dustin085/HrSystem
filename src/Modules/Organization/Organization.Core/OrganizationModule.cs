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

            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();

            return services;
        }
    }
}
