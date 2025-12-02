namespace Arcana.Plugins.Contracts.Mvvm;

/// <summary>
/// Marker interface for ViewModel input actions.
/// Inputs are the only entry point for state changes (UDF pattern).
/// </summary>
/// <remarks>
/// UDF Flow: View → Input Action → State Mutation → Output → View Re-render
/// </remarks>
public interface IViewModelInput
{
}
