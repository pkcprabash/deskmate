namespace Deskmate.Core;

public enum AvatarInput
{
    KeyPressed,
    Clicked,
    DragStarted,
    DragEnded,
    BreakAccepted,
    BreakSnoozed,
    ReminderDue,
    AlertDismissed,

    /// <summary>A Pomodoro break began: the avatar takes a sip of coffee if it is free to.</summary>
    PomodoroBreakStarted,

    /// <summary>Raised by the sprite animator when a one-shot animation finishes playing.</summary>
    AnimationCompleted,
}
