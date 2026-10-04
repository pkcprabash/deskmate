using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core.Models;

namespace Deskmate.Infrastructure.AiMessages;

/// <summary>
/// Replies to one message in a chat with the avatar. Implementations must never throw: on any
/// failure (no key configured, network error, timeout, malformed response) they return null so
/// the chat window can show a plain "couldn't reply" message instead of breaking.
/// </summary>
public interface IAiChatGenerator
{
    /// <param name="apiKey">The user's own Anthropic API key. A null/empty key always yields a null result.</param>
    /// <param name="model">The model id to use, e.g. "claude-haiku-4-5".</param>
    Task<string?> ReplyAsync(AiChatRequest request, string? apiKey, string model, CancellationToken cancellationToken = default);
}
