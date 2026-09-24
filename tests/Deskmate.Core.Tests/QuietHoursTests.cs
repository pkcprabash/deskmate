using System;
using Deskmate.Core;

namespace Deskmate.Core.Tests;

public class QuietHoursTests
{
    [Fact]
    public void IsWithin_NoRangeConfigured_ReturnsFalse()
    {
        Assert.False(QuietHours.IsWithin(null, null, new TimeOnly(23, 0)));
    }

    [Fact]
    public void IsWithin_NormalRange_InsideReturnsTrue()
    {
        var start = new TimeOnly(13, 0);
        var end = new TimeOnly(15, 0);

        Assert.True(QuietHours.IsWithin(start, end, new TimeOnly(14, 0)));
    }

    [Fact]
    public void IsWithin_NormalRange_OutsideReturnsFalse()
    {
        var start = new TimeOnly(13, 0);
        var end = new TimeOnly(15, 0);

        Assert.False(QuietHours.IsWithin(start, end, new TimeOnly(16, 0)));
    }

    [Theory]
    [InlineData(23, 0, true)] // 11pm: inside the overnight window
    [InlineData(2, 0, true)] // 2am: inside the overnight window
    [InlineData(12, 0, false)] // noon: outside
    public void IsWithin_RangeWrapsPastMidnight_HandlesBothSides(int hour, int minute, bool expected)
    {
        var start = new TimeOnly(22, 0);
        var end = new TimeOnly(7, 0);

        Assert.Equal(expected, QuietHours.IsWithin(start, end, new TimeOnly(hour, minute)));
    }

    [Fact]
    public void IsWithin_EqualStartAndEnd_TreatedAsDisabled()
    {
        var same = new TimeOnly(9, 0);

        Assert.False(QuietHours.IsWithin(same, same, new TimeOnly(9, 0)));
    }
}
