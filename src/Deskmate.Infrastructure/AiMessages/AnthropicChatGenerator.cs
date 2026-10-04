using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;
using Deskmate.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deskmate.Infrastructure.AiMessages;

/// <summary>
/// Replies to a chat message as the avatar, using the user's own Anthropic API key
/// (Settings > AI messages). Every failure mode — no key, a network error, a slow reply, an
/// empty or oversized one — returns null rather than throwing, so the chat window can show a
/// plain "couldn't reply" message instead of breaking.
/// </summary>
public sealed class AnthropicChatGenerator(ILogger<AnthropicChatGenerator> logger) : IAiChatGenerator
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private const int MaxReplyLength = 800;

    public async Task<string?> ReplyAsync(AiChatRequest request, string? apiKey, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return null;
        }

        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };
            var parameters = new MessageCreateParams
            {
                Model = string.IsNullOrWhiteSpace(model) ? "claude-haiku-4-5" : model,
                MaxTokens = 300,
                System = BuildSystemPrompt(request),
                Messages =
                [
                    ..request.History.Select(m => new MessageParam
                    {
                        Role = m.Role == ChatRole.User ? Role.User : Role.Assistant,
                        Content = m.Text,
                    }),
                    new() { Role = Role.User, Content = request.UserMessage },
                ],
            };

            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var createTask = client.Messages.Create(parameters);
            var completed = await Task.WhenAny(createTask, Task.Delay(Timeout.Infinite, linkedCts.Token));
            if (completed != createTask)
            {
                logger.LogWarning("AI chat reply timed out after {Timeout}.", RequestTimeout);
                return null;
            }

            var response = await createTask;
            var text = response.Content
                .Select(block => block.Value)
                .OfType<TextBlock>()
                .Select(block => block.Text)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                ?.Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxReplyLength)
            {
                return null;
            }

            return text;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI chat reply failed.");
            return null;
        }
    }

    public static string BuildSystemPrompt(AiChatRequest request) =>
        $"""
        You are {request.AvatarName}, a friendly animated desktop companion chatting with
        {request.UserName} while they work. Reply conversationally, in a
        {request.Tone.ToString().ToLowerInvariant()} tone, in a sentence or two — no markdown,
        no stage directions, just what you'd say out loud.
        """ + (string.IsNullOrWhiteSpace(request.CurrentFocus) ? "" : $" They're currently focused on: {request.CurrentFocus}.");
}
