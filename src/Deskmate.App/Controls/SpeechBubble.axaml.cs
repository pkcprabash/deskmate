using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Deskmate.App.Controls;

/// <summary>
/// A message bubble with two optional button slots (e.g. Done/Snooze),
/// hidden when their text isn't set. Not wired to any avatar behavior yet —
/// later phases (break suggestions, reminder alerts) will use it.
/// </summary>
public partial class SpeechBubble : UserControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<SpeechBubble, string?>(nameof(Message));

    public static readonly StyledProperty<string?> PrimaryButtonTextProperty =
        AvaloniaProperty.Register<SpeechBubble, string?>(nameof(PrimaryButtonText));

    public static readonly StyledProperty<string?> SecondaryButtonTextProperty =
        AvaloniaProperty.Register<SpeechBubble, string?>(nameof(SecondaryButtonText));

    public event EventHandler? PrimaryButtonClicked;
    public event EventHandler? SecondaryButtonClicked;

    public SpeechBubble()
    {
        InitializeComponent();
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? PrimaryButtonText
    {
        get => GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public string? SecondaryButtonText
    {
        get => GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MessageProperty)
        {
            MessageText.Text = Message;
        }
        else if (change.Property == PrimaryButtonTextProperty)
        {
            PrimaryButton.Content = PrimaryButtonText;
            PrimaryButton.IsVisible = !string.IsNullOrEmpty(PrimaryButtonText);
        }
        else if (change.Property == SecondaryButtonTextProperty)
        {
            SecondaryButton.Content = SecondaryButtonText;
            SecondaryButton.IsVisible = !string.IsNullOrEmpty(SecondaryButtonText);
        }
    }

    private void OnPrimaryButtonClick(object? sender, RoutedEventArgs e) =>
        PrimaryButtonClicked?.Invoke(this, EventArgs.Empty);

    private void OnSecondaryButtonClick(object? sender, RoutedEventArgs e) =>
        SecondaryButtonClicked?.Invoke(this, EventArgs.Empty);
}
