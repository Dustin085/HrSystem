using Attendance.Contracts;

namespace Attendance.Core.Tests;

public class LeaveApplicationServiceSubmitTests
{
    private const string TestReason = "test reason";

    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly Guid OtherEmployeeId = Guid.CreateVersion7();
    private static readonly Guid ReviewerId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 14, 0, 0, TimeSpan.FromHours(8));

    // 11/3–11/5 和 11/5–11/7 共用 11/5，算重疊；11/6–11/7 和 11/3–11/5 相鄰，不算重疊
    private static readonly DateRange Nov3To5 = new(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 5));
    private static readonly DateRange Nov5To7 = new(new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 7));
    private static readonly DateRange Nov6To7 = new(new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 7));

    private readonly InMemoryLeaveApplicationRepository _repository = new();
    private readonly FakeEmployeeDirectory _employeeDirectory = new();
    private readonly LeaveApplicationService _service;

    // xUnit 每個測試都會建立新的實例，所以每個測試都有自己的 Repository
    public LeaveApplicationServiceSubmitTests()
    {
        _employeeDirectory.AddEmployee(EmployeeId);
        _employeeDirectory.AddEmployee(OtherEmployeeId);
        _service = new LeaveApplicationService(_repository, _employeeDirectory);
    }

    #region 成功

    [Fact]
    public async Task SubmitAsync_DraftWithoutOverlap_StatusIsSubmitted()
    {
        var leave = AddLeave(EmployeeId, Nov3To5, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(LeaveApplicationStatus.Submitted, result.Value.Status);
        Assert.Equal(LeaveApplicationStatus.Submitted, leave.Status);
    }

    [Fact]
    public async Task SubmitAsync_ResubmitAfterRejected_Succeeds()
    {
        var leave = AddLeave(EmployeeId, Nov3To5, LeaveApplicationStatus.Rejected);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Null(result.Error);
        Assert.Equal(LeaveApplicationStatus.Submitted, leave.Status);
    }

    [Fact]
    public async Task SubmitAsync_AdjacentToSubmittedLeave_Succeeds()
    {
        AddLeave(EmployeeId, Nov3To5, LeaveApplicationStatus.Submitted);
        var leave = AddLeave(EmployeeId, Nov6To7, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Null(result.Error);
    }

    // 規則只禁止和「待審核、已核准」重疊，草稿、已退回、已撤回都不影響送出。
    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Rejected)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal async Task SubmitAsync_OverlapsWithNonBlockingLeave_Succeeds(LeaveApplicationStatus otherStatus)
    {
        AddLeave(EmployeeId, Nov3To5, otherStatus);
        var leave = AddLeave(EmployeeId, Nov5To7, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SubmitAsync_OverlapsWithOtherEmployeesLeave_Succeeds()
    {
        AddLeave(OtherEmployeeId, Nov3To5, LeaveApplicationStatus.Approved);
        var leave = AddLeave(EmployeeId, Nov5To7, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Null(result.Error);
    }

    #endregion

    #region 重疊

    [Theory]
    [InlineData(LeaveApplicationStatus.Submitted)]
    [InlineData(LeaveApplicationStatus.Approved)]
    internal async Task SubmitAsync_OverlapsWithBlockingLeave_ReturnsOverlapping(LeaveApplicationStatus otherStatus)
    {
        AddLeave(EmployeeId, Nov3To5, otherStatus);
        var leave = AddLeave(EmployeeId, Nov5To7, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Equal(SubmitLeaveApplicationError.Overlapping, result.Error);
        Assert.Equal(LeaveApplicationStatus.Draft, leave.Status); // 失敗時不能被送出
    }

    #endregion

    #region 狀態

    // 沒有先檢查狀態時：已送出、已核准的申請會比對到自己而被誤判為重疊；已撤回的會造成 500。
    [Theory]
    [InlineData(LeaveApplicationStatus.Submitted)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal async Task SubmitAsync_InNonSubmittableStatus_ReturnsInvalidStatus(LeaveApplicationStatus status)
    {
        var leave = AddLeave(EmployeeId, Nov3To5, status);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Equal(SubmitLeaveApplicationError.InvalidStatus, result.Error);
        Assert.Equal(status, leave.Status);
    }

    #endregion

    #region 找不到、不在職

    [Fact]
    public async Task SubmitAsync_NotFound_ReturnsLeaveApplicationNotFound()
    {
        var result = await _service.SubmitAsync(Guid.CreateVersion7());

        Assert.Equal(SubmitLeaveApplicationError.LeaveApplicationNotFound, result.Error);
    }

    [Fact]
    public async Task SubmitAsync_EmployeeNotEmployed_ReturnsEmployeeNotEmployedOnDateRange()
    {
        var leave = AddLeave(EmployeeId, Nov3To5, LeaveApplicationStatus.Draft);
        _employeeDirectory.AddEmployee(EmployeeId, isEmployed: false);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Equal(SubmitLeaveApplicationError.EmployeeNotEmployedOnDateRange, result.Error);
        Assert.Equal(LeaveApplicationStatus.Draft, leave.Status);
    }

    [Fact]
    public async Task SubmitAsync_EmployeeNotFound_ReturnsEmployeeNotFound()
    {
        var leave = AddLeave(Guid.CreateVersion7(), Nov3To5, LeaveApplicationStatus.Draft);

        var result = await _service.SubmitAsync(leave.Id);

        Assert.Equal(SubmitLeaveApplicationError.EmployeeNotFound, result.Error);
    }

    #endregion

    /// <summary>直接放進 Repository，並透過 entity 的方法轉換到指定狀態。</summary>
    private LeaveApplication AddLeave(Guid employeeId, DateRange dateRange, LeaveApplicationStatus status)
    {
        var leave = new LeaveApplication(employeeId, LeaveApplicationType.AnnualLeave, dateRange, TestReason);

        switch (status)
        {
            case LeaveApplicationStatus.Draft:
                break;
            case LeaveApplicationStatus.Submitted:
                leave.Submit();
                break;
            case LeaveApplicationStatus.Approved:
                leave.Submit();
                leave.Approve(ReviewerId, Now);
                break;
            case LeaveApplicationStatus.Rejected:
                leave.Submit();
                leave.Reject(ReviewerId, "test reject", Now);
                break;
            case LeaveApplicationStatus.Withdrawn:
                leave.Withdraw();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        _repository.Add(leave);
        return leave;
    }
}
