using System.Collections.ObjectModel;
using Arcana.Plugins.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;

namespace Arcana.App.ViewModels;

/// <summary>
/// ViewModel for Plugin Manager page.
/// </summary>
public partial class PluginManagerViewModel : ViewModelBase
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginManagerViewModel> _logger;
    private System.Timers.Timer? _healthCheckTimer;

    [ObservableProperty]
    private ObservableCollection<PluginItemViewModel> _plugins = new();

    [ObservableProperty]
    private PluginItemViewModel? _selectedPlugin;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _installProgress;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private string _installPhase = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PluginVersionInfo> _availableVersions = new();

    public PluginManagerViewModel(
        IPluginManager pluginManager,
        ILogger<PluginManagerViewModel> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;

        _pluginManager.PluginStateChanged += OnPluginStateChanged;
        _pluginManager.InstallProgressChanged += OnInstallProgressChanged;
    }

    public override async Task InitializeAsync()
    {
        await LoadPluginsAsync();
        StartHealthMonitoring();
    }

    public override async Task CleanupAsync()
    {
        StopHealthMonitoring();
        _pluginManager.PluginStateChanged -= OnPluginStateChanged;
        _pluginManager.InstallProgressChanged -= OnInstallProgressChanged;
        await base.CleanupAsync();
    }

    [RelayCommand]
    private async Task LoadPluginsAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var plugins = _pluginManager.GetAllPlugins();
            var healthStatuses = await _pluginManager.CheckAllHealthAsync();

            Plugins.Clear();
            foreach (var plugin in plugins)
            {
                var health = healthStatuses.FirstOrDefault(h => h.PluginId == plugin.Id);
                Plugins.Add(new PluginItemViewModel(plugin, health));
            }

            StatusMessage = $"Loaded {Plugins.Count} plugins";
        });
    }

    [RelayCommand]
    private async Task ActivatePluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _pluginManager.ActivatePluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Activated plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
            }
            else
            {
                SetError(result.Message ?? "Failed to activate plugin");
            }
        });
    }

    [RelayCommand]
    private async Task DeactivatePluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _pluginManager.DeactivatePluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Deactivated plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
            }
            else
            {
                SetError(result.Message ?? "Failed to deactivate plugin");
            }
        });
    }

    [RelayCommand]
    private async Task InstallPluginAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".zip");
            picker.FileTypeFilter.Add(".arcana");

            // Get the window handle for WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            IsInstalling = true;
            InstallProgress = 0;
            InstallPhase = "Starting installation...";

            var result = await _pluginManager.InstallPluginAsync(file.Path);
            if (result.Success)
            {
                StatusMessage = $"Installed plugin: {result.PluginId}";
                await LoadPluginsAsync();
            }
            else
            {
                SetError(result.Message ?? "Failed to install plugin");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin");
            SetError($"Installation failed: {ex.Message}");
        }
        finally
        {
            IsInstalling = false;
            InstallPhase = string.Empty;
        }
    }

    [RelayCommand]
    private async Task UninstallPluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        if (!plugin.CanUninstall)
        {
            SetError("Cannot uninstall built-in plugins");
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _pluginManager.UninstallPluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Uninstalled plugin: {plugin.Name}";
                Plugins.Remove(plugin);
            }
            else
            {
                SetError(result.Message ?? "Failed to uninstall plugin");
            }
        });
    }

    [RelayCommand]
    private async Task UpgradePluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        if (!plugin.CanUpgrade)
        {
            SetError("Cannot upgrade built-in plugins");
            return;
        }

        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".zip");
            picker.FileTypeFilter.Add(".arcana");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            IsInstalling = true;
            InstallProgress = 0;
            InstallPhase = "Starting upgrade...";

            var result = await _pluginManager.UpgradePluginAsync(plugin.Id, file.Path);
            if (result.Success)
            {
                StatusMessage = result.Message ?? $"Upgraded plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
            }
            else
            {
                SetError(result.Message ?? "Failed to upgrade plugin");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade plugin {PluginId}", plugin.Id);
            SetError($"Upgrade failed: {ex.Message}");
        }
        finally
        {
            IsInstalling = false;
            InstallPhase = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RollbackPluginAsync(PluginVersionInfo? version)
    {
        if (version == null || SelectedPlugin == null) return;

        if (version.IsCurrent)
        {
            SetError("Already at this version");
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            IsInstalling = true;
            InstallPhase = $"Rolling back to v{version.Version}...";

            var result = await _pluginManager.RollbackPluginAsync(SelectedPlugin.Id, version.Version.ToString());
            if (result.Success)
            {
                StatusMessage = result.Message ?? $"Rolled back to version {version.Version}";
                await RefreshPluginAsync(SelectedPlugin.Id);
                LoadVersionHistory();
            }
            else
            {
                SetError(result.Message ?? "Failed to rollback plugin");
            }

            IsInstalling = false;
            InstallPhase = string.Empty;
        });
    }

    [RelayCommand]
    private async Task ReloadPluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _pluginManager.ReloadPluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Reloaded plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
            }
            else
            {
                SetError(result.Message ?? "Failed to reload plugin");
            }
        });
    }

    [RelayCommand]
    private async Task CheckHealthAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var allHealth = await _pluginManager.CheckAllHealthAsync();
            var health = allHealth.FirstOrDefault(h => h.PluginId == plugin.Id);
            if (health != null)
            {
                plugin.UpdateHealth(health);
                StatusMessage = $"Health check: {health.State}";
            }
        });
    }

    [RelayCommand]
    private async Task CheckAllHealthAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var healthStatuses = await _pluginManager.CheckAllHealthAsync();
            foreach (var health in healthStatuses)
            {
                var plugin = Plugins.FirstOrDefault(p => p.Id == health.PluginId);
                plugin?.UpdateHealth(health);
            }
            StatusMessage = "Health check completed";
        });
    }

    partial void OnSelectedPluginChanged(PluginItemViewModel? value)
    {
        if (value != null)
        {
            LoadVersionHistory();
        }
        else
        {
            AvailableVersions.Clear();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Filter plugins based on search text
        // In a real implementation, you'd filter the observable collection
    }

    private void LoadVersionHistory()
    {
        if (SelectedPlugin == null) return;

        AvailableVersions.Clear();
        var versions = _pluginManager.GetAvailableVersions(SelectedPlugin.Id);
        foreach (var version in versions)
        {
            AvailableVersions.Add(version);
        }
    }

    private async Task RefreshPluginAsync(string pluginId)
    {
        var pluginInfo = _pluginManager.GetPlugin(pluginId);
        var health = _pluginManager.GetHealthStatus(pluginId);

        var existing = Plugins.FirstOrDefault(p => p.Id == pluginId);
        if (existing != null && pluginInfo != null)
        {
            var index = Plugins.IndexOf(existing);
            Plugins[index] = new PluginItemViewModel(pluginInfo, health);

            if (SelectedPlugin?.Id == pluginId)
            {
                SelectedPlugin = Plugins[index];
            }
        }
    }

    private void StartHealthMonitoring()
    {
        _healthCheckTimer = new System.Timers.Timer(60000); // Check every minute
        _healthCheckTimer.Elapsed += async (s, e) =>
        {
            try
            {
                var healthStatuses = await _pluginManager.CheckAllHealthAsync();
                foreach (var health in healthStatuses)
                {
                    var plugin = Plugins.FirstOrDefault(p => p.Id == health.PluginId);
                    plugin?.UpdateHealth(health);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed");
            }
        };
        _healthCheckTimer.Start();
    }

    private void StopHealthMonitoring()
    {
        _healthCheckTimer?.Stop();
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        var plugin = Plugins.FirstOrDefault(p => p.Id == e.PluginId);
        if (plugin != null)
        {
            plugin.State = e.NewState;
        }
    }

    private void OnInstallProgressChanged(object? sender, PluginInstallProgressEventArgs e)
    {
        InstallProgress = e.Progress;
        InstallPhase = e.Message ?? e.Phase.ToString();
    }
}

/// <summary>
/// ViewModel for a single plugin item.
/// </summary>
public partial class PluginItemViewModel : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public string? Description { get; }
    public string? Author { get; }
    public PluginType Type { get; }
    public string? IconPath { get; }
    public DateTime InstalledAt { get; }
    public bool IsBuiltIn { get; }
    public bool CanUninstall { get; }
    public bool CanUpgrade { get; }

    [ObservableProperty]
    private PluginState _state;

    [ObservableProperty]
    private HealthState _healthState;

    [ObservableProperty]
    private string? _healthMessage;

    [ObservableProperty]
    private TimeSpan? _responseTime;

    [ObservableProperty]
    private long _memoryUsageBytes;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private DateTime? _lastErrorAt;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private ObservableCollection<HealthCheckResult> _healthDetails = new();

    public string VersionString => Version.ToString();
    public string MemoryUsageFormatted => $"{MemoryUsageBytes / 1024.0 / 1024.0:N2} MB";
    public string TypeString => Type.ToString();
    public string StateString => State.ToString();
    public string HealthStateString => HealthState.ToString();

    public PluginItemViewModel(IPluginInfo plugin, PluginHealthStatus? health = null)
    {
        Id = plugin.Id;
        Name = plugin.Name;
        Version = plugin.Version;
        Description = plugin.Description;
        Author = plugin.Author;
        Type = plugin.Type;
        IconPath = plugin.IconPath;
        InstalledAt = plugin.InstalledAt;
        IsBuiltIn = plugin.IsBuiltIn;
        CanUninstall = plugin.CanUninstall;
        CanUpgrade = plugin.CanUpgrade;
        State = plugin.State;

        if (health != null)
        {
            UpdateHealth(health);
        }
    }

    public void UpdateHealth(PluginHealthStatus health)
    {
        HealthState = health.State;
        HealthMessage = health.Message;
        ResponseTime = health.ResponseTime;
        MemoryUsageBytes = health.MemoryUsageBytes;
        ErrorCount = health.ErrorCount;
        LastErrorAt = health.LastErrorAt;
        LastError = health.LastError;

        HealthDetails.Clear();
        if (health.Details != null)
        {
            foreach (var detail in health.Details)
            {
                HealthDetails.Add(detail);
            }
        }
    }
}
