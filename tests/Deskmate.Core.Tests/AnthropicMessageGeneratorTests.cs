using System.Threading.Tasks;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.AiMessages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskmate.Core.Tests;

public class AnthropicMessageGeneratorTests
{
    private static AiMessageRequest Request(
        AiMessageKind kind, string currentFocus = "", bool isFullGreeting = false, int minutes = 0, bool isLongBreak = false,
        int dueTodayCount = 0) => new(
        kind, Fallback: "fallback text", AvatarName: "Pixel", UserName: "Alex", Tone: MessageTone.Cheerful,
        CurrentFocus: currentFocus, TimeOfDay: "morning", IsWeekend: false, isFullGreeting, minutes, isLongBreak, dueTodayCount);

    [Fact]
    public void SystemPrompt_NamesTheAvatarAndUser()
    {
        var prompt = AnthropicMessageGenerator.BuildSystemPrompt(Request(AiMessageKind.Greeting));

        Assert.Contains("Pixel", prompt);
        Assert.Contains("Alex", prompt);
    }

    [Theory]
    [InlineData(MessageTone.Cheerful, "cheerful")]
    [InlineData(MessageTone.Calm, "calm")]
    [InlineData(MessageTone.Minimal, "minimal")]
    public void SystemPrompt_MentionsTheConfiguredTone(MessageTone tone, string expectedWord)
    {
        var request = Request(AiMessageKind.Greeting) with { Tone = tone };

        var prompt = AnthropicMessageGenerator.BuildSystemPrompt(request);

        Assert.Contains(expectedWord, prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_MentionsTimeOfDay()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: true));

        Assert.Contains("morning", prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_IncludesCurrentFocusWhenSet()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, currentFocus: "shipping the release", isFullGreeting: true));

        Assert.Contains("shipping the release", prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_OmitsFocusMentionWhenBlank()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, currentFocus: "", isFullGreeting: true));

        Assert.DoesNotContain("focused on", prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_MentionsDueTodayCountWhenSet()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: true, dueTodayCount: 3));

        Assert.Contains("3 reminders due today", prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_SingularReminderWording()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: true, dueTodayCount: 1));

        Assert.Contains("1 reminder due today", prompt);
    }

    [Fact]
    public void UserPrompt_FullGreeting_OmitsDueTodayMentionWhenZero()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: true, dueTodayCount: 0));

        Assert.DoesNotContain("due today", prompt);
    }

    [Fact]
    public void UserPrompt_LightGreeting_DiffersFromFullGreeting()
    {
        var full = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: true));
        var light = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.Greeting, isFullGreeting: false));

        Assert.NotEqual(full, light);
        Assert.Contains("welcome back", light, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserPrompt_PomodoroFocusStarted_MentionsMinutes()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.PomodoroFocusStarted, minutes: 25));

        Assert.Contains("25", prompt);
    }

    [Theory]
    [InlineData(false, "long")]
    [InlineData(true, "long")]
    public void UserPrompt_PomodoroBreakStarted_LongBreakIsMentionedOnlyWhenLong(bool isLong, string longWord)
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.PomodoroBreakStarted, minutes: 5, isLongBreak: isLong));

        Assert.Equal(isLong, prompt.Contains(longWord));
    }

    [Fact]
    public void UserPrompt_PomodoroStopped_IsNonEmpty()
    {
        var prompt = AnthropicMessageGenerator.BuildUserPrompt(Request(AiMessageKind.PomodoroStopped));

        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    [Fact]
    public async Task GenerateAsync_NoApiKey_ReturnsNullWithoutCallingTheNetwork()
    {
        var sut = new AnthropicMessageGenerator(NullLogger<AnthropicMessageGenerator>.Instance);

        var result = await sut.GenerateAsync(Request(AiMessageKind.Greeting), apiKey: null, model: "claude-haiku-4-5");

        Assert.Null(result);
    }
}
