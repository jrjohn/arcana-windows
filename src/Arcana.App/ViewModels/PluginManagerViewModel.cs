using System.Collections.ObjectModel;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Arcana.App.ViewModels;

/// <summary>
/// ViewModel for Plugin Manager page using UDF pattern.
/// </summary>
public partial class PluginManagerViewModel : ReactiveViewModelBase
{
    // ============ Dependencies ============
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginManagerViewModel> _logger;
    private System.Timers.Timer? _healthCheckTimer;

    // ============ Private State ============

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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    // ============ Computed Properties ============
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    // ============ Commands ============
    [RelayCommand]
    private void InstallPlugin() => Fx.RequestFileForInstall.Emit();

    [RelayCommand]
    private Task LoadPlugins() => LoadPluginsAsync();

    [RelayCommand]
    private Task CheckAllHealth() => CheckAllHealthAsync();

    [RelayCommand]
    private Task ActivatePlugin(PluginItemViewModel? plugin) => ActivatePluginAsync(plugin);

    [RelayCommand]
    private Task DeactivatePlugin(PluginItemViewModel? plugin) => DeactivatePluginAsync(plugin);

    [RelayCommand]
    private Task ReloadPlugin(PluginItemViewModel? plugin) => ReloadPluginAsync(plugin);

    [RelayCommand]
    private Task CheckHealth(PluginItemViewModel? plugin) => CheckHealthAsync(plugin);

    [RelayCommand]
    private void UpgradePlugin(PluginItemViewModel? plugin)
    {
        if (plugin != null)
            Fx.RequestFileForUpgrade.Emit(plugin);
    }

    [RelayCommand]
    private Task UninstallPlugin(PluginItemViewModel? plugin) => UninstallPluginAsync(plugin);

    // ============ Input/Output/Effect ============
    private Input? _input;
    private Output? _output;
    private Effect? _effect;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // ============ Constructor ============
    public PluginManagerViewModel(
        IPluginManager pluginManager,
        ILogger<PluginManagerViewModel> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;

        _pluginManager.PluginStateChanged += OnPluginStateChanged;
        _pluginManager.InstallProgressChanged += OnInstallProgressChanged;
    }

    // ============ Lifecycle ============
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

    // ============ Internal Actions ============

    private async Task LoadPluginsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            ErrorMessage = null;

            var plugins = _pluginManager.GetAllPlugins();
            var healthStatuses = await _pluginManager.CheckAllHealthAsync();

            Plugins.Clear();
            foreach (var plugin in plugins)
            {
                var health = healthStatuses.FirstOrDefault(h => h.PluginId == plugin.Id);
                Plugins.Add(new PluginItemViewModel(plugin, health));
            }

