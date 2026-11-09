using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HrSystem.Host.OpenApi
{
    /// <summary>
    /// 讓 positional record 的 schema 正確標示 required，產生的 TypeScript 型別才不會全部變成選填。
    ///
    /// Swashbuckle 預設讀不到 record 主建構子參數上的 [Required]，
    /// 也不會把不可為 null 的值型別（Guid、DateOnly…）標成 required。規則：
    /// 1. 參數有 [Required] → required，並且不可為 null（例如 request 的 DateOnly? HireDate）
    /// 2. 參數型別不可為 null（string、Guid、DateOnly…）→ required（例如回傳的 DTO）
    /// 3. 其他（例如 string?、Guid? 且沒有 [Required]）→ 選填、可為 null
    /// </summary>
    internal sealed class RecordRequiredSchemaFilter : ISchemaFilter
    {
        private readonly NullabilityInfoContext _nullability = new();

        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema target || target.Properties is null || target.Properties.Count == 0)
            {
                return;
            }

            // positional record 的主建構子：參數最多的 public 建構子
            var constructor = context.Type
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor is null)
            {
                return;
            }

            foreach (var parameter in constructor.GetParameters())
            {
                var propertyName = target.Properties.Keys
                    .FirstOrDefault(k => string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (propertyName is null)
                {
                    continue;
                }

                var hasRequiredAttribute = parameter.GetCustomAttribute<RequiredAttribute>() is not null;
                var isNotNullable = _nullability.Create(parameter).WriteState == NullabilityState.NotNull;

                if (!hasRequiredAttribute && !isNotNullable)
                {
                    continue;
                }

                target.Required ??= new HashSet<string>();
                target.Required.Add(propertyName);

                // [Required] 的 nullable 參數：在 API 契約上就是「必填且不可為 null」
                if (target.Properties[propertyName] is OpenApiSchema property && property.Type is { } type)
                {
                    property.Type = type & ~JsonSchemaType.Null;
                }
            }
        }
    }
}
