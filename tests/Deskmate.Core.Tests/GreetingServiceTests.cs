using System;
using Deskmate.Core;
using Deskmate.Core.Models;

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
        var now = new DateTimeOffset(2026, 9, 21, hour, 0, 0, TimeSpan.Zero); // a Monday

        var message = sut.BuildMessage(GreetingKind.Full, "Alex", now, MessageTone.Cheerful);

        Assert.StartsWith(expectedPrefix + ", Alex!", message);
    }

    [Fact]
    public void BuildMessage_Full_BlankUserName_FallsBackToThere()
    {
        var sut = new GreetingService();

        var message = sut.BuildMessage(
            GreetingKind.Full, "", new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.Zero), MessageTone.Cheerful);

        Assert.StartsWith("Good morning, there!", message);
    }

    [Fact]
    public void BuildMessage_WelcomeBack_IsShort()
    {
        var sut = new GreetingService();

        var message = sut.BuildMessage(GreetingKind.WelcomeBack, "Alex", DateTimeOffset.Now, MessageTone.Cheerful);

        Assert.Equal("Welcome back!", message);
    }

    [Theory]
    [InlineData(MessageTone.Cheerful, "Good morning, Alex!")]
    [InlineData(MessageTone.Calm, "Good morning, Alex. Hope you have a peaceful day.")]
    [InlineData(MessageTone.Minimal, "Good morning, Alex.")]
    public void BuildMessage_Full_VariesByTone(MessageTone tone, string expected)
    {
        var sut = new GreetingService();
        var monday = new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);

        var message = sut.BuildMessage(GreetingKind.Full, "Alex", monday, tone);

        Assert.StartsWith(expected, message);
    }

    [Theory]
    [InlineData(2026, 9, 26, "Happy Saturday")] // Saturday
    [InlineData(2026, 9, 27, "Happy Sunday")] // Sunday
    public void BuildMessage_Full_Weekend_UsesWeekendGreeting(int year, int month, int day, string expectedPrefix)
    {
        var sut = new GreetingService();
        var weekend = new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero);

        var message = sut.BuildMessage(GreetingKind.Full, "Alex", weekend, MessageTone.Cheerful);

        Assert.StartsWith(expectedPrefix + ", Alex!", message);
    }

    [Fact]
    public void BuildMessage_Full_Weekend_Minimal_StillUsesTimeOfDay()
    {
        var sut = new GreetingService();
        var saturday = new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero);

        var message = sut.BuildMessage(GreetingKind.Full, "Alex", saturday, MessageTone.Minimal);

        Assert.Equal("Good morning, Alex.", message);
    }
}
