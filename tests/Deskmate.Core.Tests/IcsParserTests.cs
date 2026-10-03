using System;
using System.Linq;
using Deskmate.Core;
using Deskmate.Core.Models;

namespace Deskmate.Core.Tests;

public class IcsParserTests
{
    private const string Preamble = "BEGIN:VCALENDAR\nVERSION:2.0\nPRODID:-//Test//Test//EN\n";
    private const string Postamble = "END:VCALENDAR\n";

    private static string Wrap(string veventBody) => Preamble + "BEGIN:VEVENT\n" + veventBody + "\nEND:VEVENT\n" + Postamble;

    [Fact]
    public void Parse_EmptyOrWhitespace_ReturnsNoEvents()
    {
        Assert.Empty(IcsParser.Parse(""));
        Assert.Empty(IcsParser.Parse("   "));
    }

    [Fact]
    public void Parse_AllDayEvent_ReturnsDateOnlyNoTime()
    {
        var ics = Wrap("UID:abc-1\nSUMMARY:Team offsite\nDTSTART;VALUE=DATE:20261005");

        var events = IcsParser.Parse(ics);

        var evt = Assert.Single(events);
        Assert.Equal("abc-1", evt.ExternalId);
        Assert.Equal("Team offsite", evt.Title);
        Assert.Equal(new DateOnly(2026, 10, 5), evt.Date);
        Assert.Null(evt.Time);
    }

