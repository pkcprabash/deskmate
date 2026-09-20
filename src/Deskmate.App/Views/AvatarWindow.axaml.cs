using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Deskmate.App.ViewModels;

namespace Deskmate.App.Views;

public partial class AvatarWindow : Window
{
    private const int ScreenMargin = 16;

    private bool _dragging;
    private PixelPoint _dragStartPointerScreenPosition;
    private PixelPoint _dragStartWindowPosition;

    public AvatarWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not AvatarViewModel viewModel)
        {
            return;
        }

        await viewModel.LoadAsync();
        Position = ResolveStartupPosition(viewModel);
    }

    private PixelPoint ResolveStartupPosition(AvatarViewModel viewModel)
    {
        var screenBounds = GetPrimaryScreenBounds();

        if (viewModel.SavedPositionX is double x && viewModel.SavedPositionY is double y)
        {
            return ClampToScreen(new PixelPoint((int)x, (int)y), screenBounds);
        }

        var defaultX = screenBounds.Right - (int)Width - ScreenMargin;
        var defaultY = screenBounds.Bottom - (int)Height - ScreenMargin;
        return ClampToScreen(new PixelPoint(defaultX, defaultY), screenBounds);
    }

    private PixelRect GetPrimaryScreenBounds() =>
        Screens.Primary?.WorkingArea ?? new PixelRect(0, 0, 1280, 800);

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

        _dragging = true;
        _dragStartPointerScreenPosition = this.PointToScreen(e.GetPosition(this));
        _dragStartWindowPosition = Position;
        e.Pointer.Capture((IInputElement)sender!);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var currentPointerScreenPosition = this.PointToScreen(e.GetPosition(this));
        var deltaX = currentPointerScreenPosition.X - _dragStartPointerScreenPosition.X;
        var deltaY = currentPointerScreenPosition.Y - _dragStartPointerScreenPosition.Y;

        Position = new PixelPoint(_dragStartWindowPosition.X + deltaX, _dragStartWindowPosition.Y + deltaY);
    }

    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        e.Pointer.Capture(null);

        Position = ClampToScreen(Position, GetPrimaryScreenBounds());

        if (DataContext is AvatarViewModel viewModel)
        {
            await viewModel.SavePositionAsync(Position.X, Position.Y);
        }
    }
}
