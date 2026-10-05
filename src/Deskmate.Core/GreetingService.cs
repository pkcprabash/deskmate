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

    /// <param name="dueTodayCount">
    /// How many reminders are due today, mentioned at the end of a <see cref="GreetingKind.Full"/>
    /// greeting (never on a lighter <see cref="GreetingKind.WelcomeBack"/>). Zero omits the mention.
    /// </param>
    public string BuildMessage(GreetingKind kind, string userName, DateTimeOffset now, MessageTone tone, int dueTodayCount = 0)
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

        string greeting;
        if (tone == MessageTone.Minimal)
        {
            greeting = $"{TimeOfDayGreeting(now)}, {name}.";
        }
        else
        {
            var isWeekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if (isWeekend)
            {
                var weekendGreeting = now.DayOfWeek == DayOfWeek.Saturday ? "Happy Saturday" : "Happy Sunday";
                greeting = tone == MessageTone.Calm
                    ? $"{weekendGreeting}, {name}. Hope you get some rest."
                    : $"{weekendGreeting}, {name}! Enjoy your day off.";
            }
            else
            {
                var timeOfDayGreeting = TimeOfDayGreeting(now);
                greeting = tone == MessageTone.Calm
                    ? $"{timeOfDayGreeting}, {name}. Hope you have a peaceful day."
                    : $"{timeOfDayGreeting}, {name}! Are you ready to ace your day?";
            }
        }

        return greeting + BuildDueTodaySuffix(dueTodayCount, tone);
    }

    private static string BuildDueTodaySuffix(int dueTodayCount, MessageTone tone)
    {
        if (dueTodayCount <= 0)
        {
            return "";
        }

        var noun = dueTodayCount == 1 ? "reminder" : "reminders";

        return tone switch
        {
            MessageTone.Minimal => $" {dueTodayCount} today.",
            MessageTone.Calm => $" You have {dueTodayCount} {noun} today.",
            _ => $" You've got {dueTodayCount} {noun} today!",
        };
    }

    private static string TimeOfDayGreeting(DateTimeOffset now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening",
    };

    /// <summary>"morning"/"afternoon"/"evening" and weekend-ness, for callers (like AI message
    /// prompts) that want the same facts this class uses without duplicating the hour cutoffs.</summary>
    public static (string TimeOfDay, bool IsWeekend) DescribeNow(DateTimeOffset now)
    {
        var timeOfDay = now.Hour switch
        {
            < 12 => "morning",
            < 18 => "afternoon",
            _ => "evening",
        };

        return (timeOfDay, now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
    }
}
