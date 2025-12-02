using Arcana.Plugins.Contracts;

namespace Arcana.Plugin.FlowChart.Navigation;

/// <summary>
/// Type-safe navigation graph for FlowChart plugin.
/// Wraps INavGraph to provide strongly-typed navigation methods.
/// </summary>
public sealed class FlowChartNavGraph
{
    private readonly INavGraph _nav;

    public FlowChartNavGraph(INavGraph nav)
    {
        _nav = nav;
    }

    // ============================================================
    // Routes - Plugin's own view routes
    // ============================================================

    public static class Routes
    {
        public const string Editor = "FlowChartEditorPage";
        // Future routes:
        // public const string Templates = "FlowChartTemplatesPage";
        // public const string Settings = "FlowChartSettingsPage";
    }

    // ============================================================
    // Navigation Actions - Type-safe methods
    // ============================================================

    /// <summary>
    /// Navigate to new flowchart editor.
    /// </summary>
    public Task<bool> ToNewEditor()
        => _nav.ToNewTab(Routes.Editor);

    /// <summary>
    /// Navigate to flowchart editor with specific diagram.
    /// </summary>
    /// <param name="filePath">Path to the diagram file to open.</param>
    public Task<bool> ToEditor(string filePath)
        => _nav.ToNewTab(Routes.Editor, new EditorArgs(EditorAction.Open, filePath));

    /// <summary>
    /// Navigate to flowchart editor with a sample diagram.
    /// </summary>
    public Task<bool> ToSampleEditor()
        => _nav.ToNewTab(Routes.Editor, new EditorArgs(EditorAction.Sample, null));

    /// <summary>
    /// Navigate to flowchart editor requesting file open dialog.
    /// </summary>
    public Task<bool> ToEditorWithOpenDialog()
        => _nav.ToNewTab(Routes.Editor, new EditorArgs(EditorAction.OpenDialog, null));

    // ============================================================
    // Common Navigation (delegated to INavGraph)
    // ============================================================

    /// <summary>
    /// Navigate back.
    /// </summary>
    public Task<bool> Back() => _nav.Back();

    /// <summary>
    /// Close current view.
    /// </summary>
    public Task Close() => _nav.Close();

    /// <summary>
    /// Navigate to any route (for cross-plugin navigation).
    /// </summary>
    public Task<bool> To(string routeId, object? parameter = null)
        => _nav.To(routeId, parameter);

    // ============================================================
    // Navigation Arguments
    // ============================================================

    /// <summary>
    /// Editor action type.
    /// </summary>
    public enum EditorAction
    {
        New,
        Open,
        OpenDialog,
        Sample
    }

    /// <summary>
    /// Arguments for navigating to the editor.
    /// </summary>
    public record EditorArgs(EditorAction Action, string? FilePath);
}
