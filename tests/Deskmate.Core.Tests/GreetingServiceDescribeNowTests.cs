using System;
using Deskmate.Core;

namespace Deskmate.Core.Tests;

public class GreetingServiceDescribeNowTests
{
    [Theory]
    [InlineData(6, "morning")]
    [InlineData(11, "morning")]
    [InlineData(12, "afternoon")]
    [InlineData(17, "afternoon")]
    [InlineData(18, "evening")]
    [InlineData(23, "evening")]
    public void DescribeNow_ReturnsTimeOfDayForHour(int hour, string expected)
    {
        var now = new DateTimeOffset(2026, 9, 28, hour, 0, 0, TimeSpan.Zero); // a Monday

        var (timeOfDay, _) = GreetingService.DescribeNow(now);

        Assert.Equal(expected, timeOfDay);
    }

    [Fact]
    public void DescribeNow_Saturday_IsWeekend()
    {
        var saturday = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        var (_, isWeekend) = GreetingService.DescribeNow(saturday);

        Assert.True(isWeekend);
    }

    [Fact]
    public void DescribeNow_Wednesday_IsNotWeekend()
    {
        var wednesday = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

        var (_, isWeekend) = GreetingService.DescribeNow(wednesday);

        Assert.False(isWeekend);
    }
}
