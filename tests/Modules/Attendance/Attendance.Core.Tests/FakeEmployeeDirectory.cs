using Organization.Contracts;

namespace Attendance.Core.Tests;

/// <summary>
/// 測試用的 IEmployeeDirectory，可以直接指定員工是否存在、是否在職。
/// </summary>
internal sealed class FakeEmployeeDirectory : IEmployeeDirectory
{
    private readonly Dictionary<Guid, bool> _isEmployed = [];

    /// <summary>新增一位員工，<paramref name="isEmployed"/> 決定他在任何期間是否都在職。</summary>
    public void AddEmployee(Guid id, bool isEmployed = true) => _isEmployed[id] = isEmployed;

    public Task<bool> ExistsByIdAsync(Guid id) => Task.FromResult(_isEmployed.ContainsKey(id));

    public Task<EmployeeDto?> GetByIdAsync(Guid id) => throw new NotSupportedException();

    public Task<EmploymentStatus?> GetEmploymentStatusOnAsync(Guid id, DateOnly date) =>
        Task.FromResult<EmploymentStatus?>(_isEmployed.TryGetValue(id, out var employed)
            ? (employed ? EmploymentStatus.Active : EmploymentStatus.Terminated)
            : null);

    public Task<bool?> IsEmployedThroughoutAsync(Guid id, DateOnly start, DateOnly end) =>
        Task.FromResult<bool?>(_isEmployed.TryGetValue(id, out var employed) ? employed : null);
}
