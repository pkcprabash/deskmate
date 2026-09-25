using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Deskmate.App.ViewModels;
using Deskmate.Core;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Display;
using Deskmate.Infrastructure.Startup;

namespace Deskmate.App.Views;

public partial class AvatarWindow : Window
{
    private const int ScreenMargin = 16;
    private const double DragThreshold = 4;
    private const double BaseSize = 180;
    private static readonly TimeSpan BehaviorTickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan GreetingDuration = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan FullScreenCheckInterval = TimeSpan.FromSeconds(3);

    public required SettingsService SettingsService { get; init; }
    public required IStartupRegistration StartupRegistration { get; init; }
    public required ReminderService ReminderService { get; init; }
    public required IFullScreenDetector FullScreenDetector { get; init; }

    private bool _pointerDown;
    private bool _movedBeyondThreshold;
    private int _pressClickCount;
    private PixelPoint _dragStartPointerScreenPosition;
    private PixelPoint _dragStartWindowPosition;
    private bool _isPausedHidden;
    private bool _isFullScreenHidden;
    private DispatcherTimer? _behaviorTimer;
    private DispatcherTimer? _fullScreenCheckTimer;
    private SpeechBubbleWindow? _breakBubble;
    private SpeechBubbleWindow? _greetingBubble;
    private SpeechBubbleWindow? _reminderBubble;

