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
}
