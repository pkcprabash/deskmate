using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core.Models;

namespace Deskmate.Infrastructure.AiMessages;

/// <summary>
/// Writes one AI-generated line for the avatar to say, in place of the static phrasing in
/// <c>Deskmate.Core.MessagePhrasing</c>. Implementations must never throw: on any failure
/// (no key configured, network error, timeout, malformed response) they return null so the
/// caller falls back to <see cref="AiMessageRequest.Fallback"/> instead of showing nothing.
/// </summary>
public interface IAiMessageGenerator
{
    /// <param name="apiKey">The user's own Anthropic API key. A null/empty key always yields a null result.</param>
    /// <param name="model">The model id to use, e.g. "claude-haiku-4-5".</param>
    Task<string?> GenerateAsync(AiMessageRequest request, string? apiKey, string model, CancellationToken cancellationToken = default);
}
