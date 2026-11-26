using System.Text.Json.Serialization;
using Attendance.Core;
using HrSystem.Host.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Organization.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    // 明確告訴 MVC 去各模組的組件找 Controller
    .AddApplicationPart(typeof(OrganizationModule).Assembly)
    .AddApplicationPart(typeof(AttendanceModule).Assembly)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenAPI（Swashbuckle）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HrSystem API", Version = "v1" });

    // 用 Action 名稱當 operationId，產生的 Angular 方法名稱才會是 createEmployee()，
    // 而不是 apiEmployeesPost()。所以 Action 名稱在整個 API 裡必須唯一。
    options.CustomOperationIds(api => api.ActionDescriptor.RouteValues["action"]);

    // 讓 C# 的 nullable 註記反映到 schema 上（string 不可為 null、string? 可為 null）
    options.SupportNonNullableReferenceTypes();

    // 補上 record 欄位的 required 標示，產生的 TypeScript 型別才不會全部是選填
    options.SchemaFilter<RecordRequiredSchemaFilter>();
});

builder.Services.AddSingleton(TimeProvider.System);

// 模組註冊（Attendance 依賴 Organization 提供的 IEmployeeDirectory）
builder.Services.AddOrganizationModule();
builder.Services.AddAttendanceModule();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 開發環境啟動時自動套用 migration，資料庫不存在時也會自動建立。
    // 正式環境不要這樣做：應該在部署流程中執行 migration，避免多台伺服器同時啟動時一起修改資料庫。
    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AttendanceDbContext>().Database.MigrateAsync();
    }

    app.UseSwagger();      // /swagger/v1/swagger.json
    app.UseSwaggerUI();    // /swagger
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
