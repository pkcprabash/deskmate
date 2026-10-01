using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Calendar;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Startup;

namespace Deskmate.App.Views;

public partial class SettingsWindow : Window
{
    private int _settingsId;
    private List<Reminder> _reminders = [];

    public required SettingsService SettingsService { get; init; }
    public required IStartupRegistration StartupRegistration { get; init; }
    public required ReminderService ReminderService { get; init; }
    public required CalendarSyncService CalendarSyncService { get; init; }

    /// <summary>Raised after a successful save, so the avatar window can reload the settings it caches.</summary>
    public event Action? SettingsSaved;

    public SettingsWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        foreach (var toneName in Enum.GetNames<MessageTone>())
        {
            ToneComboBox.Items.Add(toneName);
        }

        foreach (var packName in new AvatarPackLoader().ListValidPacks())
        {
            AvatarPackComboBox.Items.Add(packName);
        }

        var settings = await SettingsService.GetOrCreateAsync();
        _settingsId = settings.Id;

        AvatarPackComboBox.SelectedItem = settings.AvatarPack;
        AvatarNameTextBox.Text = settings.AvatarName;
        AvatarScaleSlider.Value = settings.AvatarScale;
        UserNameTextBox.Text = settings.UserName;
        CurrentFocusTextBox.Text = settings.CurrentFocus;
        WorkStartPicker.SelectedTime = settings.WorkStart.ToTimeSpan();
        WorkEndPicker.SelectedTime = settings.WorkEnd.ToTimeSpan();
        ToneComboBox.SelectedItem = settings.Tone.ToString();
        SleepAfterMinutes.Value = (decimal)settings.SleepAfter.TotalMinutes;
        BreakAfterMinutes.Value = (decimal)settings.BreakAfter.TotalMinutes;
        StartAtLoginCheckBox.IsChecked = settings.StartAtLogin;
        ReducedMotionCheckBox.IsChecked = settings.ReducedMotion;
        PomodoroFocusMinutes.Value = (decimal)settings.PomodoroFocus.TotalMinutes;
        PomodoroShortBreakMinutes.Value = (decimal)settings.PomodoroShortBreak.TotalMinutes;
        PomodoroLongBreakMinutes.Value = (decimal)settings.PomodoroLongBreak.TotalMinutes;
        PomodoroSessionsBeforeLongBreak.Value = settings.PomodoroSessionsBeforeLongBreak;
        AiMessagesEnabledCheckBox.IsChecked = settings.AiMessagesEnabled;
        AiApiKeyTextBox.Text = settings.AiApiKey;
        AiModelTextBox.Text = settings.AiModel;
        QuietHoursEnabledCheckBox.IsChecked = settings.QuietHoursStart is not null && settings.QuietHoursEnd is not null;
        QuietHoursStartPicker.SelectedTime = (settings.QuietHoursStart ?? new TimeOnly(22, 0)).ToTimeSpan();
        QuietHoursEndPicker.SelectedTime = (settings.QuietHoursEnd ?? new TimeOnly(7, 0)).ToTimeSpan();

        CalendarIcsUrlTextBox.Text = settings.CalendarIcsUrl;
        UpdateCalendarSyncStatusText(settings.LastCalendarSyncAt, settings.LastCalendarSyncError);

