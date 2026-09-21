using System;

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

    public string BuildMessage(GreetingKind kind, string userName, DateTimeOffset now)
    {
        if (kind != GreetingKind.Full)
        {
            return "Welcome back!";
        }

        var timeOfDayGreeting = now.Hour switch
        {
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening",
        };

        var name = string.IsNullOrWhiteSpace(userName) ? "there" : userName;
        return $"{timeOfDayGreeting}, {name}! Are you ready to ace your day?";
    }
}
