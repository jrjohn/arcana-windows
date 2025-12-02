namespace Arcana.Plugins.Contracts.Mvvm;

/// <summary>
/// Marker interface for ViewModel effects (side effects).
/// Effects handle one-time events like navigation, dialogs, and notifications.
/// </summary>
/// <remarks>
/// Effects are separate from state management and include:
/// - Navigation events
/// - Toast/Dialog notifications
/// - Analytics events
/// - Logging
/// </remarks>
public interface IViewModelEffect
{
}
