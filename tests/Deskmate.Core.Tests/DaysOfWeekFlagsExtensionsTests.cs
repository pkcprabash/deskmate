using System;
using Deskmate.Core.Models;

namespace Deskmate.Core.Tests;

public class DaysOfWeekFlagsExtensionsTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, DaysOfWeekFlags.Monday)]
    [InlineData(DayOfWeek.Tuesday, DaysOfWeekFlags.Tuesday)]
    [InlineData(DayOfWeek.Wednesday, DaysOfWeekFlags.Wednesday)]
    [InlineData(DayOfWeek.Thursday, DaysOfWeekFlags.Thursday)]
    [InlineData(DayOfWeek.Friday, DaysOfWeekFlags.Friday)]
    [InlineData(DayOfWeek.Saturday, DaysOfWeekFlags.Saturday)]
    [InlineData(DayOfWeek.Sunday, DaysOfWeekFlags.Sunday)]
    public void ToFlag_MapsEachWeekdayToItsOwnBit(DayOfWeek day, DaysOfWeekFlags expected)
    {
        Assert.Equal(expected, day.ToFlag());
    }

    [Fact]
    public void Contains_MatchingDay_ReturnsTrue()
    {
        var set = DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday;

        Assert.True(set.Contains(DayOfWeek.Monday));
        Assert.True(set.Contains(DayOfWeek.Wednesday));
    }

    [Fact]
    public void Contains_NonMatchingDay_ReturnsFalse()
    {
        var set = DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday;

        Assert.False(set.Contains(DayOfWeek.Tuesday));
        Assert.False(set.Contains(DayOfWeek.Sunday));
    }

    [Fact]
    public void Contains_NoneSet_NeverMatches()
    {
        Assert.False(DaysOfWeekFlags.None.Contains(DayOfWeek.Monday));
    }

    [Fact]
    public void AllSevenFlags_AreDistinctBits()
    {
        var all = DaysOfWeekFlags.Monday | DaysOfWeekFlags.Tuesday | DaysOfWeekFlags.Wednesday
            | DaysOfWeekFlags.Thursday | DaysOfWeekFlags.Friday | DaysOfWeekFlags.Saturday | DaysOfWeekFlags.Sunday;

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            Assert.True(all.Contains(day));
        }
    }
}