        await ReloadRemindersAsync();
    }

    private void UpdateCalendarSyncStatusText(DateTimeOffset? lastSyncedAt, string? lastError)
    {
        if (!string.IsNullOrWhiteSpace(lastError))
        {
            CalendarSyncStatusText.Text = $"Last sync failed: {lastError}";
        }
        else if (lastSyncedAt is { } at)
        {
            CalendarSyncStatusText.Text = $"Last synced {at.LocalDateTime:MMM d, h:mm tt}.";
        }
        else
        {
            CalendarSyncStatusText.Text = "Not synced yet.";
        }
    }

    private async void OnSyncCalendarNowClicked(object? sender, RoutedEventArgs e)
    {
        var url = CalendarIcsUrlTextBox.Text ?? "";

        SyncCalendarNowButton.IsEnabled = false;
        CalendarSyncStatusText.Text = "Syncing…";

        try
        {
            await SettingsService.UpdateAsync(_settingsId, s => s.CalendarIcsUrl = url);
            var (result, error) = await CalendarSyncService.SyncAsync(url);

            await SettingsService.UpdateAsync(_settingsId, s =>
            {
                s.LastCalendarSyncAt = DateTimeOffset.Now;
                s.LastCalendarSyncError = error;
            });

            if (result is { } r)
            {
                CalendarSyncStatusText.Text = $"Synced: {r.Added} added, {r.Updated} updated, {r.Removed} removed.";
            }
            else
            {
                CalendarSyncStatusText.Text = $"Sync failed: {error}";
            }

            await ReloadRemindersAsync();
        }
        finally
        {
            SyncCalendarNowButton.IsEnabled = true;
        }
    }

    private async Task ReloadRemindersAsync()
    {
        _reminders = await ReminderService.GetAllAsync();
        RemindersListBox.Items.Clear();

        foreach (var reminder in _reminders)
        {
            var recurrenceSuffix = reminder.Recurrence == RecurrenceType.None ? "" : $" ({reminder.Recurrence})";
            var timeSuffix = reminder.Time is { } time ? $" at {time:h:mm tt}" : "";
            RemindersListBox.Items.Add($"{reminder.Date:MMM d, yyyy}{timeSuffix} — {reminder.Title}{recurrenceSuffix}");
        }
    }

    private async void OnAddReminderClicked(object? sender, RoutedEventArgs e)
    {
        var editWindow = new ReminderEditWindow { ReminderService = ReminderService };
        editWindow.Saved += async () => await ReloadRemindersAsync();
        await editWindow.ShowDialog(this);
    }

    private async void OnEditReminderClicked(object? sender, RoutedEventArgs e)
    {
        var index = RemindersListBox.SelectedIndex;
        if (index < 0 || index >= _reminders.Count)
        {
            return;
        }

        var editWindow = new ReminderEditWindow { ReminderService = ReminderService, ExistingReminder = _reminders[index] };
        editWindow.Saved += async () => await ReloadRemindersAsync();
        await editWindow.ShowDialog(this);
    }

    private async void OnDeleteReminderClicked(object? sender, RoutedEventArgs e)
    {
        var index = RemindersListBox.SelectedIndex;
        if (index < 0 || index >= _reminders.Count)
        {
            return;
        }

        await ReminderService.DeleteAsync(_reminders[index].Id);
        await ReloadRemindersAsync();
    }

    private async void OnMarkReminderDoneClicked(object? sender, RoutedEventArgs e)
    {
        var index = RemindersListBox.SelectedIndex;
        if (index < 0 || index >= _reminders.Count)
        {
            return;
        }

        await ReminderService.MarkNextOccurrenceCompletedAsync(_reminders[index].Id);
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var startAtLogin = StartAtLoginCheckBox.IsChecked ?? true;

        await SettingsService.UpdateAsync(_settingsId, settings =>
        {
            settings.AvatarPack = AvatarPackComboBox.SelectedItem as string ?? settings.AvatarPack;
            settings.AvatarName = AvatarNameTextBox.Text ?? settings.AvatarName;
            settings.AvatarScale = AvatarScaleSlider.Value;
            settings.UserName = UserNameTextBox.Text ?? settings.UserName;
            settings.CurrentFocus = CurrentFocusTextBox.Text ?? settings.CurrentFocus;
            settings.CalendarIcsUrl = CalendarIcsUrlTextBox.Text ?? "";

            if (WorkStartPicker.SelectedTime is { } workStart)
            {
                settings.WorkStart = TimeOnly.FromTimeSpan(workStart);
            }

            if (WorkEndPicker.SelectedTime is { } workEnd)
            {
                settings.WorkEnd = TimeOnly.FromTimeSpan(workEnd);
            }

            if (ToneComboBox.SelectedItem is string toneName && Enum.TryParse<MessageTone>(toneName, out var tone))
            {
                settings.Tone = tone;
            }

            settings.SleepAfter = TimeSpan.FromMinutes((double)(SleepAfterMinutes.Value ?? 10));
            settings.BreakAfter = TimeSpan.FromMinutes((double)(BreakAfterMinutes.Value ?? 50));
            settings.StartAtLogin = startAtLogin;
            settings.ReducedMotion = ReducedMotionCheckBox.IsChecked ?? false;
            settings.PomodoroFocus = TimeSpan.FromMinutes((double)(PomodoroFocusMinutes.Value ?? 25));
            settings.PomodoroShortBreak = TimeSpan.FromMinutes((double)(PomodoroShortBreakMinutes.Value ?? 5));
            settings.PomodoroLongBreak = TimeSpan.FromMinutes((double)(PomodoroLongBreakMinutes.Value ?? 15));
            settings.PomodoroSessionsBeforeLongBreak = (int)(PomodoroSessionsBeforeLongBreak.Value ?? 4);
            settings.AiMessagesEnabled = AiMessagesEnabledCheckBox.IsChecked ?? false;
            settings.AiApiKey = AiApiKeyTextBox.Text ?? "";
            settings.AiModel = string.IsNullOrWhiteSpace(AiModelTextBox.Text) ? "claude-haiku-4-5" : AiModelTextBox.Text;

            if (QuietHoursEnabledCheckBox.IsChecked == true
                && QuietHoursStartPicker.SelectedTime is { } quietStart
                && QuietHoursEndPicker.SelectedTime is { } quietEnd)
            {
                settings.QuietHoursStart = TimeOnly.FromTimeSpan(quietStart);
                settings.QuietHoursEnd = TimeOnly.FromTimeSpan(quietEnd);
            }
            else
            {
                settings.QuietHoursStart = null;
                settings.QuietHoursEnd = null;
            }
        });

        if (startAtLogin)
        {
            await StartupRegistration.EnableAsync();
        }
        else
        {
            await StartupRegistration.DisableAsync();
        }

        SettingsSaved?.Invoke();
        Close();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close();
}
