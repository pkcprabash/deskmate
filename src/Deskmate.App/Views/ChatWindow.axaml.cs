using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.AiMessages;
using Deskmate.Infrastructure.Data;

namespace Deskmate.App.Views;

/// <summary>
/// A small chat window opened from the avatar's quick menu. Reuses the same Anthropic API key
/// and "About me" context as AI messages (Settings > AI messages); when AI messages aren't
/// turned on or configured, the input is replaced with a note pointing there instead. The
/// conversation lives only in this window, for as long as it stays open.
/// </summary>
public partial class ChatWindow : Window
{
    private static readonly IBrush UserBubbleBrush = new SolidColorBrush(Color.FromArgb(0x33, 0x40, 0xA0, 0xFF));
    private static readonly IBrush AvatarBubbleBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x80, 0x80, 0x80));

    /// <summary>How many prior messages (not turns) are sent as context with each new one.</summary>
    private const int MaxHistoryMessages = 20;

    public required SettingsService SettingsService { get; init; }
    public required IAiChatGenerator ChatGenerator { get; init; }

    private readonly List<ChatMessage> _history = [];
    private string _avatarName = "";
    private string _userName = "";
    private MessageTone _tone = MessageTone.Cheerful;
    private string _currentFocus = "";
    private string? _apiKey;
    private string _model = "";
    private bool _awaitingReply;

    public ChatWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        var settings = await SettingsService.GetOrCreateAsync();
        _avatarName = settings.AvatarName;
        _userName = settings.UserName;
        _tone = settings.Tone;
        _currentFocus = settings.CurrentFocus;
        _apiKey = settings.AiApiKey;
        _model = settings.AiModel;

        Title = $"Chat with {_avatarName}";

        var isAvailable = settings.AiMessagesEnabled && !string.IsNullOrWhiteSpace(_apiKey);
        UnavailableText.IsVisible = !isAvailable;
        InputTextBox.IsEnabled = isAvailable;
        SendButton.IsEnabled = isAvailable;

        if (isAvailable)
        {
            InputTextBox.Focus();
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        _ = SendAsync();
    }

    private void OnSendClicked(object? sender, RoutedEventArgs e) => _ = SendAsync();

    private async Task SendAsync()
    {
        var text = InputTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(text) || _awaitingReply)
        {
            return;
        }

        InputTextBox.Text = "";
        AppendMessage(ChatRole.User, text);
        var historyForRequest = _history.ToArray();
        RememberMessage(new ChatMessage(ChatRole.User, text));

        _awaitingReply = true;
        InputTextBox.IsEnabled = false;
        SendButton.IsEnabled = false;
        var thinkingBubble = AppendMessage(ChatRole.Avatar, "…");

        var request = new AiChatRequest(_avatarName, _userName, _tone, _currentFocus, text, historyForRequest);
        var reply = await ChatGenerator.ReplyAsync(request, _apiKey, _model);
        var replyText = reply ?? "Sorry, I couldn't think of a reply just now.";

        MessagesPanel.Children.Remove(thinkingBubble);
        AppendMessage(ChatRole.Avatar, replyText);
        RememberMessage(new ChatMessage(ChatRole.Avatar, replyText));

        _awaitingReply = false;
        InputTextBox.IsEnabled = true;
        SendButton.IsEnabled = true;
        InputTextBox.Focus();
    }

    /// <summary>Keeps only the most recent messages, so a long-running chat doesn't send an ever-growing prompt.</summary>
    private void RememberMessage(ChatMessage message)
    {
        _history.Add(message);
        if (_history.Count > MaxHistoryMessages)
        {
            _history.RemoveAt(0);
        }
    }

    private Border AppendMessage(ChatRole role, string text)
    {
        var bubble = new Border
        {
            Background = role == ChatRole.User ? UserBubbleBrush : AvatarBubbleBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 6),
            MaxWidth = 260,
            HorizontalAlignment = role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
        };

        MessagesPanel.Children.Add(bubble);
        Dispatcher.UIThread.Post(() => MessagesScrollViewer.ScrollToEnd(), DispatcherPriority.Background);
        return bubble;
    }
}
