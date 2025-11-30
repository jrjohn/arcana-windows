using Arcana.App.Controls;
using System.Collections.Concurrent;

namespace Arcana.App.Services;

/// <summary>
/// Implementation of IDocumentManager that tracks documents across modules and floating windows.
/// </summary>
public class DocumentManager : IDocumentManager
{
    private readonly ConcurrentDictionary<string, Dictionary<string, DocumentInfo>> _moduleDocuments = new();
    private readonly ConcurrentDictionary<string, FloatingWindowInfo> _floatingWindows = new();

    public event EventHandler<DocumentManagerEventArgs>? DocumentOpened;
    public event EventHandler<DocumentManagerEventArgs>? DocumentClosed;
    public event EventHandler<FloatingWindowEventArgs>? FloatingWindowCreated;
    public event EventHandler<FloatingWindowEventArgs>? FloatingWindowClosed;

    public void RegisterDocument(string moduleId, DocumentInfo document)
    {
        var documents = _moduleDocuments.GetOrAdd(moduleId, _ => new Dictionary<string, DocumentInfo>());
        documents[document.Id] = document;
        DocumentOpened?.Invoke(this, new DocumentManagerEventArgs(moduleId, document));
    }

    public void UnregisterDocument(string moduleId, string documentId)
    {
        if (_moduleDocuments.TryGetValue(moduleId, out var documents))
        {
            if (documents.TryGetValue(documentId, out var document))
            {
                documents.Remove(documentId);
                DocumentClosed?.Invoke(this, new DocumentManagerEventArgs(moduleId, document));
            }
        }
    }

    public IEnumerable<DocumentInfo> GetDocuments(string moduleId)
    {
        if (_moduleDocuments.TryGetValue(moduleId, out var documents))
        {
            return documents.Values.ToList();
        }
        return Enumerable.Empty<DocumentInfo>();
    }

    public IEnumerable<(string ModuleId, DocumentInfo Document)> GetAllDocuments()
    {
        foreach (var module in _moduleDocuments)
        {
            foreach (var doc in module.Value.Values)
            {
                yield return (module.Key, doc);
            }
        }
    }

    public bool IsDocumentOpen(string documentId)
    {
        // Check in module documents
        foreach (var module in _moduleDocuments.Values)
        {
            if (module.ContainsKey(documentId))
            {
                return true;
            }
        }

        // Check in floating windows
        return _floatingWindows.Values.Any(w => w.Document.Id == documentId);
    }

    public string? GetDocumentModule(string documentId)
    {
        foreach (var module in _moduleDocuments)
        {
            if (module.Value.ContainsKey(documentId))
            {
                return module.Key;
            }
        }
        return null;
    }

    public void RegisterFloatingWindow(FloatingWindowInfo window)
    {
        _floatingWindows[window.Id] = window;
        FloatingWindowCreated?.Invoke(this, new FloatingWindowEventArgs(window));
    }

    public void UnregisterFloatingWindow(string windowId)
    {
        if (_floatingWindows.TryRemove(windowId, out var window))
        {
            FloatingWindowClosed?.Invoke(this, new FloatingWindowEventArgs(window));
        }
    }

    public IEnumerable<FloatingWindowInfo> GetFloatingWindows()
    {
        return _floatingWindows.Values.ToList();
    }
}
