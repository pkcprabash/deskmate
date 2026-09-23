using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Data;

namespace Deskmate.App.Views;

public partial class ReminderEditWindow : Window
{
    public required ReminderService ReminderService { get; init; }

    /// <summary>Null when adding a new reminder; set when editing an existing one.</summary>
    public Reminder? ExistingReminder { get; init; }

    /// <summary>Raised after a successful save, so the caller can refresh its list.</summary>
    public event Action? Saved;

    public ReminderEditWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        foreach (var name in Enum.GetNames<RecurrenceType>())
        {
            RecurrenceComboBox.Items.Add(name);
        }

        var reminder = ExistingReminder;
        if (reminder is null)
        {
            Title = "New reminder";
            ReminderDatePicker.SelectedDate = DateTimeOffset.Now;
            RecurrenceComboBox.SelectedItem = nameof(RecurrenceType.None);
            AlertDayBeforeCheckBox.IsChecked = true;
            AlertOnDayCheckBox.IsChecked = true;
            return;
        }

        Title = "Edit reminder";
        TitleTextBox.Text = reminder.Title;
        NotesTextBox.Text = reminder.Notes;
        ReminderDatePicker.SelectedDate = reminder.Date.ToDateTime(TimeOnly.MinValue);
        HasTimeCheckBox.IsChecked = reminder.Time is not null;

        if (reminder.Time is { } time)
        {
            ReminderTimePicker.SelectedTime = time.ToTimeSpan();
        }

        RecurrenceComboBox.SelectedItem = reminder.Recurrence.ToString();
        AlertDayBeforeCheckBox.IsChecked = reminder.AlertDayBefore;
        AlertOnDayCheckBox.IsChecked = reminder.AlertOnDay;
        AlertBeforeCheckBox.IsChecked = reminder.AlertBefore is not null;

        if (reminder.AlertBefore is { } alertBefore)
        {
            AlertBeforeMinutes.Value = (decimal)alertBefore.TotalMinutes;
        }
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || ReminderDatePicker.SelectedDate is not { } selectedDate)
        {
            return;
        }

        var date = DateOnly.FromDateTime(selectedDate.LocalDateTime);
        var hasTime = HasTimeCheckBox.IsChecked ?? false;
        var time = hasTime && ReminderTimePicker.SelectedTime is { } selectedTime ? TimeOnly.FromTimeSpan(selectedTime) : (TimeOnly?)null;
        var recurrence = RecurrenceComboBox.SelectedItem is string recurrenceName && Enum.TryParse<RecurrenceType>(recurrenceName, out var parsedRecurrence)
            ? parsedRecurrence
            : RecurrenceType.None;
        var alertBefore = (AlertBeforeCheckBox.IsChecked ?? false) && AlertBeforeMinutes.Value is { } minutes
            ? TimeSpan.FromMinutes((double)minutes)
            : (TimeSpan?)null;
        var alertDayBefore = AlertDayBeforeCheckBox.IsChecked ?? true;
        var alertOnDay = AlertOnDayCheckBox.IsChecked ?? true;
        var title = TitleTextBox.Text!;
        var notes = NotesTextBox.Text;

        if (ExistingReminder is { } existing)
        {
            await ReminderService.UpdateAsync(existing.Id, reminder =>
            {
                reminder.Title = title;
                reminder.Notes = notes;
                reminder.Date = date;
                reminder.Time = time;
                reminder.Recurrence = recurrence;
                reminder.AlertDayBefore = alertDayBefore;
                reminder.AlertOnDay = alertOnDay;
                reminder.AlertBefore = alertBefore;
            });
        }
        else
        {
            await ReminderService.AddAsync(new Reminder
            {
                Title = title,
                Notes = notes,
                Date = date,
                Time = time,
                Recurrence = recurrence,
                AlertDayBefore = alertDayBefore,
                AlertOnDay = alertOnDay,
                AlertBefore = alertBefore,
            });
        }

        Saved?.Invoke();
        Close();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close();
}
