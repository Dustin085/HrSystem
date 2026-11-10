using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HrSystem.Host.OpenApi
{
    /// <summary>
    /// 修正 swagger.json 裡 record 欄位的「必填」標示。
    ///
    /// 【這個類別只影響 swagger.json】
    ///   它不會改變 API 的任何執行行為：不參與 JSON 轉換、不做驗證、不會讓請求回傳 400。
    ///   真正擋下錯誤請求的是 request record 上的 [Required] 等 attribute 和 [ApiController]。
    ///   這裡只是讓「API 文件」正確描述那些規則。
    ///
    /// 【為什麼需要它】
    ///   swagger.json 是前端 `npm run gen:api` 產生 TypeScript 型別的依據。
    ///   Swashbuckle 預設有兩件事做不好：
    ///   1. 讀不到寫在 record「建構子參數」上的 [Required]
    ///      （positional record 的 attribute 是標在參數上，Swashbuckle 只看屬性）
    ///   2. 不會把不可為 null 的值型別（Guid、DateOnly…）標成必填
    ///   結果是產生的 TypeScript 每個欄位都變成選填，例如 `id?: string`，前端用起來很麻煩。
    ///
    /// 【判斷規則】（依序判斷，先符合的為準）
    ///   ┌─────────────────────────────────┬─────────────────┬──────────────────────────┐
    ///   │ C# 寫法                          │ swagger         │ 產生的 TypeScript         │
    ///   ├─────────────────────────────────┼─────────────────┼──────────────────────────┤
    ///   │ [Required] DateOnly? HireDate   │ 必填、不可為 null │ hireDate: string         │
    ///   │ bool IsHalfDay = false          │ 選填            │ isHalfDay?: boolean      │
    ///   │ Guid Id、string Name（DTO）      │ 必填            │ id: string               │
    ///   │ DateOnly? TerminationDate       │ 選填、可為 null   │ terminationDate?: string | null │
    ///   └─────────────────────────────────┴─────────────────┴──────────────────────────┘
    ///
    /// 【在哪裡註冊】Program.cs 的 AddSwaggerGen：options.SchemaFilter&lt;RecordRequiredSchemaFilter&gt;()
    ///
    /// 【已知限制】
    ///   - 只處理透過建構子建立的型別（positional record）。用 { get; set; } 屬性、沒有建構子參數的 class 不會處理。
    ///   - 用 [JsonPropertyName] 改過名稱的屬性，名稱會對應不起來。
    ///
    /// 詳細說明見 docs/learning/01-value-types-nullable-requests.md。
    /// </summary>
    internal sealed class RecordRequiredSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Swashbuckle 每為一個 C# 型別產生 schema 後，就會呼叫一次這個方法。
        /// </summary>
        /// <param name="schema">Swashbuckle 已經產生好的 schema，這裡直接修改它。</param>
        /// <param name="context">context.Type 是這個 schema 對應的 C# 型別，例如 typeof(EmployeeDto)。</param>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            // 步驟 1：只處理「有屬性的物件」
            // - schema 可能是指向其他 schema 的參照（不能修改），只有 OpenApiSchema 才能修改
            // - string、enum 這類沒有屬性的 schema 不需要處理
            if (schema is not OpenApiSchema target || target.Properties is null || target.Properties.Count == 0)
            {
                return;
            }

            // 步驟 2：找出建立物件用的建構子
            // [Required] 和 ? 都寫在建構子參數上，所以要從建構子讀取這些資訊
            var constructor = FindDeserializationConstructor(context.Type);
            if (constructor is null)
            {
                return;
            }

            // NullabilityInfoContext 用來判斷參數是 string 還是 string?、DateOnly 還是 DateOnly?。
            // 它不是執行緒安全的，同時有多個 swagger.json 請求時會互相干擾，所以每次呼叫各自建立一個。
            var nullability = new NullabilityInfoContext();

            // 步驟 3：逐一檢查每個建構子參數
            foreach (var parameter in constructor.GetParameters())
            {
                // C# 參數名稱是 HireDate，JSON 屬性名稱是 hireDate（camelCase），所以不分大小寫比對
                var propertyName = target.Properties.Keys
                    .FirstOrDefault(k => string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (propertyName is null || !IsRequired(parameter, nullability))
                {
                    continue;
                }

                // 步驟 4：標示為必填（??= 代表「是 null 才賦值」）
                target.Required ??= new HashSet<string>();
                target.Required.Add(propertyName);

                // 步驟 5：拿掉「可為 null」
                // 用在 [Required] DateOnly? HireDate：C# 型別寫成 nullable，只是為了讓後端偵測「前端沒傳」，
                // 對前端來說，這個欄位的約定是「必填、不可為 null」。
                // JsonSchemaType 是 [Flags] enum，例如 String | Null；用位元運算 & ~Null 只拿掉 Null 這個旗標。
                if (target.Properties[propertyName] is OpenApiSchema property && property.Type is { } type)
                {
                    property.Type = type & ~JsonSchemaType.Null;
                }
            }
        }

        /// <summary>
        /// 判斷一個建構子參數，在 API 契約上是不是必填。規則依序判斷，先符合的為準。
        /// </summary>
        private static bool IsRequired(ParameterInfo parameter, NullabilityInfoContext nullability)
        {
            // 規則 1：明確標了 [Required] → 必填
            // 放在最前面，所以就算寫了 [Required] int X = 5 這種矛盾的寫法，也以 [Required] 為準
            if (parameter.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                return true;
            }

            // 規則 2：有預設值（例如 bool IsHalfDay = false）→ 選填
            // 前端沒傳時，System.Text.Json 會使用這個預設值，所以前端可以不傳
            if (parameter.HasDefaultValue)
            {
                return false;
            }

            // 規則 3：型別不可為 null（string、Guid、DateOnly…）→ 必填
            // 規則 4：其他（string?、Guid? 且沒有 [Required]）→ 選填
            return nullability.Create(parameter).WriteState == NullabilityState.NotNull;
        }

        /// <summary>
        /// 找出 System.Text.Json 用來建立物件的建構子，判斷方式和 System.Text.Json 一致：
        /// 1. 標了 [JsonConstructor] 的建構子
        /// 2. 只有一個 public 建構子時就是它。positional record 就是這種情況：
        ///    主建構子是 public；record 自動產生的「複製建構子」（給 with 用的）是 protected，不會被列出來
        /// 3. 有多個 public 建構子又沒標示時，System.Text.Json 自己也無法判斷，
        ///    這裡退而求其次挑參數最多的（主建構子通常包含所有屬性，參數最多）
        /// </summary>
        private static ConstructorInfo? FindDeserializationConstructor(Type type)
        {
            // GetConstructors()：透過反射取得這個型別所有 public 的實例建構子
            var constructors = type.GetConstructors();

            return constructors.FirstOrDefault(c => c.GetCustomAttribute<JsonConstructorAttribute>() is not null)
                ?? (constructors.Length == 1
                    ? constructors[0]
                    : constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault());
        }
    }
}
