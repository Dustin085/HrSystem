namespace Attendance.Core.Tests;

public class DateRangeTests
{
    private static readonly DateOnly Nov1 = new(2026, 11, 1);

    [Fact]
    public void Constructor_EndBeforeStart_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Nov1, Nov1.AddDays(-1)));
    }

    // 開始日和結束日同一天是合法的，算 1 天。
    [Fact]
    public void Constructor_StartEqualsEnd_DurationIsOneDay()
    {
        var range = new DateRange(Nov1, Nov1);

        Assert.Equal(1, range.Duration);
    }

    // 天數包含開始日和結束日，11/3–11/5 是 3 天。
    [Fact]
    public void Duration_IncludesStartAndEndDay()
    {
        var range = new DateRange(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 5));

        Assert.Equal(3, range.Duration);
    }

    [Fact]
    public void TryCreate_EndBeforeStart_ReturnsFalse()
    {
        bool created = DateRange.TryCreate(Nov1, Nov1.AddDays(-1), out var range);

        Assert.False(created);
        Assert.Equal(default, range);
    }

    [Fact]
    public void TryCreate_ValidDates_ReturnsTrue()
    {
        bool created = DateRange.TryCreate(Nov1, Nov1.AddDays(2), out var range);

        Assert.True(created);
        Assert.Equal(new DateRange(Nov1, Nov1.AddDays(2)), range);
    }

    // 閉區間：11/1–11/2 和 11/2–11/3 共用 11/2，算重疊。
    [Theory]
    [InlineData(1, 2, 2, 3)] // 只共用一天
    [InlineData(1, 5, 2, 3)] // 包含
    [InlineData(1, 3, 2, 5)] // 部分重疊
    [InlineData(2, 2, 2, 2)] // 同一天
    public void Overlaps_SharedDays_ReturnsTrue(int start1, int end1, int start2, int end2)
    {
        var a = CreateNovemberRange(start1, end1);
        var b = CreateNovemberRange(start2, end2);

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Theory]
    [InlineData(1, 2, 3, 4)] // 相鄰但不重疊
    [InlineData(1, 1, 5, 5)]
    public void Overlaps_NoSharedDays_ReturnsFalse(int start1, int end1, int start2, int end2)
    {
        var a = CreateNovemberRange(start1, end1);
        var b = CreateNovemberRange(start2, end2);

        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    private static DateRange CreateNovemberRange(int startDay, int endDay)
    {
        return new DateRange(new DateOnly(2026, 11, startDay), new DateOnly(2026, 11, endDay));
    }
}
