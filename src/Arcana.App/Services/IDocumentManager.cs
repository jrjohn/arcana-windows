using Arcana.App.Controls;

namespace Arcana.App.Services;

/// <summary>
/// Manages documents across modules and floating windows.
/// Tracks open documents, floating windows, and supports dock/undock operations.
/// </summary>
public interface IDocumentManager
{
    /// <summary>
    /// Registers a document as open within a module
    /// </summary>
    void RegisterDocument(string moduleId, DocumentInfo document);

    /// <summary>
    /// Unregisters a document when closed
    /// </summary>
    void UnregisterDocument(string moduleId, string documentId);

    /// <summary>
    /// Gets all open documents for a module
    /// </summary>
    IEnumerable<DocumentInfo> GetDocuments(string moduleId);

    /// <summary>
    /// Gets all open documents across all modules
    /// </summary>
    IEnumerable<(string ModuleId, DocumentInfo Document)> GetAllDocuments();

    /// <summary>
    /// Checks if a document is open in any module or floating window
    /// </summary>
    bool IsDocumentOpen(string documentId);

    /// <summary>
    /// Gets the module ID where a document is open
    /// </summary>
    string? GetDocumentModule(string documentId);

    /// <summary>
    /// Registers a floating window
    /// </summary>
    void RegisterFloatingWindow(FloatingWindowInfo window);

    /// <summary>
    /// Unregisters a floating window when closed
    /// </summary>
    void UnregisterFloatingWindow(string windowId);

    /// <summary>
    /// Gets all floating windows
    /// </summary>
    IEnumerable<FloatingWindowInfo> GetFloatingWindows();

    /// <summary>
    /// Event raised when a document is opened
    /// </summary>
    event EventHandler<DocumentManagerEventArgs>? DocumentOpened;

    /// <summary>
    /// Event raised when a document is closed
    /// </summary>
    event EventHandler<DocumentManagerEventArgs>? DocumentClosed;

    /// <summary>
    /// Event raised when a floating window is created
    /// </summary>
    event EventHandler<FloatingWindowEventArgs>? FloatingWindowCreated;

    /// <summary>
    /// Event raised when a floating window is closed
    /// </summary>
    event EventHandler<FloatingWindowEventArgs>? FloatingWindowClosed;
}

/// <summary>
/// Information about a floating window
/// </summary>
public class FloatingWindowInfo
{
    public string Id { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public DocumentInfo Document { get; set; } = new();
    public object? WindowReference { get; set; }
}

/// <summary>
/// Event args for document manager events
/// </summary>
public class DocumentManagerEventArgs : EventArgs
{
    public string ModuleId { get; }
    public DocumentInfo Document { get; }

    public DocumentManagerEventArgs(string moduleId, DocumentInfo document)
    {
        ModuleId = moduleId;
        Document = document;
    }
}

/// <summary>
/// Event args for floating window events
/// </summary>
public class FloatingWindowEventArgs : EventArgs
{
    public FloatingWindowInfo Window { get; }

    public FloatingWindowEventArgs(FloatingWindowInfo window)
    {
        Window = window;
    }
}
