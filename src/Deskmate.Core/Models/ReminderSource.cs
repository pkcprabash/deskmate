namespace Deskmate.Core.Models;

/// <summary>Where a reminder came from: typed in by the user, or imported from a subscribed calendar feed.</summary>
public enum ReminderSource
{
    Manual,
    IcsSubscription,
}
