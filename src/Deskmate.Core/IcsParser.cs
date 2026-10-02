using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>
/// Reads VEVENTs out of an ICS/iCalendar feed (RFC 5545) — the format behind the "secret
/// address" / private subscription URL every major calendar (Google, Outlook, Apple) can hand
/// out, which needs no OAuth app registration to use. No I/O here, so it's fully unit-testable
/// against raw ICS text.
///
/// Known, deliberate limitations:
/// - Only the plain FREQ=DAILY/WEEKLY/MONTHLY/YEARLY recurrence shapes (interval 1, an optional
///   COUNT or UNTIL, nothing else) are expanded. Any RRULE with BYDAY, an interval other than
///   1, or any other rule part is left alone, since getting a date wrong is worse than skipping
///   the event entirely.
/// - A DTSTART with an explicit UTC "Z" suffix is converted to local time; a "floating" local
///   date-time (no "Z") is read as its literal wall-clock value — any TZID parameter is not
///   resolved. Correct for the common case (the feed already in the viewer's zone); can be
///   off by the zone difference otherwise.
/// </summary>
public static class IcsParser
{
    public static IReadOnlyList<CalendarEvent> Parse(string icsText)
    {
        var events = new List<CalendarEvent>();
        if (string.IsNullOrWhiteSpace(icsText))
        {
            return events;
        }

        foreach (var eventLines in SplitIntoEvents(UnfoldLines(icsText)))
        {
            if (TryParseEvent(eventLines, out var calendarEvent))
            {
                events.Add(calendarEvent);
            }
        }

        return events;
    }

    /// <summary>RFC 5545 line folding: a line starting with a space or tab continues the previous one.</summary>
    private static List<string> UnfoldLines(string icsText)
    {
        var lines = icsText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var unfolded = new List<string>();

        foreach (var line in lines)
        {
            if (unfolded.Count > 0 && line.Length > 0 && (line[0] == ' ' || line[0] == '\t'))
            {
                unfolded[^1] += line[1..];
            }
            else if (line.Length > 0)
            {
                unfolded.Add(line);
            }
        }

        return unfolded;
    }

    private static IEnumerable<List<string>> SplitIntoEvents(List<string> lines)
    {
        List<string>? current = null;

        foreach (var line in lines)
        {
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                current = [];
            }
            else if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null)
                {
                    yield return current;
                }

