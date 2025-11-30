using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Menu registry implementation.
/// </summary>
public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentDictionary<string, MenuItemDefinition> _menuItems = new();
    private readonly ILogger<MenuRegistry>? _logger;

    public event EventHandler? MenusChanged;

    public MenuRegistry() { }

    public MenuRegistry(ILogger<MenuRegistry> logger)
    {
        _logger = logger;
    }

    public IDisposable RegisterMenuItem(MenuItemDefinition item)
    {
        ValidateMenuItem(item);

        if (!_menuItems.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException($"Menu item already registered: {item.Id}");
        }

        OnMenusChanged();

        return new Subscription(() =>
        {
            _menuItems.TryRemove(item.Id, out _);
            OnMenusChanged();
        });
    }

    public IDisposable RegisterMenuItems(IEnumerable<MenuItemDefinition> items)
    {
        var itemList = items.ToList();

        // Validate all items first
        foreach (var item in itemList)
        {
            ValidateMenuItem(item);
        }

        foreach (var item in itemList)
        {
            _menuItems[item.Id] = item;
        }

        OnMenusChanged();

        return new Subscription(() =>
        {
            foreach (var item in itemList)
            {
                _menuItems.TryRemove(item.Id, out _);
            }
            OnMenusChanged();
        });
    }

    private void ValidateMenuItem(MenuItemDefinition item)
    {
        var result = MenuItemValidator.Validate(item);

        // Log warnings
        foreach (var warning in result.Warnings)
        {
            _logger?.LogWarning("Menu item validation warning: {Warning}", warning);
        }

        // Throw on errors
        if (!result.IsValid)
        {
            var errorMessage = string.Join("; ", result.Errors);
            _logger?.LogError("Menu item validation failed: {Errors}", errorMessage);
            throw new ContributionValidationException($"Menu item validation failed: {errorMessage}")
            {
                ValidationErrors = result.Errors
            };
        }
    }

    public IReadOnlyList<MenuItemDefinition> GetMenuItems(MenuLocation location)
    {
        return _menuItems.Values
            .Where(m => m.Location == location)
            .OrderBy(m => m.Order)
            .ToList();
    }

    public IReadOnlyList<MenuItemDefinition> GetMenuItems(MenuLocation location, string moduleId)
    {
        return _menuItems.Values
            .Where(m => m.Location == location && m.ModuleId == moduleId)
            .OrderBy(m => m.Order)
            .ToList();
    }

    public IReadOnlyList<MenuItemDefinition> GetAllMenuItems()
    {
        return _menuItems.Values.OrderBy(m => m.Order).ToList();
    }

    private void OnMenusChanged()
    {
        MenusChanged?.Invoke(this, EventArgs.Empty);
    }

    private class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dispose();
            }
        }
    }
}
