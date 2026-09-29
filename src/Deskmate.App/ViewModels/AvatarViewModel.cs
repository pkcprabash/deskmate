using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Deskmate.App.Avatars;
using Deskmate.Core;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.AiMessages;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Notifications;
using Deskmate.Infrastructure.Reminders;

namespace Deskmate.App.ViewModels;

public class AvatarViewModel(
    SettingsService settingsService,
    AvatarPackLoader avatarPackLoader,
    IdleMonitor idleMonitor,
    ISessionEventsMonitor sessionEventsMonitor,
    ReminderScheduler reminderScheduler,
    ReminderService reminderService,
    NotificationService notificationService,
    IAiMessageGenerator aiMessageGenerator) : ViewModelBase
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SnoozeDuration = TimeSpan.FromMinutes(10);

    private readonly AvatarStateMachine _stateMachine = new();
    private readonly GreetingService _greetingService = new();
    private readonly PomodoroTimer _pomodoro = new();
    private readonly Queue<(Reminder Reminder, ReminderOccurrence Occurrence, ReminderAlertKind Kind)> _pendingReminderAlerts = new();

    private int _settingsId;
    private DateTimeOffset _workSessionStartedAt = DateTimeOffset.UtcNow;
    private bool _isFirstTick = true;
    private bool _subscribedToBackgroundEvents;
    private string _userName = "";
    private MessageTone _tone = MessageTone.Cheerful;
    private DateOnly? _lastGreetingDate;
    private DateTimeOffset? _lastWelcomeBackAt;
    private TimeOnly? _quietHoursStart;
    private TimeOnly? _quietHoursEnd;
    private DateTimeOffset? _pausedUntil;
    private (Reminder Reminder, ReminderOccurrence Occurrence)? _activeReminderAlert;
    private string _lastPomodoroStatus = "";
    private string _avatarName = "";
    private string _currentFocus = "";
    private bool _aiEnabled;
    private string _aiApiKey = "";
    private string _aiModel = "";

    public double? SavedPositionX { get; private set; }
    public double? SavedPositionY { get; private set; }
    public LoadedAvatarPack? Pack { get; private set; }
    public double AvatarScale { get; private set; } = 1.0;
    public bool ReducedMotion { get; private set; }
    public bool IsPomodoroRunning => _pomodoro.IsRunning;

    /// <summary>Set by the window when the avatar can't be seen (paused or a full-screen app), so messages go to a native notification instead.</summary>
    public bool IsAvatarHidden { get; set; }
    public AvatarState CurrentState => _stateMachine.CurrentState;
    public bool IsPaused => _pausedUntil is { } until && DateTimeOffset.Now < until;


    public event Action<AvatarState>? StateChanged;
    public event Action<string>? GreetingReady;
    public event Action<string>? ReminderAlertReady;
    public event Action<bool>? PausedChanged;
    public event Action<string>? PomodoroMessageReady;

    /// <summary>A break suggestion's message, resolved (possibly by AI) and ready to show with Sure/Later buttons.</summary>
    public event Action<string>? BreakSuggestionReady;

    /// <summary>Raised when the tray tooltip text should change, e.g. "Focus 12 min left" (empty when stopped).</summary>
    public event Action<string>? PomodoroStatusChanged;

    /// <summary>
    /// Loads (or reloads, after the user changes something in Settings) the current
    /// settings into the view model.
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetOrCreateAsync(cancellationToken);
        _settingsId = settings.Id;
        SavedPositionX = settings.PositionX;
        SavedPositionY = settings.PositionY;
        Pack = LoadedAvatarPack.LoadWithFallback(avatarPackLoader, settings.AvatarPack);
        AvatarScale = settings.AvatarScale;
        ReducedMotion = settings.ReducedMotion;
        _stateMachine.SleepAfter = settings.SleepAfter;
        _stateMachine.BreakAfter = settings.BreakAfter;
        _userName = settings.UserName;
        _tone = settings.Tone;
        _lastGreetingDate = settings.LastGreetingDate;
        _quietHoursStart = settings.QuietHoursStart;
        _quietHoursEnd = settings.QuietHoursEnd;
        _avatarName = settings.AvatarName;
        _currentFocus = settings.CurrentFocus;
        _aiEnabled = settings.AiMessagesEnabled;
        _aiApiKey = settings.AiApiKey;
        _aiModel = settings.AiModel;
        _pomodoro.Settings = new PomodoroSettings(
            settings.PomodoroFocus, settings.PomodoroShortBreak, settings.PomodoroLongBreak, settings.PomodoroSessionsBeforeLongBreak);

        if (!_subscribedToBackgroundEvents)
        {
            // SessionResumed and AlertDue both fire from background threads (an OS
            // notification callback, a BackgroundService's timer loop); everything downstream
            // touches Avalonia UI objects, so hop onto the UI thread before handling either.
            sessionEventsMonitor.SessionResumed += (_, _) => Dispatcher.UIThread.Post(OnSessionResumed);
            reminderScheduler.AlertDue += (reminder, occurrence, kind) =>
                Dispatcher.UIThread.Post(() => OnReminderAlertDue(reminder, occurrence, kind));
            _subscribedToBackgroundEvents = true;
        }
    }

    /// <summary>
    /// Delivers any reminder alert that fired before <see cref="LoadAsync"/> subscribed
    /// (the scheduler starts before the window does). Call once the window has finished
    /// its own setup (position, event wiring), so a replayed alert's bubble positions
    /// correctly instead of racing the window's own startup.
    /// </summary>
    public void NotifyReadyForReminders() => reminderScheduler.ReplayUndelivered();

    public Task SavePositionAsync(double x, double y, CancellationToken cancellationToken = default) =>
        settingsService.SavePositionAsync(_settingsId, x, y, cancellationToken);

    public void NotifyClicked() => Transition(AvatarInput.Clicked);

    public void NotifyDragStarted() => Transition(AvatarInput.DragStarted);

    public void NotifyDragEnded() => Transition(AvatarInput.DragEnded);

    public void NotifyAnimationCompleted() => Transition(AvatarInput.AnimationCompleted);

    public void NotifyBreakAccepted()
    {
        _workSessionStartedAt = DateTimeOffset.UtcNow;
        Transition(AvatarInput.BreakAccepted);
    }

    public void NotifyBreakSnoozed()
    {
        _workSessionStartedAt = DateTimeOffset.UtcNow;
        Transition(AvatarInput.BreakSnoozed);
    }

    public void NotifyReminderDone()
    {
        if (_activeReminderAlert is { } active)
        {
            _ = reminderService.MarkOccurrenceCompletedAsync(active.Occurrence.Id);
        }

        _activeReminderAlert = null;
        Transition(AvatarInput.AlertDismissed);
        TryShowNextReminderAlert();
    }

    public void NotifyReminderSnoozed()
    {
        if (_activeReminderAlert is { } active)
        {
            _ = reminderService.SnoozeOccurrenceAsync(active.Occurrence.Id, DateTimeOffset.Now.Add(SnoozeDuration));
        }

        _activeReminderAlert = null;
        Transition(AvatarInput.AlertDismissed);
        TryShowNextReminderAlert();
    }

    public void StartPomodoro()
    {
        if (_pomodoro.IsRunning)
        {
            return;
        }

        _pomodoro.Start(DateTimeOffset.Now);
        AnnouncePomodoroPhase();
        RefreshPomodoroStatus();
    }

    public void StopPomodoro()
    {
        if (!_pomodoro.IsRunning)
        {
            return;
        }

        _pomodoro.Stop();
        _workSessionStartedAt = DateTimeOffset.UtcNow;
        _ = AnnounceAsync(AiMessageKind.PomodoroStopped, MessagePhrasing.PomodoroStopped(_tone), m => PomodoroMessageReady?.Invoke(m));
        RefreshPomodoroStatus();
    }

    public void SkipPomodoroPhase()
    {
        if (_pomodoro.Skip(DateTimeOffset.Now))
        {
            OnPomodoroPhaseChanged();
        }
    }

    private void OnPomodoroPhaseChanged()
    {
        AnnouncePomodoroPhase();
        RefreshPomodoroStatus();
    }

    private void AnnouncePomodoroPhase()
    {
        var settings = _pomodoro.Settings;

        switch (_pomodoro.Phase)
        {
            case PomodoroPhase.Focus:
                var focusMinutes = (int)settings.FocusDuration.TotalMinutes;
                _ = AnnounceAsync(
                    AiMessageKind.PomodoroFocusStarted, MessagePhrasing.PomodoroFocusStarted(_tone, focusMinutes),
                    m => PomodoroMessageReady?.Invoke(m), minutes: focusMinutes);
                break;
            case PomodoroPhase.ShortBreak:
            case PomodoroPhase.LongBreak:
                var isLong = _pomodoro.Phase == PomodoroPhase.LongBreak;
                var duration = isLong ? settings.LongBreakDuration : settings.ShortBreakDuration;
                _workSessionStartedAt = DateTimeOffset.UtcNow;
                // The coffee-sip animation fires immediately; the (possibly AI-rewritten) bubble text can lag a beat behind it.
                Transition(AvatarInput.PomodoroBreakStarted);
                _ = AnnounceAsync(
                    AiMessageKind.PomodoroBreakStarted, MessagePhrasing.PomodoroBreakStarted(_tone, isLong, (int)duration.TotalMinutes),
                    m => PomodoroMessageReady?.Invoke(m), minutes: (int)duration.TotalMinutes, isLongBreak: isLong);
                break;
        }
    }

    /// <summary>Resolves the message (AI if configured, the static fallback otherwise) and then announces it.</summary>
    private async Task AnnounceAsync(AiMessageKind kind, string fallback, Action<string> onVisible, int minutes = 0, bool isLongBreak = false, bool isFullGreeting = false)
    {
        var message = await ResolveMessageAsync(kind, fallback, isFullGreeting, minutes, isLongBreak);
        Announce(message, onVisible);
    }

    /// <summary>
    /// Asks the configured AI message generator to rewrite <paramref name="fallback"/> for this
    /// moment; returns the fallback unchanged if AI messages are off, no key is set, or the call
    /// fails (the generator's contract is to return null rather than throw in that case).
    /// </summary>
    private async Task<string> ResolveMessageAsync(AiMessageKind kind, string fallback, bool isFullGreeting = false, int minutes = 0, bool isLongBreak = false)
    {
        if (!_aiEnabled || string.IsNullOrWhiteSpace(_aiApiKey))
        {
            return fallback;
        }

        var (timeOfDay, isWeekend) = GreetingService.DescribeNow(DateTimeOffset.Now);
        var request = new AiMessageRequest(
            kind, fallback, _avatarName, _userName, _tone, _currentFocus, timeOfDay, isWeekend, isFullGreeting, minutes, isLongBreak);

        var generated = await aiMessageGenerator.GenerateAsync(request, _aiApiKey, _aiModel);
        return generated ?? fallback;
    }

    /// <summary>Speech bubble when the avatar is visible, native notification when it isn't. Silent during quiet hours or pause.</summary>
    private void Announce(string message, Action<string> onVisible)
    {
        if (IsPaused || QuietHours.IsWithin(_quietHoursStart, _quietHoursEnd, TimeOnly.FromDateTime(DateTime.Now)))
        {
            return;
        }

        if (IsAvatarHidden)
        {
            _ = notificationService.ShowAsync("Deskmate", message);
        }
        else
        {
            onVisible(message);
        }
    }

    private void RefreshPomodoroStatus()
    {
        var status = "";
        if (_pomodoro.IsRunning)
        {
            var label = _pomodoro.Phase switch
            {
                PomodoroPhase.Focus => "Focus",
                PomodoroPhase.LongBreak => "Long break",
                _ => "Break",
            };
            var minutesLeft = (int)Math.Ceiling(_pomodoro.Remaining(DateTimeOffset.Now).TotalMinutes);
            status = $"{label}: {minutesLeft} min left";
        }

        if (status == _lastPomodoroStatus)
        {
            return;
        }

        _lastPomodoroStatus = status;
        PomodoroStatusChanged?.Invoke(status);
    }

    /// <summary>Pause mode: the avatar hides and stops noticing anything until it lifts.</summary>
    public void NotifyPauseFor(TimeSpan duration)
    {
        _pausedUntil = DateTimeOffset.Now.Add(duration);
        PausedChanged?.Invoke(true);
    }

    /// <summary>Pauses until the start of the next calendar day (local time).</summary>
    public void NotifyPauseUntilTomorrow()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        _pausedUntil = new DateTimeOffset(tomorrow.ToDateTime(TimeOnly.MinValue));
        PausedChanged?.Invoke(true);
    }

    public void NotifyResume()
    {
        if (_pausedUntil is null)
        {
            return;
        }

        _pausedUntil = null;
        PausedChanged?.Invoke(false);
        TryShowNextReminderAlert();
    }

    /// <summary>
    /// Shows a greeting if one is due: the full "Good morning, Alex!" on the first
    /// unlock/launch of the day, a lighter "Welcome back!" (at most once an hour) later.
    /// Called on app start and whenever the session resumes (unlock/wake).
    /// </summary>
    public void NotifyPossibleGreeting()
    {
        if (IsPaused)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        var kind = _greetingService.DetermineKind(_lastGreetingDate, today, _lastWelcomeBackAt, now);

        if (kind == GreetingKind.None)
        {
            return;
        }

        _lastWelcomeBackAt = now;

        if (kind == GreetingKind.Full)
        {
            _lastGreetingDate = today;
            _ = settingsService.UpdateAsync(_settingsId, s => s.LastGreetingDate = today);
        }

        var fallback = _greetingService.BuildMessage(kind, _userName, now, _tone);
        _ = AnnounceGreetingAsync(fallback, isFullGreeting: kind == GreetingKind.Full);
    }

    private async Task AnnounceGreetingAsync(string fallback, bool isFullGreeting)
    {
        var message = await ResolveMessageAsync(AiMessageKind.Greeting, fallback, isFullGreeting);
        GreetingReady?.Invoke(message);
    }

    /// <summary>
    /// Called on a UI timer, roughly once a second. Keystrokes aren't routed through the
    /// window, so a recent keypress is inferred from <see cref="IdleMonitor"/> instead of
    /// a dedicated event.
    /// </summary>
    public void NotifyTick()
    {
        if (_pausedUntil is { } until)
        {
            if (DateTimeOffset.Now < until)
            {
                return;
            }

            _pausedUntil = null;
            PausedChanged?.Invoke(false);
            TryShowNextReminderAlert();
        }

        if (_pomodoro.Tick(DateTimeOffset.Now))
        {
            OnPomodoroPhaseChanged();
        }
        else
        {
            RefreshPomodoroStatus();
        }

        var idleFor = idleMonitor.GetIdleDuration();

        if (!_isFirstTick && idleFor < TickInterval)
        {
            Transition(AvatarInput.KeyPressed);
        }

        _isFirstTick = false;

        // The Pomodoro cycle schedules the breaks itself, so the generic "you've been working a while" nudge steps aside.
        _stateMachine.SuppressBreakSuggestions = _pomodoro.IsRunning
            || QuietHours.IsWithin(_quietHoursStart, _quietHoursEnd, TimeOnly.FromDateTime(DateTime.Now));

        var previous = _stateMachine.CurrentState;
        _stateMachine.Tick(idleFor, DateTimeOffset.UtcNow - _workSessionStartedAt);

        if (_stateMachine.CurrentState == previous)
        {
            return;
        }

        if (_stateMachine.CurrentState == AvatarState.Sleeping)
        {
            _workSessionStartedAt = DateTimeOffset.UtcNow;
        }

        if (_stateMachine.CurrentState == AvatarState.SuggestingBreak)
        {
            _ = AnnounceAsync(AiMessageKind.BreakSuggestion, MessagePhrasing.BreakSuggestion(_tone), m => BreakSuggestionReady?.Invoke(m));
        }

        StateChanged?.Invoke(_stateMachine.CurrentState);
    }

    private void OnSessionResumed()
    {
        // A locked/asleep Mac still lets the avatar fall asleep on its own; this just
        // wakes it immediately on unlock/resume instead of waiting for the next keypress.
        if (_stateMachine.CurrentState == AvatarState.Sleeping)
        {
            Transition(AvatarInput.KeyPressed);
        }

        NotifyPossibleGreeting();
    }

    private void OnReminderAlertDue(Reminder reminder, ReminderOccurrence occurrence, ReminderAlertKind kind)
    {
        _pendingReminderAlerts.Enqueue((reminder, occurrence, kind));
        _ = notificationService.ShowAsync(reminder.Title, BuildAlertMessage(reminder, kind));
        TryShowNextReminderAlert();
    }

    /// <summary>Only one alert is shown at a time; the rest wait their turn.</summary>
    private void TryShowNextReminderAlert()
    {
        if (IsPaused || _activeReminderAlert is not null || _pendingReminderAlerts.Count == 0)
        {
            return;
        }

        var next = _pendingReminderAlerts.Dequeue();
        _activeReminderAlert = (next.Reminder, next.Occurrence);
        Transition(AvatarInput.ReminderDue);
        ReminderAlertReady?.Invoke(BuildAlertMessage(next.Reminder, next.Kind));
    }

    private static string BuildAlertMessage(Reminder reminder, ReminderAlertKind kind)
    {
        var prefix = kind switch
        {
            ReminderAlertKind.DayBefore => "Tomorrow: ",
            ReminderAlertKind.Overdue => "Still pending: ",
            _ => "",
        };

        return prefix + reminder.Title;
    }

    private void Transition(AvatarInput input)
    {
        var previous = _stateMachine.CurrentState;
        _stateMachine.Apply(input);

        if (_stateMachine.CurrentState != previous)
        {
            StateChanged?.Invoke(_stateMachine.CurrentState);
        }
    }
}
