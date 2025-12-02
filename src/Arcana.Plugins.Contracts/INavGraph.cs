namespace Arcana.Plugins.Contracts;

/// <summary>
/// Navigation graph interface for type-safe navigation.
/// Plugins can use this interface directly or wrap it in their own NavGraph class.
/// </summary>
public interface INavGraph
{
    /// <summary>
    /// Navigate to a route by ID.
    /// </summary>
    /// <param name="routeId">The route identifier (view ID).</param>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>True if navigation succeeded.</returns>
    Task<bool> To(string routeId, object? parameter = null);

    /// <summary>
    /// Navigate to a route in a new tab.
    /// </summary>
    /// <param name="routeId">The route identifier (view ID).</param>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>True if navigation succeeded.</returns>
    Task<bool> ToNewTab(string routeId, object? parameter = null);

    /// <summary>
    /// Navigate within an existing tab's frame.
    /// </summary>
    /// <param name="parentRouteId">The parent tab's route ID.</param>
    /// <param name="routeId">The route identifier to navigate to.</param>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>True if navigation succeeded.</returns>
    Task<bool> ToWithinTab(string parentRouteId, string routeId, object? parameter = null);

    /// <summary>
    /// Navigate back.
    /// </summary>
    /// <returns>True if navigation succeeded.</returns>
    Task<bool> Back();

    /// <summary>
    /// Navigate forward.
    /// </summary>
    /// <returns>True if navigation succeeded.</returns>
    Task<bool> Forward();

    /// <summary>
    /// Close the current view/tab.
    /// </summary>
    Task Close();

    /// <summary>
    /// Show a dialog and wait for result.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="routeId">The dialog route identifier.</param>
    /// <param name="parameter">Optional dialog parameter.</param>
    /// <returns>The dialog result, or null if cancelled.</returns>
    Task<TResult?> ShowDialog<TResult>(string routeId, object? parameter = null);

    /// <summary>
    /// Gets whether back navigation is available.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether forward navigation is available.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the current route ID.
    /// </summary>
    string? CurrentRouteId { get; }
}
