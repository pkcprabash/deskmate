using System;
using System.Linq;
using Deskmate.Core;
using Deskmate.Core.Models;

namespace Deskmate.Core.Tests;

public class ReminderRulesTests
{
    [Fact]
    public void GenerateOccurrenceDates_None_ReturnsSingleDateWhenInRange()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5), Recurrence = RecurrenceType.None };

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)).ToList();

        Assert.Equal([new DateOnly(2026, 10, 5)], dates);
    }

    [Fact]
    public void GenerateOccurrenceDates_None_EmptyWhenOutsideRange()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 9, 1), Recurrence = RecurrenceType.None };

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)).ToList();

        Assert.Empty(dates);
    }

    [Fact]
    public void GenerateOccurrenceDates_Daily_ReturnsEveryDayInRange()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 1), Recurrence = RecurrenceType.Daily };

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 5)).ToList();

        Assert.Equal([new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 5)], dates);
    }

    [Fact]
    public void GenerateOccurrenceDates_Weekly_ReturnsSameWeekday()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 1), Recurrence = RecurrenceType.Weekly }; // Thursday

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 22)).ToList();

        Assert.Equal(
            [new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 22)],
            dates);
    }

    [Fact]
    public void GenerateOccurrenceDates_Monthly_ClampsToShorterMonth()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 1, 31), Recurrence = RecurrenceType.Monthly };

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)).ToList();

        Assert.Equal([new DateOnly(2026, 2, 28)], dates);
    }

    [Fact]
    public void GenerateOccurrenceDates_Yearly_ClampsLeapDayOnNonLeapYear()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2024, 2, 29), Recurrence = RecurrenceType.Yearly };

        var dates = sut.GenerateOccurrenceDates(reminder, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).ToList();

        Assert.Equal([new DateOnly(2026, 2, 28)], dates);
    }

    [Fact]
    public void DetermineAlertKind_DayBefore_WhenTodayIsOneDayBefore()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5), AlertDayBefore = true };
        var occurrence = new ReminderOccurrence { Date = reminder.Date };
        var now = new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.DayBefore, sut.DetermineAlertKind(reminder, occurrence, now));
    }

    [Fact]
    public void DetermineAlertKind_OnDay_NoTimeSet_FiresAnytimeOnTheDay()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5), AlertOnDay = true };
        var occurrence = new ReminderOccurrence { Date = reminder.Date };
        var now = new DateTimeOffset(2026, 10, 5, 0, 1, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.OnDay, sut.DetermineAlertKind(reminder, occurrence, now));
    }

    [Fact]
    public void DetermineAlertKind_OnDay_WithTimeSet_WaitsUntilThatTime()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5), AlertOnDay = true, Time = new TimeOnly(14, 0) };
        var occurrence = new ReminderOccurrence { Date = reminder.Date };
        var tooEarly = new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
        var dueTime = new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.None, sut.DetermineAlertKind(reminder, occurrence, tooEarly));
        Assert.Equal(ReminderAlertKind.OnDay, sut.DetermineAlertKind(reminder, occurrence, dueTime));
    }

    [Fact]
    public void DetermineAlertKind_Advance_FiresAlertBeforeInterval()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder
        {
            Date = new DateOnly(2026, 10, 5),
            Time = new TimeOnly(14, 0),
            AlertBefore = TimeSpan.FromHours(1),
        };
        var occurrence = new ReminderOccurrence { Date = reminder.Date };
        var now = new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.Advance, sut.DetermineAlertKind(reminder, occurrence, now));
    }

    [Fact]
    public void DetermineAlertKind_Overdue_WhenPastDueAndNotCompleted()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5) };
        var occurrence = new ReminderOccurrence { Date = reminder.Date, OnDayShown = true };
        var now = new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.Overdue, sut.DetermineAlertKind(reminder, occurrence, now));
    }

    [Fact]
    public void DetermineAlertKind_Completed_NeverAlerts()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5) };
        var occurrence = new ReminderOccurrence { Date = reminder.Date, Completed = true };
        var now = new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.None, sut.DetermineAlertKind(reminder, occurrence, now));
    }

    [Fact]
    public void DetermineAlertKind_Snoozed_SuppressedUntilSnoozeExpires()
    {
        var sut = new ReminderRules();
        var reminder = new Reminder { Date = new DateOnly(2026, 10, 5), AlertOnDay = true };
        var snoozedUntil = new DateTimeOffset(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);
        var occurrence = new ReminderOccurrence { Date = reminder.Date, SnoozedUntil = snoozedUntil };

        var stillSnoozed = new DateTimeOffset(2026, 10, 5, 9, 30, 0, TimeSpan.Zero);
        var afterSnooze = new DateTimeOffset(2026, 10, 5, 10, 1, 0, TimeSpan.Zero);

        Assert.Equal(ReminderAlertKind.None, sut.DetermineAlertKind(reminder, occurrence, stillSnoozed));
        Assert.Equal(ReminderAlertKind.OnDay, sut.DetermineAlertKind(reminder, occurrence, afterSnooze));
    }

    [Fact]
    public void MarkShown_SetsTheCorrespondingFlag()
    {
        var sut = new ReminderRules();
        var occurrence = new ReminderOccurrence();

        sut.MarkShown(occurrence, ReminderAlertKind.OnDay);

        Assert.True(occurrence.OnDayShown);
        Assert.False(occurrence.DayBeforeShown);
    }
}
