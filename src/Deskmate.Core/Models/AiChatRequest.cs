namespace Deskmate.Core.Models;

/// <summary>
/// Everything an <c>IAiChatGenerator</c> needs to reply to one message in a chat with the
/// avatar. No I/O here, so prompt-building stays easy to test.
/// </summary>
public sealed record AiChatRequest(
    string AvatarName,
    string UserName,
    MessageTone Tone,
    string CurrentFocus,
    string UserMessage);
