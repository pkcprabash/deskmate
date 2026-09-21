using System;
using Avalonia.Controls;

namespace Deskmate.App.Views;

/// <summary>
/// A small borderless window hosting a <see cref="Controls.SpeechBubble"/>, shown next to
/// <see cref="AvatarWindow"/> for prompts that need buttons (e.g. break suggestions). A
/// separate top-level window, rather than content inside <see cref="AvatarWindow"/>, since
/// that window is a fixed 180x180 and can't grow to fit a bubble.
/// </summary>
public partial class SpeechBubbleWindow : Window
{
    public SpeechBubbleWindow()
    {
        InitializeComponent();
    }

    public void Configure(string message, string primaryButtonText, string secondaryButtonText,
        Action onPrimaryClicked, Action onSecondaryClicked)
    {
        Bubble.Message = message;
        Bubble.PrimaryButtonText = primaryButtonText;
        Bubble.SecondaryButtonText = secondaryButtonText;
        Bubble.PrimaryButtonClicked += (_, _) => onPrimaryClicked();
        Bubble.SecondaryButtonClicked += (_, _) => onSecondaryClicked();
    }
}
