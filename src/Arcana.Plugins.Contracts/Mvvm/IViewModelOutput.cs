namespace Arcana.Plugins.Contracts.Mvvm;

/// <summary>
/// Marker interface for ViewModel output (readonly state).
/// Outputs expose reactive, read-only state to the View.
/// </summary>
/// <remarks>
/// Outputs should only contain:
/// - Read-only properties
/// - Computed values derived from state
/// - Observable collections (read-only)
/// </remarks>
public interface IViewModelOutput
{
}
