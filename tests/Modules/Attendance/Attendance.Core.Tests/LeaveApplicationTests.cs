namespace Attendance.Core.Tests;

public class LeaveApplicationTests
{
    private const string TestReason = "test reason";

    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly Guid ReviewerId = Guid.CreateVersion7();
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 14, 0, 0, TimeSpan.FromHours(8));

    #region 建立

    [Fact]
    public void Constructor_WithValidArguments_StatusIsDraft()
    {
        var leave = CreateLeaveFromToday(LeaveApplicationType.AnnualLeave, 2);

        Assert.Equal(LeaveApplicationStatus.Draft, leave.Status);
    }

    [Fact]
    public void Constructor_WithEmptyEmployeeId_ThrowsArgumentException()
    {
        var dateRange = new DateRange(Today, Today.AddDays(2));

        Assert.Throws<ArgumentException>(
            () => new LeaveApplication(Guid.Empty, LeaveApplicationType.AnnualLeave, dateRange, null));
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateLeaveFromToday((LeaveApplicationType)99, 2));
    }

    [Theory]
    [InlineData(LeaveApplicationType.AnnualLeave)]
    [InlineData(LeaveApplicationType.PersonalLeave)]
    [InlineData(LeaveApplicationType.SickLeave)]
    internal void Constructor_WithDefinedType_RecordsType(LeaveApplicationType type)
    {
        var leave = CreateLeaveFromToday(type, 2, TestReason);

        Assert.Equal(type, leave.Type);
    }

    // 請假時需指定開始日與結束日，兩天都包含在內。
    [Fact]
    public void Constructor_WithValidArguments_DurationIncludesStartAndEndDay()
    {
        const int leaveDays = 7;

        var leave = CreateLeaveFromToday(LeaveApplicationType.AnnualLeave, leaveDays);

        Assert.Equal(leaveDays, leave.Duration);
    }

    #endregion

    #region 天數上限

    // 事假最多 14 天，剛好 14 天要可以。
    [Fact]
    public void Constructor_PersonalLeaveAtDayLimit_Succeeds()
    {
        var leave = CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 14, TestReason);

        Assert.Equal(14, leave.Duration);
    }

    [Fact]
    public void Constructor_PersonalLeaveExceedsDayLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 15, TestReason));
    }

    // 天數上限只套用在事假。
    [Theory]
    [InlineData(LeaveApplicationType.SickLeave)]
    [InlineData(LeaveApplicationType.AnnualLeave)]
    internal void Constructor_NonPersonalLeaveExceedsPersonalLeaveDayLimit_Succeeds(LeaveApplicationType type)
    {
        var leave = CreateLeaveFromToday(type, 15);

        Assert.Equal(15, leave.Duration);
    }

    #endregion

    #region 理由

    [Fact]
    public void Constructor_WithReason_RecordsReason()
    {
        var leave = CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 2, TestReason);

        Assert.Equal(TestReason, leave.Reason);
    }

    // 事假以外的假別可以不填理由。
    [Theory]
    [InlineData(LeaveApplicationType.SickLeave)]
    [InlineData(LeaveApplicationType.AnnualLeave)]
    internal void Constructor_NonPersonalLeaveWithoutReason_Succeeds(LeaveApplicationType type)
    {
        var leave = CreateLeaveFromToday(type, 2, reason: null);

        Assert.Null(leave.Reason);
    }

    // 事假必須填理由，空字串和空白都視為沒填。
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_PersonalLeaveWithoutReason_ThrowsArgumentException(string? reason)
    {
        Assert.Throws<ArgumentException>(
            () => CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 2, reason));
    }

    // 理由最多 200 字，剛好 200 字要可以。
    [Fact]
    public void Constructor_ReasonAtLengthLimit_Succeeds()
    {
        string reason = new('r', 200);

        var leave = CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 2, reason);

        Assert.Equal(reason, leave.Reason);
    }

    [Fact]
    public void Constructor_ReasonExceedsLengthLimit_ThrowsArgumentException()
    {
        string tooLongReason = new('r', 201);

        Assert.Throws<ArgumentException>(
            () => CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 2, tooLongReason));
    }

    #endregion

    #region 修改

    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Rejected)]
    internal void Update_InDraftOrRejected_UpdatesFields(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);
        const string reason = "new test reason";
        var dateRange = new DateRange(Today.AddDays(1), Today.AddDays(7));
        const LeaveApplicationType type = LeaveApplicationType.SickLeave;

        leave.Update(type, dateRange, reason);

        Assert.Equal(reason, leave.Reason);
        Assert.Equal(dateRange, leave.DateRange);
        Assert.Equal(type, leave.Type);
    }

    [Theory]
    [InlineData(LeaveApplicationStatus.Submitted)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal void Update_InOtherStatus_ThrowsInvalidOperationException(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);
        var dateRange = new DateRange(Today.AddDays(1), Today.AddDays(7));

        Assert.Throws<InvalidOperationException>(
            () => leave.Update(LeaveApplicationType.SickLeave, dateRange, "new test reason"));
    }

    [Fact]
    public void Update_WithUndefinedType_ThrowsArgumentOutOfRangeException()
    {
        var leave = CreateTestLeave();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => leave.Update((LeaveApplicationType)99, leave.DateRange, TestReason));
    }

    // 修改時也要重新驗證假別和理由的組合，而且驗證失敗時不能只改到一半。
    [Fact]
    public void Update_ToPersonalLeaveWithoutReason_ThrowsAndKeepsOriginalValues()
    {
        var originalRange = new DateRange(Today, Today.AddDays(2));
        var leave = new LeaveApplication(EmployeeId, LeaveApplicationType.SickLeave, originalRange, null);
        var newRange = new DateRange(Today.AddDays(5), Today.AddDays(6));

        Assert.Throws<ArgumentException>(
            () => leave.Update(LeaveApplicationType.PersonalLeave, newRange, null));

        Assert.Equal(LeaveApplicationType.SickLeave, leave.Type);
        Assert.Equal(originalRange, leave.DateRange);
        Assert.Null(leave.Reason);
    }

    [Fact]
    public void Update_PersonalLeaveExceedsDayLimit_ThrowsArgumentException()
    {
        var leave = CreateTestLeave();
        var tooLongRange = new DateRange(Today, Today.AddDays(14)); // 15 天

        Assert.Throws<ArgumentException>(
            () => leave.Update(LeaveApplicationType.PersonalLeave, tooLongRange, TestReason));
    }

    #endregion

    #region 送出

    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Rejected)]
    internal void Submit_InDraftOrRejected_StatusIsSubmitted(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        leave.Submit();

        Assert.Equal(LeaveApplicationStatus.Submitted, leave.Status);
    }

    [Theory]
    [InlineData(LeaveApplicationStatus.Submitted)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal void Submit_InOtherStatus_ThrowsInvalidOperationException(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        Assert.Throws<InvalidOperationException>(() => leave.Submit());
    }

    // 退回後重新送出，要清除上一次的審核資訊。
    [Fact]
    public void Submit_AfterRejected_ClearsPreviousReviewInfo()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Rejected);

        leave.Submit();

        Assert.Null(leave.ReviewerId);
        Assert.Null(leave.ReviewedAt);
        Assert.Null(leave.RejectReason);
    }

    #endregion

    #region 核准

    [Fact]
    public void Approve_WhenSubmitted_RecordsReviewerAndTime()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);

        leave.Approve(ReviewerId, Now);

        Assert.Equal(LeaveApplicationStatus.Approved, leave.Status);
        Assert.Equal(ReviewerId, leave.ReviewerId);
        Assert.Equal(Now, leave.ReviewedAt);
    }

    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Rejected)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal void Approve_InOtherStatus_ThrowsInvalidOperationException(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        Assert.Throws<InvalidOperationException>(() => leave.Approve(ReviewerId, Now));
    }

    [Fact]
    public void Approve_BySelf_ThrowsArgumentException()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);

        Assert.Throws<ArgumentException>(() => leave.Approve(EmployeeId, Now));
    }

    [Fact]
    public void Approve_WithEmptyReviewerId_ThrowsArgumentException()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);

        Assert.Throws<ArgumentException>(() => leave.Approve(Guid.Empty, Now));
    }

    #endregion

    #region 退回

    [Fact]
    public void Reject_WhenSubmitted_RecordsReviewerTimeAndReason()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);
        const string rejectReason = "日期需要調整";

        leave.Reject(ReviewerId, rejectReason, Now);

        Assert.Equal(LeaveApplicationStatus.Rejected, leave.Status);
        Assert.Equal(ReviewerId, leave.ReviewerId);
        Assert.Equal(Now, leave.ReviewedAt);
        Assert.Equal(rejectReason, leave.RejectReason);
    }

    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Rejected)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal void Reject_InOtherStatus_ThrowsInvalidOperationException(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        Assert.Throws<InvalidOperationException>(() => leave.Reject(ReviewerId, "test reject", Now));
    }

    // 傳入 null 時會拋出 ArgumentNullException（ArgumentException 的子類別），所以用 ThrowsAny。
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithoutReason_ThrowsArgumentException(string? rejectReason)
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);

        Assert.ThrowsAny<ArgumentException>(() => leave.Reject(ReviewerId, rejectReason!, Now));
    }

    [Fact]
    public void Reject_BySelf_ThrowsArgumentException()
    {
        var leave = CreateLeaveInStatus(LeaveApplicationStatus.Submitted);

        Assert.Throws<ArgumentException>(() => leave.Reject(EmployeeId, "test reject", Now));
    }

    #endregion

    #region 撤回

    [Theory]
    [InlineData(LeaveApplicationStatus.Draft)]
    [InlineData(LeaveApplicationStatus.Submitted)]
    internal void Withdraw_InDraftOrSubmitted_StatusIsWithdrawn(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        leave.Withdraw();

        Assert.Equal(LeaveApplicationStatus.Withdrawn, leave.Status);
    }

    [Theory]
    [InlineData(LeaveApplicationStatus.Rejected)]
    [InlineData(LeaveApplicationStatus.Approved)]
    [InlineData(LeaveApplicationStatus.Withdrawn)]
    internal void Withdraw_InOtherStatus_ThrowsInvalidOperationException(LeaveApplicationStatus status)
    {
        var leave = CreateLeaveInStatus(status);

        Assert.Throws<InvalidOperationException>(() => leave.Withdraw());
    }

    #endregion

    private static LeaveApplication CreateLeaveFromToday(LeaveApplicationType type, int days, string? reason = null)
    {
        var dateRange = new DateRange(Today, Today.AddDays(days - 1)); // 包含當天，故 -1
        return new LeaveApplication(EmployeeId, type, dateRange, reason);
    }

    private static LeaveApplication CreateTestLeave()
    {
        return CreateLeaveFromToday(LeaveApplicationType.PersonalLeave, 3, TestReason);
    }

    private static LeaveApplication CreateLeaveInStatus(LeaveApplicationStatus status)
    {
        var leave = CreateTestLeave();

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

        return leave;
    }
}
