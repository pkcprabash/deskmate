using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Deskmate.App.ViewModels;
using Deskmate.Core;

namespace Deskmate.App.Views;

public partial class AvatarWindow : Window
{
    private const int ScreenMargin = 16;
    private const double DragThreshold = 4;

    private bool _pointerDown;
    private bool _movedBeyondThreshold;
    private int _pressClickCount;
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

        viewModel.StateChanged += OnStateChanged;
        OnStateChanged(viewModel.CurrentState);
    }

    private void OnStateChanged(AvatarState state)
    {
        var animationName = state switch
        {
            AvatarState.Waving => "wave",
            AvatarState.Held => "held",
            _ => "idle",
        };

        PlayAnimation(animationName);
    }

    private void PlayAnimation(string name)
    {
        var pack = (DataContext as AvatarViewModel)?.Pack;
        if (pack is null || !pack.TryGetAnimation(name, out var animation, out var sheet))
        {
            return;
        }

        Sprite.Play(sheet, animation, pack.FrameSize.Width, pack.FrameSize.Height);
    }

    private void OnAnimationCompleted(object? sender, EventArgs e)
    {
        (DataContext as AvatarViewModel)?.NotifyAnimationCompleted();
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
            Position = ClampToScreen(Position, GetPrimaryScreenBounds());
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

    private void OnQuitClicked(object? sender, RoutedEventArgs e)
    {
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
