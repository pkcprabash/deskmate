using System;
using System.Linq;
using Deskmate.Core;

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
    public void Parse_RecurringEvent_IsSkippedRatherThanExpanded()
    {
        var ics = Wrap("UID:abc-9\nSUMMARY:Weekly sync\nDTSTART;VALUE=DATE:20261005\nRRULE:FREQ=WEEKLY;COUNT=5");

        Assert.Empty(IcsParser.Parse(ics));
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
