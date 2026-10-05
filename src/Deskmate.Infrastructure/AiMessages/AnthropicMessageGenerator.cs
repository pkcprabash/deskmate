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
/// Asks Claude to rewrite the avatar's line for the moment, using the user's own Anthropic API
/// key (Settings > AI messages). Every failure mode — no key, a network error, a slow reply, an
/// empty or oddly-shaped one — falls back to null rather than throwing or blocking the avatar,
/// since <see cref="AiMessageRequest.Fallback"/> always has a perfectly good static line ready.
/// </summary>
public sealed class AnthropicMessageGenerator(ILogger<AnthropicMessageGenerator> logger) : IAiMessageGenerator
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
    private const int MaxReplyLength = 200;

    public async Task<string?> GenerateAsync(AiMessageRequest request, string? apiKey, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };
            var parameters = new MessageCreateParams
            {
                Model = string.IsNullOrWhiteSpace(model) ? "claude-haiku-4-5" : model,
                MaxTokens = 100,
                System = BuildSystemPrompt(request),
                Messages = [new() { Role = Role.User, Content = BuildUserPrompt(request) }],
            };

            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var createTask = client.Messages.Create(parameters);
            var completed = await Task.WhenAny(createTask, Task.Delay(Timeout.Infinite, linkedCts.Token));
            if (completed != createTask)
            {
                logger.LogWarning("AI message generation timed out after {Timeout}.", RequestTimeout);
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
            logger.LogWarning(ex, "AI message generation failed; using the built-in phrasing instead.");
            return null;
        }
    }

    public static string BuildSystemPrompt(AiMessageRequest request) =>
        $"""
        You write exactly one short line of dialogue for {request.AvatarName}, a friendly animated
        desktop companion who keeps {request.UserName} company while they work. Reply with the
        line itself and nothing else: no quotation marks, no markdown, no explanation, one
        sentence, under 15 words. Match a {request.Tone.ToString().ToLowerInvariant()} tone.
        """;

    public static string BuildUserPrompt(AiMessageRequest request) => request.Kind switch
    {
        AiMessageKind.Greeting when request.IsFullGreeting =>
            $"Greet {request.UserName} for the first time today. It's {request.TimeOfDay}" +
            (request.IsWeekend ? " on a weekend." : ".") +
            (string.IsNullOrWhiteSpace(request.CurrentFocus) ? "" : $" They're currently focused on: {request.CurrentFocus}.") +
            (request.DueTodayCount > 0
                ? $" Mention briefly that they have {request.DueTodayCount} reminder{(request.DueTodayCount == 1 ? "" : "s")} due today."
                : ""),
        AiMessageKind.Greeting =>
            $"{request.UserName} is back after being away for a bit today. Say a brief, light 'welcome back'.",
        AiMessageKind.BreakSuggestion =>
            $"{request.UserName} has been working for a while. Gently suggest they take a break.",
        AiMessageKind.PomodoroFocusStarted =>
            $"A {request.Minutes}-minute focus session just started" +
            (string.IsNullOrWhiteSpace(request.CurrentFocus) ? "." : $" for: {request.CurrentFocus}.") +
            " Encourage them into it.",
        AiMessageKind.PomodoroBreakStarted =>
            $"A {request.Minutes}-minute {(request.IsLongBreak ? "long " : "")}break just started after a focus session. Congratulate them and tell them to enjoy the break.",
        AiMessageKind.PomodoroStopped => "The focus session was just stopped early. Acknowledge it kindly.",
        _ => "Say something encouraging.",
    };
}
