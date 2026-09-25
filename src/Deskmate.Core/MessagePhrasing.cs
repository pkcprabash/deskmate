using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>Tone-varied phrasing for the short, non-greeting messages the avatar shows.</summary>
public static class MessagePhrasing
{
    public static string BreakSuggestion(MessageTone tone) => tone switch
    {
        MessageTone.Calm => "Maybe time for a short break?",
        MessageTone.Minimal => "Break?",
        _ => "You've been at it for a while. Take a break?",
    };

    public static string PomodoroFocusStarted(MessageTone tone, int minutes) => tone switch
    {
        MessageTone.Calm => $"Let's focus for {minutes} minutes.",
        MessageTone.Minimal => $"Focus: {minutes} min.",
        _ => $"Focus time! {minutes} minutes, you've got this.",
    };

    public static string PomodoroBreakStarted(MessageTone tone, bool isLongBreak, int minutes) => (tone, isLongBreak) switch
    {
        (MessageTone.Calm, false) => $"Nice work. Take a {minutes}-minute breather.",
        (MessageTone.Calm, true) => $"Well done. Enjoy a longer {minutes}-minute rest.",
        (MessageTone.Minimal, false) => $"Break: {minutes} min.",
        (MessageTone.Minimal, true) => $"Long break: {minutes} min.",
        (_, false) => $"Great focus! Take a {minutes}-minute break.",
        (_, true) => $"Four down! Enjoy a long {minutes}-minute break.",
    };

    public static string PomodoroStopped(MessageTone tone) => tone switch
    {
        MessageTone.Calm => "Focus session ended.",
        MessageTone.Minimal => "Stopped.",
        _ => "Focus session stopped. Nice effort!",
    };
}
