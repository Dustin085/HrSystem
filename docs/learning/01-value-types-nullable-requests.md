# 01. 值型別、Nullable 與 Request 欄位設計

> 學習筆記。整理自 HrSystem 開發過程中的討論，範例都取自本專案。

## 目錄
1. [值型別與參考型別](#1-值型別與參考型別)
2. [傳遞參數：複製資料、複製地址、ref](#2-傳遞參數複製資料複製地址ref)
3. [預設值 default](#3-預設值-default)
4. [兩種 `?`：Nullable 值型別與 Nullable 參考型別](#4-兩種-nullable-值型別與-nullable-參考型別)
5. [為什麼 C# 這樣設計](#5-為什麼-c-這樣設計)
6. [JSON 請求進來時發生的事](#6-json-請求進來時發生的事)
7. [Request 欄位的寫法規則](#7-request-欄位的寫法規則)
8. [其他做法：`[JsonRequired]` 與 `RespectRequiredConstructorParameters`](#8-其他做法jsonrequired-與-respectrequiredconstructorparameters)
9. [本專案的對應](#9-本專案的對應)
10. [常見誤解](#10-常見誤解)

---

## 1. 值型別與參考型別

| | 值型別 | 參考型別 |
|---|---|---|
| 宣告方式 | `struct`、`enum`、`record struct` | `class`、`record`、`interface` |
| 常見例子 | `int`、`bool`、`decimal`、`Guid`、`DateOnly`、`TimeOnly`、所有 `enum` | `string`、`EmployeeDto`、`List<T>`、所有介面 |
| 變數裡放的是 | **資料本身** | 物件的**地址** |
| 可以自己建立嗎 | 可以，用 `struct` | 可以，用 `class` 或 `record` |
| 未賦值的欄位 | `default`（見第 3 節） | `null` |
| 能不能是 `null` | **不能**，除非寫成 `T?` | 執行時期**一定可能**是 `null` |

### 自己建立值型別
```csharp
public readonly record struct TimeRange(TimeOnly Start, TimeOnly End);
```
適合自己寫成 struct 的情況：
- 代表**一個單一的值**（時間區間、座標、金額）
- **很小**（一般建議 16 位元組以內，因為每次傳遞都會複製整份）
- **不可變**（加上 `readonly`）

其他情況用 `class` 或 `record`。

### 和 JavaScript 的對照
概念上接近「JS 原生型別 vs 物件」，但有三個差別：
1. JS 的原生型別是固定的那幾種，**不能自己建立**；C# 可以用 `struct` 建立新的值型別。
2. C# 的 `string` 是**參考型別**（JS 是原生型別）。不過它不可變、`==` 比較內容，用起來很像值型別。
3. 「傳地址」要精確理解，見下一節。

---

## 2. 傳遞參數：複製資料、複製地址、ref

| | 賦值或傳參數時複製的是 | 方法裡修改後，呼叫端看得到嗎 |
|---|---|---|
| 值型別 | 整份資料 | 看不到 |
| 參考型別 | **地址**（兩個變數指向同一個物件） | 改物件的**內容**：看得到；把參數**換成別的物件**：看不到 |
| 加上 `ref` | **變數本身** | 看得到 |

```csharp
void Clear(List<int> list) => list.Clear();          // 呼叫端看得到：同一個物件被清空
void Replace(List<int> list) => list = new();        // 呼叫端看不到：只換了自己手上的地址複本
void Increment(ref int x) => x++;                    // 呼叫端看得到：ref 傳的是變數本身
```

- 參考型別的傳遞方式叫 **call by sharing**，JS 的物件也是這樣。
- `ref` 才是真正的「傳參考」，JS 沒有對應功能。
- 本專案的例子：`Interlocked.Increment(ref _lastEmployeeNumber)`（`InMemoryEmployeeRepository`）。
  `ref` 只能用在**變數或欄位**，不能用在屬性。

---

## 3. 預設值 default

| 型別 | `default` 值 |
|---|---|
| `int`、`long`、`decimal` | `0` |
| `bool` | `false` |
| `Guid` | `00000000-0000-0000-0000-000000000000`（`Guid.Empty`） |
| `DateOnly` | `0001-01-01` |
| `TimeOnly` | `00:00:00` |
| `enum` | 數值 `0` 對應的成員（通常是第一個成員） |
| 任何參考型別 | `null` |
| 任何 `T?`（`Nullable<T>`） | `null` |

- **欄位**沒有賦值時，會自動是 `default`。
- **區域變數**沒有賦值就使用，會編譯錯誤（CS0165）。

---

## 4. 兩種 `?`：Nullable 值型別與 Nullable 參考型別

|  | 值型別的 `?`（`DateOnly?`） | 參考型別的 `?`（`string?`） |
|---|---|---|
| 本質 | **真的是不同的型別**：`Nullable<DateOnly>` | **只是給編譯器看的註記** |
| 執行時期有沒有差別 | 有，多了一個「有沒有值」的旗標 | 沒有，`string` 和 `string?` 完全一樣 |
| 加入的版本 | C# 2.0（2005） | C# 8.0（2019） |
| 控制它的設定 | 一直都有 | `.csproj` 的 `<Nullable>enable</Nullable>` |

`Nullable<T>` 大約長這樣：
```csharp
public struct Nullable<T> where T : struct
{
    private readonly bool hasValue;   // 多出來的旗標
    internal readonly T value;
}
```

### Nullable 參考型別（`<Nullable>enable</Nullable>`）
- `string`：意圖是「不會是 null」；`string?`：「可能是 null」。
- 編譯器依此檢查，違反時給**警告**（不是錯誤）。
  想變成錯誤：`<WarningsAsErrors>nullable</WarningsAsErrors>`。
- 只是編譯時期的安全網：**執行時期 `string` 還是可能是 null**（例如 JSON 沒傳的欄位）。
- `!` 運算子（例如 `request.HireDate!.Value`）是告訴編譯器「我確定不是 null」，會關掉警告。判斷錯了，執行時還是會 `NullReferenceException`，要謹慎使用。

---

## 5. 為什麼 C# 這樣設計

1. **值型別是資料本身，沒有位置放 null。**
   `null` 是「地址沒有指向任何物件」，是地址的概念。`int` 的 4 個位元組每一種組合都是合法數字，沒有任何組合能代表「沒有值」，所以必須多一個旗標（`Nullable<T>`），型別也會變大。
2. **效能。** 值型別不需要 heap 配置、不需要 GC、陣列是連續排列，CPU 快取效率高。如果每個值型別都能是 null，每個值都要多帶旗標，這些優勢就沒了。所以「預設不可為 null，需要時才用 `?` 付出代價」。
3. **歷史。**
   - C# 1.0：值型別不能是 null；參考型別可以是 null（沿襲 C、C++、Java）。
   - C# 2.0：資料庫欄位常允許 null，於是加入 `Nullable<T>` 和 `?`。
   - C# 8.0：參考型別的 null 被稱為「十億美元的錯誤」（Tony Hoare），加入 nullable 參考型別。
4. **為什麼兩種 `?` 不一致：向下相容。** C# 8.0 時已有近 20 年的程式碼，若在執行時期改變 `string` 的行為，舊程式碼會全部壞掉，所以只做成編譯時期的註記。
   比較新的語言（Kotlin、Swift、Rust）一開始就統一了：所有型別預設不可為 null。

---

## 6. JSON 請求進來時發生的事

```
JSON ──① System.Text.Json──→ request record ──② 模型驗證 [Required]──→ Controller
```

### ① System.Text.Json：缺少的欄位填入 default
positional record 只能透過建構子建立，System.Text.Json 對 JSON 裡**找不到的欄位**，會傳入該型別的 `default`（建構子參數有寫預設值的話，用那個預設值），**而且不會報錯**。

| 型別 | 欄位**沒傳** | 欄位傳了 **`null`** |
|---|---|---|
| `DateOnly` | `0001-01-01`（默默的） | 轉換錯誤 → 400 |
| `DateOnly?` | `null` | `null` |
| `string` | `null` | `null` |

> 最危險的是「**沒傳**」：前端漏了一個欄位，後端拿到一個看起來合法的值，完全察覺不到。

為什麼不乾脆報錯？「沒傳的欄位就用預設值」是 JSON 函式庫長期的慣例，讓 API 新增欄位時舊的前端不會壞掉。

### ② `[Required]`：只檢查是不是 null
```csharp
// 判斷邏輯大致如此
if (value is null) return false;
if (value is string s && string.IsNullOrWhiteSpace(s)) return false;
return true;
```
`0001-01-01` 不是 null，所以**對不可為 null 的值型別加 `[Required]`，等於沒檢查**。

### 結論
| request 的寫法 | 前端沒傳 | `[Required]` | 結果 |
|---|---|---|---|
| `[Required] DateOnly HireDate` | `0001-01-01` | ✅ 通過 | 存進西元 1 年，沒有任何錯誤 ❌ |
| `[Required] DateOnly? HireDate` | `null` | ❌ 不通過 | 400：The HireDate field is required. ✅ |

**`?` 是技術手段，不是 API 約定。** 對前端來說欄位仍然是**必填**（swagger 標示 required、TypeScript 型別是 `hireDate: string`）。`?` 只是讓後端**有能力偵測到「沒傳」**，`[Required]` 再把它擋下來。

**寫成 `DateOnly` 不會讓欄位變成必填。** C# 型別只描述後端拿到的值，對前端沒有約束力。

---

## 7. Request 欄位的寫法規則

| 欄位的用途 | 寫法 | 前端沒傳時 | 例子 |
|---|---|---|---|
| **必填的值型別** | `[Required] DateOnly? HireDate` | 400 | 到職日、排班的四個欄位 |
| **必填的字串** | `[Required] string Name` | 400 | 姓名 |
| **選填，沒傳代表「不篩選」** | `DateOnly? From`（不加 `[Required]`） | `null` | 排班查詢的 `employeeId`、`from`、`to` |
| **選填，有合理預設值** | `int PageSize = 20` | 用 `20` | 分頁大小、`bool IncludeInactive = false` |
| **Response DTO** | `DateOnly HireDate`（**不加** `?`） | 不適用，後端一定會填 | `EmployeeDto` |
| **路由參數** | `Guid id` | 不適用，網址沒有就不會進到 action | `GET /api/employees/{id:guid}` |

- 必填的值型別在 Service 裡用 `request.HireDate!.Value` 取值（已經驗證過不是 null）。
- 有預設值的參數（`int PageSize = 20`）：沒傳時拿到的是**你刻意指定的值**，不是「默默變成 0」。
- `enum` 也是值型別，必填時同樣要寫成 `[Required] MyEnum?`。

---

## 8. 其他做法：`[JsonRequired]` 與 `RespectRequiredConstructorParameters`

```csharp
// A：個別欄位（.NET 7+）
public record CreateEmployeeRequest([Required] string Name, [JsonRequired] DateOnly HireDate);

// B：全域，所有 record 建構子參數都必填（.NET 9+）
.AddJsonOptions(o => o.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
```

兩者都讓**第 ① 步（JSON 轉換）**就報錯，而不是第 ② 步。

| | `[Required]` + `T?`（本專案） | `[JsonRequired]` / `RespectRequiredConstructorParameters` |
|---|---|---|
| Service 程式碼 | 要寫 `!.Value` | 乾淨 |
| 錯誤訊息 | 有欄位名稱，所有錯誤一次回傳 | System.Text.Json 的格式，key 是 `$` 或 `request` |
| 多個欄位都有問題時 | 全部列出 | JSON 轉換失敗就停止，後續驗證不會執行 |
| 前端顯示欄位錯誤 | 容易 | 較難 |

本專案選擇前者：對使用者比較友善，`!.Value` 是可以接受的代價。

---

## 9. 本專案的對應

| 檔案 | 欄位 | 為什麼這樣寫 |
|---|---|---|
| `src/Modules/Organization/Organization.Contracts/CreateEmployeeRequest.cs` | `[Required, StringLength(50)] string Name` | 參考型別，`[Required]` 擋得住 null 和空白 |
| 同上 | `[Required] DateOnly? HireDate` | 必填的值型別 |
| `src/Modules/Attendance/Attendance.Contracts/CreateShiftAssignmentRequest.cs` | 四個欄位都是 `[Required] T?` | 必填的值型別 |
| `src/Modules/Attendance/Attendance.Core/ShiftAssignmentsController.cs` 的查詢參數 | `Guid? employeeId`、`DateOnly? from`、`DateOnly? to` | 選填，`null` 代表不篩選 |
| `src/Modules/Organization/Organization.Contracts/EmployeeDto.cs` | `Guid Id`、`DateOnly HireDate` | Response，後端一定會填 |
| `src/HrSystem.Host/OpenApi/RecordRequiredSchemaFilter.cs` | — | 讓 swagger 正確標示 required：有 `[Required]`、或型別不可為 null 的參數都標成必填 |

### 如果排班 request 沒有加 `?`，前端漏傳欄位時
| 欄位 | 會變成 | 後果 |
|---|---|---|
| `EmployeeId` | `Guid.Empty` | 回傳「找不到此員工」：有擋下來，但**錯誤訊息是錯的** |
| `Date` | `0001-01-01` | 排了一個西元 1 年的班，**成功存入** |
| `StartTime` | `00:00` | **最危險**：默默排出 00:00 上班的班，完全看不出問題 |
| `EndTime` | `00:00` | 回傳「下班時間必須晚於上班時間」：**錯誤訊息是錯的** |

---

## 10. 常見誤解

| 誤解 | 正確 |
|---|---|
| 寫成 `DateOnly` 就代表前端一定要傳 | C# 型別對前端沒有約束力。沒傳就默默變成 `0001-01-01` |
| `[Required]` 可以擋下任何沒傳的欄位 | 只擋得住 `null`（和空白字串）。不可為 null 的值型別永遠通過 |
| 開了 `<Nullable>enable</Nullable>`，`string` 在執行時期就不會是 null | 只是編譯時期的檢查。JSON 沒傳的 `string` 欄位執行時期就是 null |
| `string?` 和 `DateOnly?` 是同一種東西 | `DateOnly?` 是真的 `Nullable<DateOnly>`；`string?` 只是註記 |
| 參考型別是「傳參考」 | 是「傳地址的複本」（call by sharing）。真正的傳參考要用 `ref` |
| 所有值型別欄位都要加 `?` | 只有 **request 的必填欄位**和**代表「不篩選」的選填欄位**需要；有預設值的選填欄位、Response DTO 不需要 |
