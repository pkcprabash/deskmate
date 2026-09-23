using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Deskmate.App.Avatars;
using Deskmate.Core;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Activity;
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
    NotificationService notificationService) : ViewModelBase
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SnoozeDuration = TimeSpan.FromMinutes(10);

    private readonly AvatarStateMachine _stateMachine = new();
    private readonly GreetingService _greetingService = new();
    private readonly Queue<(Reminder Reminder, ReminderOccurrence Occurrence, ReminderAlertKind Kind)> _pendingReminderAlerts = new();

    private int _settingsId;
    private DateTimeOffset _workSessionStartedAt = DateTimeOffset.UtcNow;
    private bool _isFirstTick = true;
    private bool _subscribedToBackgroundEvents;
    private string _userName = "";
    private DateOnly? _lastGreetingDate;
    private DateTimeOffset? _lastWelcomeBackAt;
    private (Reminder Reminder, ReminderOccurrence Occurrence)? _activeReminderAlert;

    public double? SavedPositionX { get; private set; }
    public double? SavedPositionY { get; private set; }
    public LoadedAvatarPack? Pack { get; private set; }
    public AvatarState CurrentState => _stateMachine.CurrentState;

    public event Action<AvatarState>? StateChanged;
    public event Action<string>? GreetingReady;
    public event Action<string>? ReminderAlertReady;

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
        Pack = LoadedAvatarPack.Load(avatarPackLoader, settings.AvatarPack);
        _stateMachine.SleepAfter = settings.SleepAfter;
        _stateMachine.BreakAfter = settings.BreakAfter;
        _userName = settings.UserName;
        _lastGreetingDate = settings.LastGreetingDate;

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

    /// <summary>
    /// Shows a greeting if one is due: the full "Good morning, Alex!" on the first
    /// unlock/launch of the day, a lighter "Welcome back!" (at most once an hour) later.
    /// Called on app start and whenever the session resumes (unlock/wake).
    /// </summary>
    public void NotifyPossibleGreeting()
    {
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

        GreetingReady?.Invoke(_greetingService.BuildMessage(kind, _userName, now));
    }

    /// <summary>
    /// Called on a UI timer, roughly once a second. Keystrokes aren't routed through the
    /// window, so a recent keypress is inferred from <see cref="IdleMonitor"/> instead of
    /// a dedicated event.
    /// </summary>
    public void NotifyTick()
    {
        var idleFor = idleMonitor.GetIdleDuration();

        if (!_isFirstTick && idleFor < TickInterval)
        {
            Transition(AvatarInput.KeyPressed);
        }

        _isFirstTick = false;

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
        if (_activeReminderAlert is not null || _pendingReminderAlerts.Count == 0)
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
