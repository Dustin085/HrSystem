using System.Text.Json.Serialization;
using Attendance.Core;
using HrSystem.Host.OpenApi;
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
    app.UseSwagger();      // /swagger/v1/swagger.json
    app.UseSwaggerUI();    // /swagger
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
