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

    /// <summary>Raised by the sprite animator when a one-shot animation finishes playing.</summary>
    AnimationCompleted,
}
