using System;

namespace Deskmate.Core;

/// <summary>Whether "now" falls within a quiet-hours window. Handles ranges that wrap past midnight.</summary>
public static class QuietHours
{
    public static bool IsWithin(TimeOnly? start, TimeOnly? end, TimeOnly now)
    {
        if (start is not { } s || end is not { } e || s == e)
        {
            return false;
        }

        return s < e
            ? now >= s && now < e
            : now >= s || now < e; // wraps past midnight, e.g. 22:00-07:00
    }
}