    [Fact]
    public void Parse_FloatingLocalDateTime_ReadsWallClockValue()
    {
        var ics = Wrap("UID:abc-2\nSUMMARY:Standup\nDTSTART:20261005T090000");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 5), evt.Date);
        Assert.Equal(new TimeOnly(9, 0, 0), evt.Time);
    }

    [Fact]
    public void Parse_UtcDateTime_ConvertsToLocalTime()
    {
        var ics = Wrap("UID:abc-3\nSUMMARY:Call\nDTSTART:20261005T140000Z");

        var evt = Assert.Single(IcsParser.Parse(ics));

        var expectedLocal = DateTime.SpecifyKind(new DateTime(2026, 10, 5, 14, 0, 0), DateTimeKind.Utc).ToLocalTime();
        Assert.Equal(DateOnly.FromDateTime(expectedLocal), evt.Date);
        Assert.Equal(TimeOnly.FromDateTime(expectedLocal), evt.Time);
    }

    [Fact]
    public void Parse_TzidParameter_IsIgnoredButValueStillParses()
    {
        var ics = Wrap("UID:abc-4\nSUMMARY:Lunch\nDTSTART;TZID=America/New_York:20261005T120000");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 5), evt.Date);
        Assert.Equal(new TimeOnly(12, 0, 0), evt.Time);
    }

    [Fact]
    public void Parse_FoldedSummaryLine_IsUnfoldedBeforeParsing()
    {
        // RFC 5545 line folding: a continuation line starts with a single space, which is
        // stripped on unfolding — a real space at the fold point needs a second one here.
        var ics = Wrap("UID:abc-5\nSUMMARY:A very long meeting title that wraps\n  across two lines\nDTSTART;VALUE=DATE:20261005");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal("A very long meeting title that wraps across two lines", evt.Title);
    }

    [Theory]
    [InlineData("Buy milk\\, eggs", "Buy milk, eggs")]
    [InlineData("Q1\\; Q2 review", "Q1; Q2 review")]
    [InlineData("Line one\\nLine two", "Line one Line two")]
    [InlineData("C:\\\\path\\\\to\\\\file", "C:\\path\\to\\file")]
    public void Parse_UnescapesSummaryText(string raw, string expected)
    {
        var ics = Wrap($"UID:abc-6\nSUMMARY:{raw}\nDTSTART;VALUE=DATE:20261005");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(expected, evt.Title);
    }

    [Fact]
    public void Parse_MissingSummary_FallsBackToPlaceholderTitle()
    {
        var ics = Wrap("UID:abc-7\nDTSTART;VALUE=DATE:20261005");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal("(untitled event)", evt.Title);
    }

    [Fact]
    public void Parse_MissingUid_SkipsEvent()
    {
        var ics = Wrap("SUMMARY:No UID\nDTSTART;VALUE=DATE:20261005");

        Assert.Empty(IcsParser.Parse(ics));
    }

    [Fact]
    public void Parse_MissingDtStart_SkipsEvent()
    {
        var ics = Wrap("UID:abc-8\nSUMMARY:No date");

        Assert.Empty(IcsParser.Parse(ics));
    }

    [Fact]
    public void Parse_SimpleRecurringEvent_IsImportedAsRecurring()
    {
        var ics = Wrap("UID:abc-9\nSUMMARY:Weekly sync\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;COUNT=5");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(RecurrenceType.Weekly, evt.Recurrence);
        Assert.Equal(new DateOnly(2026, 11, 2), evt.RecurrenceEndDate); // the 5th weekly occurrence from Oct 5
    }

    [Theory]
    [InlineData("DAILY", RecurrenceType.Daily)]
    [InlineData("daily", RecurrenceType.Daily)] // FREQ is case-insensitive
    [InlineData("WEEKLY", RecurrenceType.Weekly)]
    [InlineData("MONTHLY", RecurrenceType.Monthly)]
    [InlineData("YEARLY", RecurrenceType.Yearly)]
    public void Parse_RRuleFrequencies_MapToTheMatchingRecurrenceType(string freq, RecurrenceType expected)
    {
        var ics = Wrap($"UID:freq-1\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ={freq}");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(expected, evt.Recurrence);
        Assert.Null(evt.RecurrenceEndDate); // no COUNT/UNTIL: recurs indefinitely
    }

    [Fact]
    public void Parse_RRuleWithUntilDateOnly_SetsRecurrenceEndDate()
    {
        var ics = Wrap("UID:until-1\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=DAILY;UNTIL=20261010");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 10), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_RRuleWithUntilUtcDateTime_ConvertsToLocalDate()
    {
        var ics = Wrap("UID:until-2\nSUMMARY:Event\nDTSTART:20261005T090000\nRRULE:FREQ=DAILY;UNTIL=20261010T235900Z");

        var evt = Assert.Single(IcsParser.Parse(ics));

        var expectedLocal = DateTime.SpecifyKind(new DateTime(2026, 10, 10, 23, 59, 0), DateTimeKind.Utc).ToLocalTime();
        Assert.Equal(DateOnly.FromDateTime(expectedLocal), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_RRuleWithExplicitIntervalOne_IsStillSupported()
    {
        var ics = Wrap("UID:interval-1\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=DAILY;INTERVAL=1;COUNT=3");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 7), evt.RecurrenceEndDate);
    }

    [Theory]
    [InlineData("FREQ=WEEKLY;INTERVAL=2;COUNT=5")] // every other week: interval != 1
    [InlineData("FREQ=WEEKLY;BYDAY=1MO,WE")] // an ordinal prefix isn't valid for WEEKLY
    [InlineData("FREQ=WEEKLY;BYDAY=XX")] // not a real weekday code
    [InlineData("FREQ=WEEKLY;BYDAY=")] // empty BYDAY
    [InlineData("FREQ=MONTHLY;BYDAY=1MO,3MO")] // multiple ordinal/weekday pairs: unsupported
    [InlineData("FREQ=MONTHLY;BYDAY=MO")] // a weekday with no ordinal prefix, on MONTHLY
    [InlineData("FREQ=MONTHLY;BYDAY=0TH")] // ordinal 0 isn't valid RRULE syntax
    [InlineData("FREQ=MONTHLY;BYDAY=6TH")] // out of the realistic 1-5 range
    [InlineData("FREQ=MONTHLY;BYDAY=3XX")] // not a real weekday code
    [InlineData("FREQ=DAILY;BYDAY=MO")] // BYDAY on DAILY isn't a recognized shape
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=15")] // a specific day of the month
    [InlineData("FREQ=YEARLY;BYMONTH=12")]
    [InlineData("FREQ=DAILY;COUNT=5;UNTIL=20261010")] // COUNT and UNTIL together: malformed per RFC 5545
    [InlineData("FREQ=SECONDLY;COUNT=5")] // an unsupported frequency
    [InlineData("COUNT=5")] // no FREQ at all
    [InlineData("FREQ=DAILY;COUNT=abc")] // non-numeric COUNT
    [InlineData("FREQ=DAILY;UNTIL=not-a-date")]
    [InlineData("not even key-value pairs")]
    public void Parse_UnsupportedOrMalformedRRule_SkipsTheEventEntirely(string rrule)
    {
        var ics = Wrap($"UID:unsupported-1\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:{rrule}");

        Assert.Empty(IcsParser.Parse(ics));
    }

    [Fact]
    public void Parse_RRuleWithZeroCount_IsRejected()
    {
        var ics = Wrap("UID:zero-count\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=DAILY;COUNT=0");

        Assert.Empty(IcsParser.Parse(ics));
    }

    [Fact]
    public void Parse_RRuleWithWkst_IsIgnoredButStillSupported()
    {
        var ics = Wrap("UID:wkst-1\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;WKST=SU;COUNT=2");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(RecurrenceType.Weekly, evt.Recurrence);
    }

    [Fact]
    public void Parse_WeeklyByDay_SetsRecurrenceWeekdays()
    {
        var ics = Wrap("UID:byday-1\nSUMMARY:Standup\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(RecurrenceType.Weekly, evt.Recurrence);
        Assert.Equal(DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday | DaysOfWeekFlags.Friday, evt.RecurrenceWeekdays);
        Assert.Null(evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_WeeklyByDay_IsCaseInsensitive()
    {
        var ics = Wrap("UID:byday-2\nSUMMARY:Standup\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;BYDAY=mo,we");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday, evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_WeeklyByDay_WithCount_SetsRecurrenceEndDateToTheNthMatchingWeekday()
    {
        // Starting Monday Oct 5 2026, Mon/Wed/Fri: Oct5, Oct7, Oct9, Oct12, Oct14 -> the 5th is Oct 14.
        var ics = Wrap("UID:byday-3\nSUMMARY:Standup\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR;COUNT=5");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 14), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_WeeklyByDay_WithUntil_SetsRecurrenceEndDate()
    {
        var ics = Wrap("UID:byday-4\nSUMMARY:Standup\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20261020");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 10, 20), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_PlainWeeklyRRule_LeavesRecurrenceWeekdaysNull()
    {
        var ics = Wrap("UID:byday-5\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;COUNT=3");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Null(evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_NonWeeklyRRule_DoesNotSetRecurrenceWeekdays()
    {
        var ics = Wrap("UID:byday-6\nSUMMARY:Event\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=DAILY;COUNT=3");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Null(evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_MonthlyOrdinalByDay_SetsOrdinalAndWeekday()
    {
        // October 15, 2026 is the 3rd Thursday of the month.
        var ics = Wrap("UID:ordinal-1\nSUMMARY:Board meeting\nDTSTART;VALUE=DATE:20261015\nRRULE:FREQ=MONTHLY;BYDAY=3TH");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(RecurrenceType.Monthly, evt.Recurrence);
        Assert.Equal(3, evt.RecurrenceOrdinal);
        Assert.Equal(DaysOfWeekFlags.Thursday, evt.RecurrenceWeekdays);
        Assert.Null(evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_NegativeOrdinalByDay_MeansCountingFromTheEnd()
    {
        // October 30, 2026 is the last Friday of the month.
        var ics = Wrap("UID:ordinal-2\nSUMMARY:Game night\nDTSTART;VALUE=DATE:20261030\nRRULE:FREQ=MONTHLY;BYDAY=-1FR");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(-1, evt.RecurrenceOrdinal);
        Assert.Equal(DaysOfWeekFlags.Friday, evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_YearlyOrdinalByDay_IsSupported()
    {
        // November 26, 2026 is the 4th Thursday of November — Thanksgiving-style.
        var ics = Wrap("UID:ordinal-3\nSUMMARY:Thanksgiving\nDTSTART;VALUE=DATE:20261126\nRRULE:FREQ=YEARLY;BYDAY=4TH");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(RecurrenceType.Yearly, evt.Recurrence);
        Assert.Equal(4, evt.RecurrenceOrdinal);
        Assert.Equal(DaysOfWeekFlags.Thursday, evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_MonthlyOrdinalByDay_IsCaseInsensitive()
    {
        var ics = Wrap("UID:ordinal-4\nSUMMARY:Board meeting\nDTSTART;VALUE=DATE:20261015\nRRULE:FREQ=MONTHLY;BYDAY=3th");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(DaysOfWeekFlags.Thursday, evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_MonthlyOrdinalByDay_WithCount_SetsRecurrenceEndDateToTheNthOccurrence()
    {
        // 3rd Thursday: Oct 15, Nov 19, Dec 17 2026 -> the 3rd one is Dec 17.
        var ics = Wrap("UID:ordinal-5\nSUMMARY:Board meeting\nDTSTART;VALUE=DATE:20261015\nRRULE:FREQ=MONTHLY;BYDAY=3TH;COUNT=3");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 12, 17), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_YearlyOrdinalByDay_WithCount_SetsRecurrenceEndDateToTheNthYear()
    {
        // 4th Thursday of November: 2026-11-26, then 2027-11-25.
        var ics = Wrap("UID:ordinal-6\nSUMMARY:Thanksgiving\nDTSTART;VALUE=DATE:20261126\nRRULE:FREQ=YEARLY;BYDAY=4TH;COUNT=2");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2027, 11, 25), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_MonthlyOrdinalByDay_WithUntil_SetsRecurrenceEndDate()
    {
        var ics = Wrap("UID:ordinal-7\nSUMMARY:Board meeting\nDTSTART;VALUE=DATE:20261015\nRRULE:FREQ=MONTHLY;BYDAY=3TH;UNTIL=20261231");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal(new DateOnly(2026, 12, 31), evt.RecurrenceEndDate);
    }

    [Fact]
    public void Parse_WeeklyByDay_LeavesRecurrenceOrdinalNull()
    {
        var ics = Wrap("UID:ordinal-8\nSUMMARY:Standup\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Null(evt.RecurrenceOrdinal);
    }

    [Fact]
    public void Parse_PlainMonthlyRRule_LeavesRecurrenceOrdinalNull()
    {
        var ics = Wrap("UID:ordinal-9\nSUMMARY:Rent due\nDTSTART;VALUE=DATE:20261015\nRRULE:FREQ=MONTHLY;COUNT=3");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Null(evt.RecurrenceOrdinal);
        Assert.Null(evt.RecurrenceWeekdays);
    }

    [Fact]
    public void Parse_CancelledEvent_IsSkipped()
    {
        var ics = Wrap("UID:abc-10\nSUMMARY:Cancelled meeting\nDTSTART;VALUE=DATE:20261005\nSTATUS:CANCELLED");

        Assert.Empty(IcsParser.Parse(ics));
    }

    [Fact]
    public void Parse_MultipleEvents_ReturnsAllValidOnes()
    {
        var ics = Preamble
            + "BEGIN:VEVENT\nUID:multi-1\nSUMMARY:First\nDTSTART;VALUE=DATE:20261005\nEND:VEVENT\n"
            + "BEGIN:VEVENT\nUID:multi-2\nSUMMARY:Second\nDTSTART;VALUE=DATE:20261006\nEND:VEVENT\n"
            + Postamble;

        var events = IcsParser.Parse(ics);

        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.ExternalId == "multi-1" && e.Title == "First");
        Assert.Contains(events, e => e.ExternalId == "multi-2" && e.Title == "Second");
    }

    [Fact]
    public void Parse_CrlfLineEndings_ParsesCorrectly()
    {
        var ics = Wrap("UID:abc-11\nSUMMARY:CRLF test\nDTSTART;VALUE=DATE:20261005").Replace("\n", "\r\n");

        var evt = Assert.Single(IcsParser.Parse(ics));

        Assert.Equal("CRLF test", evt.Title);
    }
}
