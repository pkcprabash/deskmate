namespace Deskmate.Core;

public enum AvatarInput
{
    KeyPressed,
    Tick,
    Clicked,
    DragStarted,
    DragEnded,
    ReminderDue,
    AlertDismissed,
    BreakAccepted,
    BreakSnoozed,

    /// <summary>Raised by the sprite animator when a one-shot animation finishes playing.</summary>
    AnimationCompleted,
}