            StatusMessage = $"Loaded {Plugins.Count} plugins";
            Fx.PluginsLoaded.Emit();
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    private async Task ActivatePluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null || IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _pluginManager.ActivatePluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Activated plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
                Fx.ShowSuccess.Emit($"Activated: {plugin.Name}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to activate plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeactivatePluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null || IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _pluginManager.DeactivatePluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Deactivated plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
                Fx.ShowSuccess.Emit($"Deactivated: {plugin.Name}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to deactivate plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallPluginAsync(string filePath)
    {
        try
        {
            IsInstalling = true;
            InstallProgress = 0;
            InstallPhase = "Starting installation...";

            var result = await _pluginManager.InstallPluginAsync(filePath);
            if (result.Success)
            {
                StatusMessage = $"Installed plugin: {result.PluginId}";
                await LoadPluginsAsync();
                Fx.ShowSuccess.Emit($"Installed: {result.PluginId}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to install plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin");
            ErrorMessage = $"Installation failed: {ex.Message}";
            Fx.ShowError.Emit(ErrorMessage);
        }
        finally
        {
            IsInstalling = false;
            InstallPhase = string.Empty;
        }
    }

    private async Task UninstallPluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null || IsBusy) return;

        if (!plugin.CanUninstall)
        {
            Fx.ShowWarning.Emit("Cannot uninstall built-in plugins");
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _pluginManager.UninstallPluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Uninstalled plugin: {plugin.Name}";
                Plugins.Remove(plugin);
                Fx.ShowSuccess.Emit($"Uninstalled: {plugin.Name}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to uninstall plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpgradePluginAsync(PluginItemViewModel? plugin, string filePath)
    {
        if (plugin == null) return;

        if (!plugin.CanUpgrade)
        {
            Fx.ShowWarning.Emit("Cannot upgrade built-in plugins");
            return;
        }

        try
        {
            IsInstalling = true;
            InstallProgress = 0;
            InstallPhase = "Starting upgrade...";

            var result = await _pluginManager.UpgradePluginAsync(plugin.Id, filePath);
            if (result.Success)
            {
                StatusMessage = result.Message ?? $"Upgraded plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
                Fx.ShowSuccess.Emit($"Upgraded: {plugin.Name}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to upgrade plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade plugin {PluginId}", plugin.Id);
            ErrorMessage = $"Upgrade failed: {ex.Message}";
            Fx.ShowError.Emit(ErrorMessage);
        }
        finally
        {
            IsInstalling = false;
            InstallPhase = string.Empty;
        }
    }

    private async Task RollbackPluginAsync(PluginVersionInfo? version)
    {
        if (version == null || SelectedPlugin == null || IsBusy) return;

        if (version.IsCurrent)
        {
            Fx.ShowWarning.Emit("Already at this version");
            return;
        }

        try
        {
            IsBusy = true;
            IsInstalling = true;
            InstallPhase = $"Rolling back to v{version.Version}...";

            var result = await _pluginManager.RollbackPluginAsync(SelectedPlugin.Id, version.Version.ToString());
            if (result.Success)
            {
                StatusMessage = result.Message ?? $"Rolled back to version {version.Version}";
                await RefreshPluginAsync(SelectedPlugin.Id);
                LoadVersionHistory();
                Fx.ShowSuccess.Emit($"Rolled back to v{version.Version}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to rollback plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
            IsInstalling = false;
            InstallPhase = string.Empty;
        }
    }

    private async Task ReloadPluginAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null || IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _pluginManager.ReloadPluginAsync(plugin.Id);
            if (result.Success)
            {
                StatusMessage = $"Reloaded plugin: {plugin.Name}";
                await RefreshPluginAsync(plugin.Id);
                Fx.ShowSuccess.Emit($"Reloaded: {plugin.Name}");
            }
            else
            {
                ErrorMessage = result.Message ?? "Failed to reload plugin";
                Fx.ShowError.Emit(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckHealthAsync(PluginItemViewModel? plugin)
    {
        if (plugin == null || IsBusy) return;

        try
        {
            IsBusy = true;
            var allHealth = await _pluginManager.CheckAllHealthAsync();
            var health = allHealth.FirstOrDefault(h => h.PluginId == plugin.Id);
            if (health != null)
            {
                plugin.UpdateHealth(health);
                StatusMessage = $"Health check: {health.State}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckAllHealthAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var healthStatuses = await _pluginManager.CheckAllHealthAsync();
            foreach (var health in healthStatuses)
            {
                var plugin = Plugins.FirstOrDefault(p => p.Id == health.PluginId);
                plugin?.UpdateHealth(health);
            }
            StatusMessage = "Health check completed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============ State Change Handlers ============

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
    }

    // ============ Private Helpers ============

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
        _healthCheckTimer = new System.Timers.Timer(60000);
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

    // ============ Disposal ============
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopHealthMonitoring();
            _effect?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ============================================================
    // NESTED CLASSES: Input, Output, Effect
    // ============================================================

    #region Input

    public sealed class Input : IViewModelInput
    {
        private readonly PluginManagerViewModel _vm;

        internal Input(PluginManagerViewModel vm) => _vm = vm;

        public Task LoadPlugins() => _vm.LoadPluginsAsync();
        public Task ActivatePlugin(PluginItemViewModel? plugin) => _vm.ActivatePluginAsync(plugin);
        public Task DeactivatePlugin(PluginItemViewModel? plugin) => _vm.DeactivatePluginAsync(plugin);
        public Task InstallPlugin(string filePath) => _vm.InstallPluginAsync(filePath);
        public Task UninstallPlugin(PluginItemViewModel? plugin) => _vm.UninstallPluginAsync(plugin);
        public Task UpgradePlugin(PluginItemViewModel? plugin, string filePath) => _vm.UpgradePluginAsync(plugin, filePath);
        public Task RollbackPlugin(PluginVersionInfo? version) => _vm.RollbackPluginAsync(version);
        public Task ReloadPlugin(PluginItemViewModel? plugin) => _vm.ReloadPluginAsync(plugin);
        public Task CheckHealth(PluginItemViewModel? plugin) => _vm.CheckHealthAsync(plugin);
        public Task CheckAllHealth() => _vm.CheckAllHealthAsync();

        public void SelectPlugin(PluginItemViewModel? plugin) => _vm.SelectedPlugin = plugin;
        public void UpdateSearchText(string text) => _vm.SearchText = text;

        // File picker requests (View handles the picker, then calls Install/Upgrade)
        public void RequestInstall() => _vm.Fx.RequestFileForInstall.Emit();
        public void RequestUpgrade(PluginItemViewModel? plugin)
        {
            if (plugin != null)
                _vm.Fx.RequestFileForUpgrade.Emit(plugin);
        }
    }

    #endregion

    #region Output

    public sealed class Output : IViewModelOutput
    {
        private readonly PluginManagerViewModel _vm;

        internal Output(PluginManagerViewModel vm) => _vm = vm;

        // Plugin state
        public ObservableCollection<PluginItemViewModel> Plugins => _vm.Plugins;
        public PluginItemViewModel? SelectedPlugin => _vm.SelectedPlugin;
        public ObservableCollection<PluginVersionInfo> AvailableVersions => _vm.AvailableVersions;
        public string SearchText => _vm.SearchText;

        // UI state
        public string StatusMessage => _vm.StatusMessage;
        public double InstallProgress => _vm.InstallProgress;
        public bool IsInstalling => _vm.IsInstalling;
        public string InstallPhase => _vm.InstallPhase;
        public bool IsLoading => _vm.IsLoading;
        public bool IsBusy => _vm.IsBusy;
        public string? ErrorMessage => _vm.ErrorMessage;

        // Computed state
        public bool HasPlugins => _vm.Plugins.Count > 0;
        public bool HasSelection => _vm.SelectedPlugin != null;
        public bool CanActivate => _vm.SelectedPlugin != null && _vm.SelectedPlugin.State != PluginState.Active;
        public bool CanDeactivate => _vm.SelectedPlugin != null && _vm.SelectedPlugin.State == PluginState.Active;
    }

    #endregion

    #region Effect

    public sealed class Effect : IViewModelEffect, IDisposable
    {
        private bool _disposed;

        // Notifications
        public EffectSubject<string> ShowError { get; } = new();
        public EffectSubject<string> ShowWarning { get; } = new();
        public EffectSubject<string> ShowSuccess { get; } = new();
        public EffectSubject<string> ShowInfo { get; } = new();

        // File picker requests
        public EffectSubject RequestFileForInstall { get; } = new();
        public EffectSubject<PluginItemViewModel> RequestFileForUpgrade { get; } = new();

        // Events
        public EffectSubject PluginsLoaded { get; } = new();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            ShowError.Dispose();
            ShowWarning.Dispose();
            ShowSuccess.Dispose();
            ShowInfo.Dispose();
            RequestFileForInstall.Dispose();
            RequestFileForUpgrade.Dispose();
            PluginsLoaded.Dispose();
        }
    }

    #endregion
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
