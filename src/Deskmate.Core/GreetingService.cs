using System;
using Deskmate.Core.Models;

namespace Deskmate.Core;

public enum GreetingKind
{
    None,
    Full,
    WelcomeBack,
}

/// <summary>
/// Decides whether the avatar should greet the user right now, and what to say.
/// No UI or clock dependency, so it's fully unit-testable.
/// </summary>
public class GreetingService
{
    private static readonly TimeSpan WelcomeBackCooldown = TimeSpan.FromHours(1);

    /// <summary>
    /// The first greeting of a new day is the full "Good morning, Alex!" message.
    /// Later unlocks the same day get a lighter "Welcome back!", at most once an hour.
    /// </summary>
    public GreetingKind DetermineKind(DateOnly? lastGreetingDate, DateOnly today, DateTimeOffset? lastWelcomeBackAt, DateTimeOffset now)
    {
        if (lastGreetingDate != today)
        {
            return GreetingKind.Full;
        }

        if (lastWelcomeBackAt is { } last && now - last < WelcomeBackCooldown)
        {
            return GreetingKind.None;
        }

        return GreetingKind.WelcomeBack;
    }

    public string BuildMessage(GreetingKind kind, string userName, DateTimeOffset now, MessageTone tone)
    {
        var name = string.IsNullOrWhiteSpace(userName) ? "there" : userName;

        if (kind != GreetingKind.Full)
        {
            return tone switch
            {
                MessageTone.Calm => "Welcome back.",
                MessageTone.Minimal => "Back.",
                _ => "Welcome back!",
            };
        }

        if (tone == MessageTone.Minimal)
        {
            return $"{TimeOfDayGreeting(now)}, {name}.";
        }

        var isWeekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if (isWeekend)
        {
            var weekendGreeting = now.DayOfWeek == DayOfWeek.Saturday ? "Happy Saturday" : "Happy Sunday";
            return tone == MessageTone.Calm
                ? $"{weekendGreeting}, {name}. Hope you get some rest."
                : $"{weekendGreeting}, {name}! Enjoy your day off.";
        }

        var timeOfDayGreeting = TimeOfDayGreeting(now);
        return tone == MessageTone.Calm
            ? $"{timeOfDayGreeting}, {name}. Hope you have a peaceful day."
            : $"{timeOfDayGreeting}, {name}! Are you ready to ace your day?";
    }

    private static string TimeOfDayGreeting(DateTimeOffset now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening",
    };
}
