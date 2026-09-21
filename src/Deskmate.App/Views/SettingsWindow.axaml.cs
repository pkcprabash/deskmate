using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Startup;

namespace Deskmate.App.Views;

public partial class SettingsWindow : Window
{
    private int _settingsId;

    public required SettingsService SettingsService { get; init; }
    public required IStartupRegistration StartupRegistration { get; init; }

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

        foreach (var packName in ListAvailableAvatarPacks())
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
    }

    private static string[] ListAvailableAvatarPacks()
    {
        var packsDirectory = AvatarPackLoader.GetPacksDirectory();
        if (!Directory.Exists(packsDirectory))
        {
            return [];
        }

        return Directory.GetDirectories(packsDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToArray();
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
