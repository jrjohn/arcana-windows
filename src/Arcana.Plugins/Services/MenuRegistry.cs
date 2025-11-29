using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// Menu registry implementation.
/// 菜單註冊表實作
/// </summary>
public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentDictionary<string, MenuItemDefinition> _menuItems = new();

    public event EventHandler? MenusChanged;

    public IDisposable RegisterMenuItem(MenuItemDefinition item)
    {
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

    public IReadOnlyList<MenuItemDefinition> GetMenuItems(MenuLocation location)
    {
        return _menuItems.Values
            .Where(m => m.Location == location)
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
