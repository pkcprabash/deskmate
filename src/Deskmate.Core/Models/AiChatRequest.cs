using System.Collections.Generic;

namespace Deskmate.Core.Models;

/// <summary>
/// Everything an <c>IAiChatGenerator</c> needs to reply to one message in a chat with the
/// avatar. No I/O here, so prompt-building stays easy to test.
/// </summary>
/// <param name="History">Earlier turns in this conversation, oldest first, not including <paramref name="UserMessage"/>.</param>
public sealed record AiChatRequest(
    string AvatarName,
    string UserName,
    MessageTone Tone,
    string CurrentFocus,
    string UserMessage,
    IReadOnlyList<ChatMessage> History);
