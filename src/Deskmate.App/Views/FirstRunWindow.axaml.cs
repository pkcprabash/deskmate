using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Startup;

namespace Deskmate.App.Views;

public partial class FirstRunWindow : Window
{
    public required SettingsService SettingsService { get; init; }
    public required IStartupRegistration StartupRegistration { get; init; }
    public required int SettingsId { get; init; }
    public required string DefaultAvatarPack { get; init; }

    /// <summary>Raised once setup is saved, so the app can hand off to the normal AvatarWindow.</summary>
    public event Action? Completed;

    public FirstRunWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        foreach (var packName in ListAvailableAvatarPacks())
        {
            AvatarPackComboBox.Items.Add(packName);
        }

        AvatarPackComboBox.SelectedItem = DefaultAvatarPack;
        AvatarNameTextBox.Text = "Pixel";
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

    private async void OnGetStartedClicked(object? sender, RoutedEventArgs e)
    {
        var startAtLogin = StartAtLoginCheckBox.IsChecked ?? true;

        await SettingsService.UpdateAsync(SettingsId, settings =>
        {
            settings.UserName = string.IsNullOrWhiteSpace(UserNameTextBox.Text) ? settings.UserName : UserNameTextBox.Text;
            settings.AvatarPack = AvatarPackComboBox.SelectedItem as string ?? settings.AvatarPack;
            settings.AvatarName = string.IsNullOrWhiteSpace(AvatarNameTextBox.Text) ? settings.AvatarName : AvatarNameTextBox.Text;
            settings.StartAtLogin = startAtLogin;
            settings.FirstRunCompleted = true;
        });

        if (startAtLogin)
        {
            await StartupRegistration.EnableAsync();
        }

        Completed?.Invoke();
    }
}
