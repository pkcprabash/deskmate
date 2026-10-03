using System;
using System.Collections.Generic;
using Deskmate.Core;
using Deskmate.Core.Models;

namespace Deskmate.Core.Tests;

public class CalendarSyncPlannerTests
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    private static CalendarEvent Event(
        string id, string title, DateOnly date, TimeOnly? time = null,
        RecurrenceType recurrence = RecurrenceType.None, DateOnly? recurrenceEndDate = null,
        DaysOfWeekFlags? recurrenceWeekdays = null) =>
        new(id, title, date, time, recurrence, recurrenceEndDate, recurrenceWeekdays);

    private static SyncedReminderSnapshot Snapshot(
        int reminderId, string externalId, string title, DateOnly date, TimeOnly? time = null,
        RecurrenceType recurrence = RecurrenceType.None, DateOnly? recurrenceEndDate = null,
        DaysOfWeekFlags? recurrenceWeekdays = null) =>
        new(reminderId, externalId, title, date, time, recurrence, recurrenceEndDate, recurrenceWeekdays);

    [Fact]
    public void Plan_NewEvent_IsAdded()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [],
            incoming: [Event("evt-1", "Dentist", Today.AddDays(5))],
            windowStart: Today);

        var added = Assert.Single(plan.ToAdd);
        Assert.Equal("evt-1", added.ExternalId);
        Assert.Empty(plan.ToUpdate);
        Assert.Empty(plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_UnchangedEvent_IsNeitherAddedNorUpdated()
    {
        var date = Today.AddDays(5);
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Dentist", date)],
            incoming: [Event("evt-1", "Dentist", date)],
            windowStart: Today);

        Assert.Empty(plan.ToAdd);
        Assert.Empty(plan.ToUpdate);
        Assert.Empty(plan.ToRemoveReminderIds);
    }

    [Theory]
    [InlineData("Dentist (moved)", 5, null)]
    [InlineData("Dentist", 6, null)]
    public void Plan_ChangedTitleOrDate_IsUpdated(string newTitle, int newDayOffset, int? _)
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Dentist", Today.AddDays(5))],
            incoming: [Event("evt-1", newTitle, Today.AddDays(newDayOffset))],
            windowStart: Today);

        var update = Assert.Single(plan.ToUpdate);
        Assert.Equal(1, update.ReminderId);
        Assert.Equal("evt-1", update.Event.ExternalId);
    }

    [Fact]
    public void Plan_ChangedTime_IsUpdated()
    {
        var date = Today.AddDays(5);
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Standup", date, new TimeOnly(9, 0))],
            incoming: [Event("evt-1", "Standup", date, new TimeOnly(9, 30))],
            windowStart: Today);

        Assert.Single(plan.ToUpdate);
    }

    [Fact]
    public void Plan_EventNoLongerInFeed_UpcomingOne_IsRemoved()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Cancelled meeting", Today.AddDays(3))],
            incoming: [],
            windowStart: Today);

        Assert.Empty(plan.ToAdd);
        Assert.Empty(plan.ToUpdate);
        Assert.Equal([1], plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_EventNoLongerInFeed_ButInThePast_IsLeftAlone()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Old meeting", Today.AddDays(-3))],
            incoming: [],
            windowStart: Today);

        Assert.Empty(plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_EventExactlyOnWindowStart_IsEligibleForRemoval()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Today's meeting", Today)],
            incoming: [],
            windowStart: Today);

        Assert.Equal([1], plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_MixedScenario_ComputesAllThreeBucketsIndependently()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing:
            [
                Snapshot(1, "kept", "Unchanged", Today.AddDays(1)),
                Snapshot(2, "changed", "Old title", Today.AddDays(2)),
                Snapshot(3, "gone", "Removed meeting", Today.AddDays(3)),
            ],
            incoming:
            [
                Event("kept", "Unchanged", Today.AddDays(1)),
                Event("changed", "New title", Today.AddDays(2)),
                Event("brand-new", "New event", Today.AddDays(4)),
            ],
            windowStart: Today);

        Assert.Equal(["brand-new"], (IEnumerable<string>)[.. plan.ToAdd.Select(e => e.ExternalId)]);
        Assert.Equal(2, Assert.Single(plan.ToUpdate).ReminderId);
        Assert.Equal([3], plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_EmptyEverything_ProducesEmptyPlan()
    {
        var plan = CalendarSyncPlanner.Plan(existing: [], incoming: [], windowStart: Today);

        Assert.Empty(plan.ToAdd);
        Assert.Empty(plan.ToUpdate);
        Assert.Empty(plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_NewRecurringEvent_CarriesRecurrenceFields()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [],
            incoming: [Event("evt-1", "Standup", Today.AddDays(1), recurrence: RecurrenceType.Daily)],
            windowStart: Today);

        var added = Assert.Single(plan.ToAdd);
        Assert.Equal(RecurrenceType.Daily, added.Recurrence);
    }

    [Fact]
    public void Plan_RecurrenceTypeChanged_IsUpdated()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Standup", Today.AddDays(1), recurrence: RecurrenceType.Weekly)],
            incoming: [Event("evt-1", "Standup", Today.AddDays(1), recurrence: RecurrenceType.Daily)],
            windowStart: Today);

        Assert.Single(plan.ToUpdate);
    }

    [Fact]
    public void Plan_RecurrenceEndDateChanged_IsUpdated()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Standup", Today, recurrence: RecurrenceType.Daily, recurrenceEndDate: Today.AddDays(10))],
            incoming: [Event("evt-1", "Standup", Today, recurrence: RecurrenceType.Daily, recurrenceEndDate: Today.AddDays(20))],
            windowStart: Today);

        Assert.Single(plan.ToUpdate);
    }

    [Fact]
    public void Plan_RecurrenceWeekdaysChanged_IsUpdated()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Standup", Today, recurrence: RecurrenceType.Weekly, recurrenceWeekdays: DaysOfWeekFlags.Monday)],
            incoming: [Event("evt-1", "Standup", Today, recurrence: RecurrenceType.Weekly, recurrenceWeekdays: DaysOfWeekFlags.Monday | DaysOfWeekFlags.Friday)],
            windowStart: Today);

        Assert.Single(plan.ToUpdate);
    }

    [Fact]
    public void Plan_UnchangedRecurrenceWeekdays_IsNotUpdated()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Standup", Today, recurrence: RecurrenceType.Weekly, recurrenceWeekdays: DaysOfWeekFlags.Monday)],
            incoming: [Event("evt-1", "Standup", Today, recurrence: RecurrenceType.Weekly, recurrenceWeekdays: DaysOfWeekFlags.Monday)],
            windowStart: Today);

        Assert.Empty(plan.ToUpdate);
    }

    [Fact]
    public void Plan_RecurringEventGoneFromFeed_WithOldStartDate_IsStillRemoved()
    {
        // The series started years ago but is still recurring indefinitely (no end date) —
        // its original Date being long past must not protect it from removal.
        var plan = CalendarSyncPlanner.Plan(
            existing: [Snapshot(1, "evt-1", "Old standing meeting", new DateOnly(2020, 1, 1), recurrence: RecurrenceType.Weekly)],
            incoming: [],
            windowStart: Today);

        Assert.Equal([1], plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_RecurringEventGoneFromFeed_ButItsSeriesAlreadyEnded_IsLeftAlone()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing:
            [
                Snapshot(1, "evt-1", "Finished series", new DateOnly(2020, 1, 1),
                    recurrence: RecurrenceType.Weekly, recurrenceEndDate: Today.AddDays(-30)),
            ],
            incoming: [],
            windowStart: Today);

        Assert.Empty(plan.ToRemoveReminderIds);
    }

    [Fact]
    public void Plan_RecurringEventGoneFromFeed_SeriesEndingInTheFuture_IsRemoved()
    {
        var plan = CalendarSyncPlanner.Plan(
            existing:
            [
                Snapshot(1, "evt-1", "Short series", new DateOnly(2020, 1, 1),
                    recurrence: RecurrenceType.Weekly, recurrenceEndDate: Today.AddDays(5)),
            ],
            incoming: [],
            windowStart: Today);

        Assert.Equal([1], plan.ToRemoveReminderIds);
    }
}