    public AvatarWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not AvatarViewModel viewModel)
        {
            return;
        }

        await viewModel.LoadAsync();
        Width = BaseSize * viewModel.AvatarScale;
        Height = BaseSize * viewModel.AvatarScale;
        Position = ResolveStartupPosition(viewModel);

        viewModel.StateChanged += OnStateChanged;
        viewModel.GreetingReady += OnGreetingReady;
        viewModel.ReminderAlertReady += OnReminderAlertReady;
        viewModel.PausedChanged += OnPausedChanged;
        OnStateChanged(viewModel.CurrentState);
        viewModel.NotifyPossibleGreeting();
        viewModel.NotifyReadyForReminders();

        _behaviorTimer = new DispatcherTimer { Interval = BehaviorTickInterval };
        _behaviorTimer.Tick += (_, _) => viewModel.NotifyTick();
        _behaviorTimer.Start();

        _fullScreenCheckTimer = new DispatcherTimer { Interval = FullScreenCheckInterval };
        _fullScreenCheckTimer.Tick += (_, _) => CheckFullScreen();
        _fullScreenCheckTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _behaviorTimer?.Stop();
        _fullScreenCheckTimer?.Stop();
        _breakBubble?.Close();
        _greetingBubble?.Close();
        _reminderBubble?.Close();
    }

    private void CheckFullScreen()
    {
        bool isFullScreen;
        try
        {
            isFullScreen = FullScreenDetector.IsFullScreenAppActive();
        }
        catch
        {
            // Best-effort: if platform detection misbehaves, just don't hide for it.
            isFullScreen = false;
        }

        if (isFullScreen == _isFullScreenHidden)
        {
            return;
        }

        _isFullScreenHidden = isFullScreen;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_isPausedHidden || _isFullScreenHidden)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void OnStateChanged(AvatarState state)
    {
        if (state != AvatarState.SuggestingBreak)
        {
            _breakBubble?.Close();
            _breakBubble = null;
        }

        if (state != AvatarState.Alerting)
        {
            _reminderBubble?.Close();
            _reminderBubble = null;
        }

        var animationName = state switch
        {
            AvatarState.Waving => "wave",
            AvatarState.Held => "held",
            AvatarState.Typing => "typing",
            AvatarState.Stretching => "stretch",
            AvatarState.SippingCoffee => "coffee",
            AvatarState.LookingAround => "look",
            AvatarState.Sleeping => "sleeping",
            AvatarState.Waking => "wave",
            AvatarState.Yawning => "yawn",
            AvatarState.SuggestingBreak => "sign",
            AvatarState.Alerting => "sign",
            _ => "idle",
        };

        PlayAnimation(animationName);

        if (state == AvatarState.SuggestingBreak)
        {
            ShowBreakBubble();
        }
    }

    private void ShowBreakBubble()
    {
        if (DataContext is not AvatarViewModel viewModel)
        {
            return;
        }

        _breakBubble = CreateBubble(
            viewModel.BreakSuggestionMessage,
            "Sure",
            "Later",
            onPrimaryClicked: viewModel.NotifyBreakAccepted,
            onSecondaryClicked: viewModel.NotifyBreakSnoozed);
    }

    private void OnReminderAlertReady(string message)
    {
        if (DataContext is not AvatarViewModel viewModel)
        {
            return;
        }

        _reminderBubble?.Close();
        _reminderBubble = CreateBubble(
            message,
            "Done",
            "Snooze",
            onPrimaryClicked: viewModel.NotifyReminderDone,
            onSecondaryClicked: viewModel.NotifyReminderSnoozed);
    }

    private void OnPausedChanged(bool isPaused)
    {
        if (isPaused)
        {
            _breakBubble?.Close();
            _breakBubble = null;
            _reminderBubble?.Close();
            _reminderBubble = null;
            _greetingBubble?.Close();
            _greetingBubble = null;
        }

        _isPausedHidden = isPaused;
        UpdateVisibility();
    }

    private void OnGreetingReady(string message)
    {
        _greetingBubble?.Close();
        _greetingBubble = CreateBubble(message, "", "", onPrimaryClicked: () => { }, onSecondaryClicked: () => { });

        var timer = new DispatcherTimer { Interval = GreetingDuration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _greetingBubble?.Close();
            _greetingBubble = null;
        };
        timer.Start();
    }

    private SpeechBubbleWindow CreateBubble(string message, string primaryText, string secondaryText, Action onPrimaryClicked, Action onSecondaryClicked)
    {
        var bubble = new SpeechBubbleWindow();
        bubble.Configure(message, primaryText, secondaryText, onPrimaryClicked, onSecondaryClicked);
        bubble.Opened += (_, _) => PositionBubble(bubble);
        bubble.Show();
        return bubble;
    }

    private void PositionBubble(SpeechBubbleWindow bubble)
    {
        // Position is in device pixels, but sizes are DIPs, so scale before mixing them.
        // bubble.Width/Height stay NaN for a SizeToContent window (they're the explicit
        // size constraint, unset here) — ClientSize is the actual resolved size.
        var scaling = RenderScaling;
        var bubbleSize = bubble.ClientSize;
        var x = Position.X + (Width * scaling - bubbleSize.Width * scaling) / 2;
        var y = Position.Y - bubbleSize.Height * scaling - 8 * scaling;
        bubble.Position = new PixelPoint((int)x, (int)y);
    }

    private void PlayAnimation(string name)
    {
        if (DataContext is not AvatarViewModel viewModel || viewModel.Pack is not { } pack)
        {
            return;
        }

        if (!pack.TryGetAnimation(name, out var animation, out var sheet))
        {
            return;
        }

        Sprite.Play(
            sheet, animation, pack.FrameSize.Width, pack.FrameSize.Height,
            renderWidth: pack.FrameSize.Width * viewModel.AvatarScale,
            renderHeight: pack.FrameSize.Height * viewModel.AvatarScale,
            reducedMotion: viewModel.ReducedMotion);
    }

    private void OnAnimationCompleted(object? sender, EventArgs e)
    {
        (DataContext as AvatarViewModel)?.NotifyAnimationCompleted();
    }

    private PixelPoint ResolveStartupPosition(AvatarViewModel viewModel)
    {
        if (viewModel.SavedPositionX is double x && viewModel.SavedPositionY is double y)
        {
            // The saved position may be on any monitor, not necessarily the primary one.
            var saved = new PixelPoint((int)x, (int)y);
            return ClampToScreen(saved, GetScreenBoundsForPosition(saved));
        }

        var screenBounds = GetPrimaryScreenBounds();
        var defaultX = screenBounds.Right - (int)Width - ScreenMargin;
        var defaultY = screenBounds.Bottom - (int)Height - ScreenMargin;
        return ClampToScreen(new PixelPoint(defaultX, defaultY), screenBounds);
    }

    private PixelRect GetPrimaryScreenBounds() =>
        Screens.Primary?.WorkingArea ?? new PixelRect(0, 0, 1280, 800);

    /// <summary>The working area of whichever monitor currently contains <paramref name="position"/>.</summary>
    private PixelRect GetScreenBoundsForPosition(PixelPoint position) =>
        (Screens.ScreenFromPoint(position) ?? Screens.Primary)?.WorkingArea ?? GetPrimaryScreenBounds();

    private PixelPoint ClampToScreen(PixelPoint position, PixelRect screenBounds)
    {
        var maxX = Math.Max(screenBounds.X, screenBounds.Right - (int)Width);
        var maxY = Math.Max(screenBounds.Y, screenBounds.Bottom - (int)Height);
        var x = Math.Clamp(position.X, screenBounds.X, maxX);
        var y = Math.Clamp(position.Y, screenBounds.Y, maxY);
        return new PixelPoint(x, y);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pointerDown = true;
        _movedBeyondThreshold = false;
        _pressClickCount = e.ClickCount;
        _dragStartPointerScreenPosition = this.PointToScreen(e.GetPosition(this));
        _dragStartWindowPosition = Position;
        e.Pointer.Capture((IInputElement)sender!);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_pointerDown)
        {
            return;
        }

        var currentPointerScreenPosition = this.PointToScreen(e.GetPosition(this));
        var deltaX = currentPointerScreenPosition.X - _dragStartPointerScreenPosition.X;
        var deltaY = currentPointerScreenPosition.Y - _dragStartPointerScreenPosition.Y;

        if (!_movedBeyondThreshold && (Math.Abs(deltaX) > DragThreshold || Math.Abs(deltaY) > DragThreshold))
        {
            _movedBeyondThreshold = true;
            (DataContext as AvatarViewModel)?.NotifyDragStarted();
        }

        if (_movedBeyondThreshold)
        {
            Position = new PixelPoint(_dragStartWindowPosition.X + deltaX, _dragStartWindowPosition.Y + deltaY);
        }
    }

    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pointerDown)
        {
            return;
        }

        _pointerDown = false;
        e.Pointer.Capture(null);

        var viewModel = DataContext as AvatarViewModel;

        if (_movedBeyondThreshold)
        {
            Position = ClampToScreen(Position, GetScreenBoundsForPosition(Position));
            if (viewModel is not null)
            {
                await viewModel.SavePositionAsync(Position.X, Position.Y);
                viewModel.NotifyDragEnded();
            }
        }
        else if (_pressClickCount >= 2)
        {
            QuickMenu.Open(RootSurface);
        }
        else
        {
            viewModel?.NotifyClicked();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter or Key.Space:
                QuickMenu.Open(RootSurface);
                e.Handled = true;
                break;
            case Key.S when e.KeyModifiers == KeyModifiers.None:
                OnSettingsClicked(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void OnQuitClicked(object? sender, RoutedEventArgs e)
    {
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    /// <summary>Entry points for the tray icon's menu, which lives at the Application level.</summary>
    public void ShowSettings() => OnSettingsClicked(this, new RoutedEventArgs());

    public void PauseForOneHour() => (DataContext as AvatarViewModel)?.NotifyPauseFor(TimeSpan.FromHours(1));

    public void PauseUntilTomorrow() => (DataContext as AvatarViewModel)?.NotifyPauseUntilTomorrow();

    public void Resume() => (DataContext as AvatarViewModel)?.NotifyResume();

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        SettingsButton.Opacity = 1;
        SettingsButton.IsHitTestVisible = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        SettingsButton.Opacity = 0;
        SettingsButton.IsHitTestVisible = false;
    }

    private async void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            SettingsService = SettingsService,
            StartupRegistration = StartupRegistration,
            ReminderService = ReminderService,
        };
        settingsWindow.SettingsSaved += OnSettingsSaved;
        await settingsWindow.ShowDialog(this);
    }

    private async void OnSettingsSaved()
    {
        if (DataContext is AvatarViewModel viewModel)
        {
            await viewModel.LoadAsync();
            OnStateChanged(viewModel.CurrentState);
        }
    }
}
