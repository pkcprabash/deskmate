using System.Collections.Generic;
using System.Threading.Tasks;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.AiMessages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskmate.Core.Tests;

public class AnthropicChatGeneratorTests
{
    private static AiChatRequest Request(
        string currentFocus = "", string userMessage = "How's it going?", IReadOnlyList<ChatMessage>? history = null,
        IReadOnlyList<string>? dueTodayReminders = null) => new(
        AvatarName: "Pixel", UserName: "Alex", Tone: MessageTone.Cheerful, CurrentFocus: currentFocus,
        UserMessage: userMessage, History: history ?? [], DueTodayReminders: dueTodayReminders ?? []);

    [Fact]
    public void SystemPrompt_NamesTheAvatarAndUser()
    {
        var prompt = AnthropicChatGenerator.BuildSystemPrompt(Request());

        Assert.Contains("Pixel", prompt);
        Assert.Contains("Alex", prompt);
    }

    [Theory]
    [InlineData(MessageTone.Cheerful, "cheerful")]
    [InlineData(MessageTone.Calm, "calm")]
    [InlineData(MessageTone.Minimal, "minimal")]
    public void SystemPrompt_MentionsTheConfiguredTone(MessageTone tone, string expectedWord)
    {
        var request = Request() with { Tone = tone };

        var prompt = AnthropicChatGenerator.BuildSystemPrompt(request);

        Assert.Contains(expectedWord, prompt);
    }

    [Fact]
    public void SystemPrompt_IncludesCurrentFocusWhenSet()
    {
        var prompt = AnthropicChatGenerator.BuildSystemPrompt(Request(currentFocus: "shipping the release"));

        Assert.Contains("shipping the release", prompt);
    }

    [Fact]
    public void SystemPrompt_OmitsFocusMentionWhenBlank()
    {
        var prompt = AnthropicChatGenerator.BuildSystemPrompt(Request(currentFocus: ""));

        Assert.DoesNotContain("focused on", prompt);
    }

    [Fact]
    public void SystemPrompt_MentionsTodaysReminderTitlesWhenSet()
    {
        var prompt = AnthropicChatGenerator.BuildSystemPrompt(Request(dueTodayReminders: ["Pay rent", "Call dentist"]));

        Assert.Contains("Pay rent", prompt);
        Assert.Contains("Call dentist", prompt);
    }

    [Fact]
    public void SystemPrompt_OmitsRemindersClauseWhenNoneDueToday()
    {
        var prompt = AnthropicChatGenerator.BuildSystemPrompt(Request(dueTodayReminders: []));

        Assert.DoesNotContain("Today's reminders", prompt);
    }

    [Fact]
    public async Task ReplyAsync_NoApiKey_ReturnsNullWithoutCallingTheNetwork()
    {
        var sut = new AnthropicChatGenerator(NullLogger<AnthropicChatGenerator>.Instance);

        var result = await sut.ReplyAsync(Request(), apiKey: null, model: "claude-haiku-4-5");

        Assert.Null(result);
    }

    [Fact]
    public async Task ReplyAsync_EmptyMessage_ReturnsNullWithoutCallingTheNetwork()
    {
        var sut = new AnthropicChatGenerator(NullLogger<AnthropicChatGenerator>.Instance);

        var result = await sut.ReplyAsync(Request(userMessage: "   "), apiKey: "key", model: "claude-haiku-4-5");

        Assert.Null(result);
    }

    [Fact]
    public async Task ReplyAsync_NoApiKey_WithHistory_ReturnsNullWithoutCallingTheNetwork()
    {
        var sut = new AnthropicChatGenerator(NullLogger<AnthropicChatGenerator>.Instance);
        var history = new[] { new ChatMessage(ChatRole.User, "Hey"), new ChatMessage(ChatRole.Avatar, "Hi there!") };

        var result = await sut.ReplyAsync(Request(history: history), apiKey: null, model: "claude-haiku-4-5");

        Assert.Null(result);
    }
}
