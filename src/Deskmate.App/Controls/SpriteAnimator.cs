using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Deskmate.Core.Models;

namespace Deskmate.App.Controls;

/// <summary>
/// Plays a named animation from an avatar pack's sprite sheet: a single
/// horizontal strip of equal-sized frames, looping or one-shot.
/// </summary>
public class SpriteAnimator : Control
{
    public event EventHandler? AnimationCompleted;

    private Bitmap? _sheet;
    private int _frameWidth;
    private int _frameHeight;
    private int _frameCount;
    private bool _loop;
    private int _currentFrame;
    private DispatcherTimer? _timer;

    /// <summary>
    /// frameWidth/frameHeight describe the source sprite sheet's frame size (for cropping);
    /// renderWidth/renderHeight are the on-screen size, defaulting to the frame size when
    /// omitted. Kept separate so AvatarScale can resize the visual without touching how
    /// frames are cropped from the sheet.
    /// </summary>
    public void Play(Bitmap sheet, AvatarAnimation animation, int frameWidth, int frameHeight, double? renderWidth = null, double? renderHeight = null)
    {
        Stop();

        _sheet = sheet;
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _frameCount = Math.Max(1, animation.Frames);
        _loop = animation.Loop;
        _currentFrame = 0;

        Width = renderWidth ?? frameWidth;
        Height = renderHeight ?? frameHeight;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0 / Math.Max(1, animation.Fps)),
        };
        _timer.Tick += OnTick;
        _timer.Start();

        InvalidateVisual();
    }

    public void Stop()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Tick -= OnTick;
        _timer.Stop();
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _currentFrame++;

        if (_currentFrame >= _frameCount)
        {
            if (_loop)
            {
                _currentFrame = 0;
            }
            else
            {
                _currentFrame = _frameCount - 1;
                Stop();
                InvalidateVisual();
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_sheet is null)
        {
            return;
        }

        var sourceRect = new Rect(_currentFrame * _frameWidth, 0, _frameWidth, _frameHeight);
        var destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.DrawImage(_sheet, sourceRect, destRect);
    }
}
