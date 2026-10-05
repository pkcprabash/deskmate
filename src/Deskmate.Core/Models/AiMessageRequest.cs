using System;

namespace Deskmate.Core.Models;

/// <summary>
/// Everything an <c>IAiMessageGenerator</c> needs to write one line in the avatar's voice, plus
/// the deterministic <see cref="Fallback"/> text to fall back to if the AI call is disabled,
/// unconfigured, slow, or fails. No I/O here, so the fields (and the fallback) stay easy to test.
/// </summary>
/// <param name="DueTodayCount">How many reminders are due today; only meaningful alongside <paramref name="IsFullGreeting"/>.</param>
public sealed record AiMessageRequest(
    AiMessageKind Kind,
    string Fallback,
    string AvatarName,
    string UserName,
    MessageTone Tone,
    string CurrentFocus,
    string TimeOfDay,
    bool IsWeekend,
    bool IsFullGreeting = false,
    int Minutes = 0,
    bool IsLongBreak = false,
    int DueTodayCount = 0);
