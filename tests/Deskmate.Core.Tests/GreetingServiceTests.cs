using System;
using Deskmate.Core;

namespace Deskmate.Core.Tests;

public class GreetingServiceTests
{
    [Fact]
    public void DetermineKind_NoPriorGreetingToday_ReturnsFull()
    {
        var sut = new GreetingService();
        var today = new DateOnly(2026, 9, 21);

        var kind = sut.DetermineKind(lastGreetingDate: null, today, lastWelcomeBackAt: null, now: DateTimeOffset.Now);

        Assert.Equal(GreetingKind.Full, kind);
    }

    [Fact]
    public void DetermineKind_PriorGreetingWasYesterday_ReturnsFull()
    {
        var sut = new GreetingService();
        var today = new DateOnly(2026, 9, 21);
        var yesterday = new DateOnly(2026, 9, 20);

        var kind = sut.DetermineKind(yesterday, today, lastWelcomeBackAt: null, now: DateTimeOffset.Now);

        Assert.Equal(GreetingKind.Full, kind);
    }

    [Fact]
    public void DetermineKind_SameDay_NoRecentWelcomeBack_ReturnsWelcomeBack()
    {
        var sut = new GreetingService();
        var today = new DateOnly(2026, 9, 21);
        var now = new DateTimeOffset(2026, 9, 21, 14, 0, 0, TimeSpan.Zero);
        var twoHoursAgo = now.AddHours(-2);

        var kind = sut.DetermineKind(today, today, twoHoursAgo, now);

        Assert.Equal(GreetingKind.WelcomeBack, kind);
    }

    [Fact]
    public void DetermineKind_SameDay_WelcomedBackWithinTheHour_ReturnsNone()
    {
        var sut = new GreetingService();
        var today = new DateOnly(2026, 9, 21);
        var now = new DateTimeOffset(2026, 9, 21, 14, 0, 0, TimeSpan.Zero);
        var tenMinutesAgo = now.AddMinutes(-10);

        var kind = sut.DetermineKind(today, today, tenMinutesAgo, now);

        Assert.Equal(GreetingKind.None, kind);
    }

    [Theory]
    [InlineData(8, "Good morning")]
    [InlineData(14, "Good afternoon")]
    [InlineData(20, "Good evening")]
    public void BuildMessage_Full_VariesByTimeOfDay(int hour, string expectedPrefix)
    {
        var sut = new GreetingService();
        var now = new DateTimeOffset(2026, 9, 21, hour, 0, 0, TimeSpan.Zero);

        var message = sut.BuildMessage(GreetingKind.Full, "Alex", now);

        Assert.StartsWith(expectedPrefix + ", Alex!", message);
    }

    [Fact]
    public void BuildMessage_Full_BlankUserName_FallsBackToThere()
    {
        var sut = new GreetingService();

        var message = sut.BuildMessage(GreetingKind.Full, "", new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.Zero));

        Assert.StartsWith("Good morning, there!", message);
    }

    [Fact]
    public void BuildMessage_WelcomeBack_IsShort()
    {
        var sut = new GreetingService();

        var message = sut.BuildMessage(GreetingKind.WelcomeBack, "Alex", DateTimeOffset.Now);

        Assert.Equal("Welcome back!", message);
    }
}