                current = null;
            }
            else
            {
                current?.Add(line);
            }
        }
    }

    private static bool TryParseEvent(List<string> lines, out CalendarEvent calendarEvent)
    {
        calendarEvent = null!;

        string? uid = null;
        string? summary = null;
        string? status = null;
        string? dtStartProperty = null;
        string? dtStartValue = null;
        string? rruleValue = null;

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            var propertyName = line[..colonIndex];
            var value = line[(colonIndex + 1)..];
            var bareName = propertyName.Split(';')[0].ToUpperInvariant();

            switch (bareName)
            {
                case "UID":
                    uid = value.Trim();
                    break;
                case "SUMMARY":
                    summary = UnescapeText(value);
                    break;
                case "STATUS":
                    status = value.Trim();
                    break;
                case "RRULE":
                    rruleValue = value.Trim();
                    break;
                case "DTSTART":
                    dtStartProperty = propertyName;
                    dtStartValue = value.Trim();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(uid) || dtStartValue is null
            || string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryParseDtStart(dtStartProperty!, dtStartValue, out var date, out var time))
        {
            return false;
        }

        var recurrence = RecurrenceType.None;
        DateOnly? recurrenceEndDate = null;

        if (rruleValue is not null && !TryParseRRule(rruleValue, date, out recurrence, out recurrenceEndDate))
        {
            return false;
        }

        var title = string.IsNullOrWhiteSpace(summary) ? "(untitled event)" : summary;
        calendarEvent = new CalendarEvent(uid, title, date, time, recurrence, recurrenceEndDate);
        return true;
    }

    private static readonly HashSet<string> SupportedRRuleKeys =
        new(StringComparer.OrdinalIgnoreCase) { "FREQ", "INTERVAL", "COUNT", "UNTIL", "WKST" };

    /// <summary>
    /// Parses only the RRULE shapes listed in this class's doc comment. Returns false for
    /// anything else — including a well-formed but unsupported rule — so the caller skips the
    /// event rather than import it on the wrong dates.
    /// </summary>
    private static bool TryParseRRule(string rruleValue, DateOnly dtStartDate, out RecurrenceType recurrence, out DateOnly? recurrenceEndDate)
    {
        recurrence = RecurrenceType.None;
        recurrenceEndDate = null;

        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in rruleValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex < 0)
            {
                return false;
            }

            parts[part[..equalsIndex].Trim()] = part[(equalsIndex + 1)..].Trim();
        }

        foreach (var key in parts.Keys)
        {
            if (!SupportedRRuleKeys.Contains(key))
            {
                return false;
            }
        }

        if (!parts.TryGetValue("FREQ", out var freq))
        {
            return false;
        }

        recurrence = freq.ToUpperInvariant() switch
        {
            "DAILY" => RecurrenceType.Daily,
            "WEEKLY" => RecurrenceType.Weekly,
            "MONTHLY" => RecurrenceType.Monthly,
            "YEARLY" => RecurrenceType.Yearly,
            _ => RecurrenceType.None,
        };

        if (recurrence == RecurrenceType.None)
        {
            return false;
        }

        if (parts.TryGetValue("INTERVAL", out var intervalText) && (!int.TryParse(intervalText, out var interval) || interval != 1))
        {
            return false;
        }

        var hasCount = parts.TryGetValue("COUNT", out var countText);
        var hasUntil = parts.TryGetValue("UNTIL", out var untilText);

        if (hasCount && hasUntil)
        {
            return false; // RFC 5545 forbids both together; treat as malformed rather than guess which wins.
        }

        if (hasCount)
        {
            if (!int.TryParse(countText, out var count) || count < 1)
            {
                return false;
            }

            recurrenceEndDate = RecurrenceStep.NthOccurrence(dtStartDate, recurrence, count - 1);
        }
        else if (hasUntil)
        {
            if (!TryParseDateOrDateTime(untilText!, out var untilDate, out _))
            {
                return false;
            }

            recurrenceEndDate = untilDate;
        }

        return true;
    }

    private static bool TryParseDtStart(string propertyName, string value, out DateOnly date, out TimeOnly? time)
    {
        var isDateOnly = propertyName.Contains("VALUE=DATE", StringComparison.OrdinalIgnoreCase)
            && !propertyName.Contains("VALUE=DATE-TIME", StringComparison.OrdinalIgnoreCase);

        if (isDateOnly)
        {
            time = null;
            return DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        return TryParseDateOrDateTime(value, out date, out time);
    }

    /// <summary>Shared by DTSTART (without an explicit VALUE=DATE) and RRULE's UNTIL, whose format mirrors it.</summary>
    private static bool TryParseDateOrDateTime(string value, out DateOnly date, out TimeOnly? time)
    {
        date = default;
        time = null;

        if (value.Length == 8 && !value.Contains('T'))
        {
            return DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        var isUtc = value.EndsWith('Z');
        var localComponent = isUtc ? value[..^1] : value;

        if (!DateTime.TryParseExact(
                localComponent, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        if (isUtc)
        {
            var local = DateTime.SpecifyKind(parsed, DateTimeKind.Utc).ToLocalTime();
            date = DateOnly.FromDateTime(local);
            time = TimeOnly.FromDateTime(local);
        }
        else
        {
            date = DateOnly.FromDateTime(parsed);
            time = TimeOnly.FromDateTime(parsed);
        }

        return true;
    }

    /// <summary>Undoes RFC 5545 text escaping (\\n, \\,, \;, \\\\), collapsing a line break to a space.</summary>
    private static string UnescapeText(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                switch (value[i + 1])
                {
                    case 'n' or 'N':
                        builder.Append(' ');
                        i++;
                        continue;
                    case ',' or ';' or '\\':
                        builder.Append(value[i + 1]);
                        i++;
                        continue;
                }
            }

            builder.Append(value[i]);
        }

        return builder.ToString().Trim();
    }
}
