using System.Diagnostics.CodeAnalysis;

namespace Organization.Core
{
    /// <summary>
    /// 操作的結果：成功時有 <see cref="Value"/>，失敗時有 <see cref="Error"/>，兩者不會同時存在。
    /// 業務規則不通過屬於正常情況，用回傳值表達，不丟例外。
    ///
    /// 刻意不放在共用專案：依照微服務拆分的標準，每個模組各自擁有一份，
    /// 之後拆成獨立服務時不需要共用套件，各模組也可以各自修改。
    /// Attendance.Core 有一份相同的 Result，修改時不需要同步。
    ///
    /// 必須是 public：public 的 Service 會回傳它（CS0050）。
    /// </summary>
    /// <typeparam name="TValue">成功時的值</typeparam>
    /// <typeparam name="TError">失敗的原因，每個操作使用自己的 enum</typeparam>
    public sealed class Result<TValue, TError>
        where TError : struct, Enum
    {
        public TValue? Value { get; }
        public TError? Error { get; }

        [MemberNotNullWhen(true, nameof(Value))]
        [MemberNotNullWhen(false, nameof(Error))]
        public bool IsSuccess => Error is null;

        private Result(TValue? value, TError? error)
        {
            Value = value;
            Error = error;
        }

        public static Result<TValue, TError> Success(TValue value) => new(value, null);

        public static Result<TValue, TError> Failure(TError error) => new(default, error);

        /// <summary>
        /// 讓方法可以直接 <c>return dto;</c>。
        /// 注意：TValue 是介面（例如 IReadOnlyList&lt;T&gt;）時 C# 不會套用這個轉換，請改用 <see cref="Success"/>。
        /// </summary>
        public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

        /// <summary>讓方法可以直接 <c>return XxxError.Something;</c>。</summary>
        public static implicit operator Result<TValue, TError>(TError error) => Failure(error);
    }
}
