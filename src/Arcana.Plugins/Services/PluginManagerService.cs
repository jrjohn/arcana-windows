using System.IO.Compression;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Full-featured plugin manager service implementing IPluginManager.
/// </summary>
public class PluginManagerService : IPluginManager, IAsyncDisposable
{
    private readonly PluginManager _pluginManager;
    private readonly PluginHealthMonitor _healthMonitor;
    private readonly ILogger<PluginManagerService> _logger;
    private readonly string _pluginsPath;
    private readonly string _backupsPath;
    private readonly Dictionary<string, List<PluginVersionInfo>> _versionHistory = new();

    public event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;
    public event EventHandler<PluginInstallProgressEventArgs>? InstallProgressChanged;

    public PluginManagerService(
        PluginManager pluginManager,
        PluginHealthMonitor healthMonitor,
        ILogger<PluginManagerService> logger,
        string pluginsPath)
    {
        _pluginManager = pluginManager;
        _healthMonitor = healthMonitor;
        _logger = logger;
        _pluginsPath = pluginsPath;
        _backupsPath = Path.Combine(pluginsPath, ".backups");

        Directory.CreateDirectory(_backupsPath);

        // Subscribe to health changes
        _healthMonitor.HealthChanged += OnHealthChanged;

        // Load version history
        LoadVersionHistory();
    }

    public IReadOnlyList<IPluginInfo> GetAllPlugins()
    {
        var result = new List<IPluginInfo>();

        // Add loaded plugins
        foreach (var lp in _pluginManager.Plugins.Values)
        {
            result.Add(PluginInfo.FromPlugin(lp.Plugin, lp.PluginPath));
        }

        // Add pending (lazy-loaded) plugins
        foreach (var pending in _pluginManager.PendingPlugins.Values)
        {
            result.Add(PluginInfo.FromManifest(pending.Manifest, pending.PluginPath));
        }

        return result;
    }

    public IPluginInfo? GetPlugin(string pluginId)
    {
        // Check loaded plugins first
        var loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
        if (loaded != null)
        {
            return PluginInfo.FromPlugin(loaded.Plugin, loaded.PluginPath);
        }

        // Check pending plugins
        var pending = _pluginManager.PendingPlugins.GetValueOrDefault(pluginId);
        if (pending != null)
        {
            return PluginInfo.FromManifest(pending.Manifest, pending.PluginPath);
        }

        return null;
    }

    public async Task<PluginOperationResult> ActivatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldState = GetPluginState(pluginId);
            await _pluginManager.ActivatePluginAsync(pluginId, cancellationToken);

            RaiseStateChanged(pluginId, oldState, PluginState.Active);
            _healthMonitor.RecordSuccess(pluginId);

            return PluginOperationResult.Succeeded(pluginId, "Plugin activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate plugin {PluginId}", pluginId);
            _healthMonitor.RecordError(pluginId, ex);
            return PluginOperationResult.Failed(pluginId, ex.Message, "ACTIVATION_FAILED", ex);
        }
    }

    public async Task<PluginOperationResult> DeactivatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldState = GetPluginState(pluginId);
            await _pluginManager.DeactivatePluginAsync(pluginId, cancellationToken);

            RaiseStateChanged(pluginId, oldState, PluginState.Loaded);
            _healthMonitor.RecordSuccess(pluginId);

            return PluginOperationResult.Succeeded(pluginId, "Plugin deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate plugin {PluginId}", pluginId);
            _healthMonitor.RecordError(pluginId, ex);
            return PluginOperationResult.Failed(pluginId, ex.Message, "DEACTIVATION_FAILED", ex);
        }
    }

    public async Task<PluginOperationResult> InstallPluginAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            return PluginOperationResult.Failed(null, $"Package file not found: {packagePath}", "FILE_NOT_FOUND");
        }

        using var stream = File.OpenRead(packagePath);
        return await InstallPluginAsync(stream, Path.GetFileName(packagePath), cancellationToken);
    }

    public async Task<PluginOperationResult> InstallPluginAsync(Stream packageStream, string fileName, CancellationToken cancellationToken = default)
    {
        string? pluginId = null;
        string? tempPath = null;

        try
        {
            // Create temp extraction directory
            tempPath = Path.Combine(Path.GetTempPath(), $"arcana_plugin_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            RaiseProgress(null, InstallPhase.Extracting, 0.1, "Extracting package...");

            // Extract package
            using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempPath);
            }

            RaiseProgress(null, InstallPhase.Validating, 0.3, "Validating plugin...");

            // Read manifest
            var manifestPath = Path.Combine(tempPath, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                return PluginOperationResult.Failed(null, "Invalid plugin package: missing plugin.json", "INVALID_PACKAGE");
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest == null || string.IsNullOrEmpty(manifest.Id))
            {
                return PluginOperationResult.Failed(null, "Invalid plugin manifest", "INVALID_MANIFEST");
            }

            pluginId = manifest.Id;
            var version = Version.TryParse(manifest.Version, out var v) ? v : new Version(1, 0, 0);

            // Check if already installed
            var existingPlugin = GetPlugin(pluginId);
            if (existingPlugin != null)
            {
                return PluginOperationResult.Failed(pluginId,
                    $"Plugin {pluginId} is already installed. Use upgrade instead.", "ALREADY_INSTALLED");
            }

            RaiseProgress(pluginId, InstallPhase.Installing, 0.6, "Installing plugin...");

            // Move to plugins directory (use copy+delete for cross-volume support)
            var targetPath = Path.Combine(_pluginsPath, pluginId);
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            MoveDirectoryCrossVolume(tempPath, targetPath);
            tempPath = null; // Prevent cleanup

            RaiseProgress(pluginId, InstallPhase.Activating, 0.8, "Loading plugin...");

            // Reload plugins
            await _pluginManager.DiscoverPluginsAsync(cancellationToken);

            // Record version
            RecordVersion(pluginId, version, targetPath);

            RaiseProgress(pluginId, InstallPhase.Completed, 1.0, "Installation completed");

            _logger.LogInformation("Installed plugin {PluginId} v{Version}", pluginId, version);
            return PluginOperationResult.Succeeded(pluginId, $"Plugin {pluginId} installed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin from {FileName}", fileName);
            RaiseProgress(pluginId, InstallPhase.Failed, 0, $"Installation failed: {ex.Message}");
            return PluginOperationResult.Failed(pluginId, ex.Message, "INSTALL_FAILED", ex);
        }
        finally
        {
            // Cleanup temp directory
            if (tempPath != null && Directory.Exists(tempPath))
            {
                try { Directory.Delete(tempPath, true); } catch { }
            }
        }
    }

    public async Task<PluginOperationResult> UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            var plugin = GetPlugin(pluginId);
            if (plugin == null)
            {
                return PluginOperationResult.Failed(pluginId, "Plugin not found", "NOT_FOUND");
            }

            if (!plugin.CanUninstall)
            {
                return PluginOperationResult.Failed(pluginId, "Cannot uninstall built-in plugin", "BUILTIN_PLUGIN");
            }

            // Deactivate first
            if (plugin.State == PluginState.Active)
            {
                await DeactivatePluginAsync(pluginId, cancellationToken);
            }

            // Remove from plugins
            var pluginPath = plugin.InstallPath;

            // Clean up backups
            var backupPath = Path.Combine(_backupsPath, pluginId);
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);
            }

            // Remove version history
            _versionHistory.Remove(pluginId);
            await SaveVersionHistoryAsync();

            // Delete plugin directory
            if (Directory.Exists(pluginPath))
            {
                Directory.Delete(pluginPath, true);
            }

            RaiseStateChanged(pluginId, PluginState.Loaded, PluginState.Uninstalled);

            _logger.LogInformation("Uninstalled plugin {PluginId}", pluginId);
            return PluginOperationResult.Succeeded(pluginId, "Plugin uninstalled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin {PluginId}", pluginId);
            return PluginOperationResult.Failed(pluginId, ex.Message, "UNINSTALL_FAILED", ex);
        }
    }

    public async Task<PluginOperationResult> UpgradePluginAsync(string pluginId, string packagePath, CancellationToken cancellationToken = default)
    {
        string? backupPath = null;

        try
        {
            var existingPlugin = GetPlugin(pluginId);
            if (existingPlugin == null)
            {
                return PluginOperationResult.Failed(pluginId, "Plugin not found", "NOT_FOUND");
            }

            if (!existingPlugin.CanUpgrade)
            {
                return PluginOperationResult.Failed(pluginId, "Cannot upgrade built-in plugin", "BUILTIN_PLUGIN");
            }

            var currentVersion = existingPlugin.Version;
            var wasActive = existingPlugin.State == PluginState.Active;

            RaiseProgress(pluginId, InstallPhase.BackingUp, 0.1, "Creating backup...");

            // Create backup
            backupPath = await CreateBackupAsync(pluginId, currentVersion, cancellationToken);

            // Handle hot reload interface
            IDictionary<string, object>? savedState = null;
            var loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginHotReload hotReload)
            {
                await hotReload.OnBeforeReloadAsync(cancellationToken);
                savedState = await hotReload.GetStateAsync(cancellationToken);
            }

            // Handle upgradeable interface
            if (loaded?.Plugin is IPluginUpgradeable upgradeable)
            {
                // Read new version from package
                var newVersion = await GetVersionFromPackageAsync(packagePath, cancellationToken);
                if (!upgradeable.CanUpgradeTo(newVersion))
                {
                    return PluginOperationResult.Failed(pluginId,
                        $"Plugin cannot be upgraded to version {newVersion}", "UPGRADE_NOT_SUPPORTED");
                }

                var canUpgrade = await upgradeable.OnBeforeUpgradeAsync(currentVersion, newVersion, cancellationToken);
                if (!canUpgrade)
                {
                    return PluginOperationResult.Failed(pluginId, "Plugin rejected the upgrade", "UPGRADE_REJECTED");
                }
            }

            // Deactivate current version
            if (wasActive)
            {
                await DeactivatePluginAsync(pluginId, cancellationToken);
            }

            RaiseProgress(pluginId, InstallPhase.Extracting, 0.3, "Extracting new version...");

            // Extract and install new version
            var pluginPath = existingPlugin.InstallPath;
            var tempPath = Path.Combine(Path.GetTempPath(), $"arcana_upgrade_{Guid.NewGuid()}");

            try
            {
                Directory.CreateDirectory(tempPath);
                ZipFile.ExtractToDirectory(packagePath, tempPath);

                // Validate manifest
                var manifestPath = Path.Combine(tempPath, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    throw new InvalidOperationException("Invalid package: missing plugin.json");
                }

                RaiseProgress(pluginId, InstallPhase.Installing, 0.5, "Installing new version...");

                // Replace plugin files
                Directory.Delete(pluginPath, true);
                Directory.Move(tempPath, pluginPath);
            }
            catch
            {
                // Restore backup on failure
                if (backupPath != null && Directory.Exists(backupPath))
                {
                    if (Directory.Exists(pluginPath))
                        Directory.Delete(pluginPath, true);
                    CopyDirectory(backupPath, pluginPath);
                }
                throw;
            }

            RaiseProgress(pluginId, InstallPhase.Activating, 0.7, "Reloading plugin...");

            // Reload plugin
            await _pluginManager.DiscoverPluginsAsync(cancellationToken);

            // Get new version and record it
            var newPlugin = GetPlugin(pluginId);
            if (newPlugin != null)
            {
                RecordVersion(pluginId, newPlugin.Version, pluginPath);
            }

            // Handle post-upgrade
            loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginUpgradeable upgradeableNew && newPlugin != null)
            {
                await upgradeableNew.OnAfterUpgradeAsync(currentVersion, newPlugin.Version, cancellationToken);
            }

            // Restore state if hot reload
            if (loaded?.Plugin is IPluginHotReload hotReloadNew && savedState != null)
            {
                await hotReloadNew.RestoreStateAsync(savedState, cancellationToken);
                await hotReloadNew.OnAfterReloadAsync(cancellationToken);
            }

            // Reactivate if was active
            if (wasActive)
            {
                await ActivatePluginAsync(pluginId, cancellationToken);
            }

            RaiseProgress(pluginId, InstallPhase.Completed, 1.0, "Upgrade completed");

            _logger.LogInformation("Upgraded plugin {PluginId} from v{OldVersion} to v{NewVersion}",
                pluginId, currentVersion, newPlugin?.Version);

            return PluginOperationResult.Succeeded(pluginId,
                $"Plugin upgraded from {currentVersion} to {newPlugin?.Version}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade plugin {PluginId}", pluginId);
            RaiseProgress(pluginId, InstallPhase.Failed, 0, $"Upgrade failed: {ex.Message}");
            return PluginOperationResult.Failed(pluginId, ex.Message, "UPGRADE_FAILED", ex);
        }
    }

    public async Task<PluginOperationResult> RollbackPluginAsync(string pluginId, string targetVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var plugin = GetPlugin(pluginId);
            if (plugin == null)
            {
                return PluginOperationResult.Failed(pluginId, "Plugin not found", "NOT_FOUND");
            }

            var versions = GetAvailableVersions(pluginId);
            var targetVersionInfo = versions.FirstOrDefault(v => v.Version.ToString() == targetVersion);

            if (targetVersionInfo == null)
            {
                return PluginOperationResult.Failed(pluginId,
                    $"Version {targetVersion} not found in backup history", "VERSION_NOT_FOUND");
            }

            if (!Directory.Exists(targetVersionInfo.BackupPath))
            {
                return PluginOperationResult.Failed(pluginId, "Backup files not found", "BACKUP_NOT_FOUND");
            }

            var currentVersion = plugin.Version;
            var wasActive = plugin.State == PluginState.Active;

            // Handle upgradeable interface
            var loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginUpgradeable upgradeable)
            {
                if (!upgradeable.CanRollbackTo(targetVersionInfo.Version))
                {
                    return PluginOperationResult.Failed(pluginId,
                        $"Plugin cannot be rolled back to version {targetVersion}", "ROLLBACK_NOT_SUPPORTED");
                }

                var canRollback = await upgradeable.OnBeforeRollbackAsync(currentVersion, targetVersionInfo.Version, cancellationToken);
                if (!canRollback)
                {
                    return PluginOperationResult.Failed(pluginId, "Plugin rejected the rollback", "ROLLBACK_REJECTED");
                }
            }

            RaiseProgress(pluginId, InstallPhase.RollingBack, 0.2, $"Rolling back to version {targetVersion}...");

            // Deactivate current version
            if (wasActive)
            {
                await DeactivatePluginAsync(pluginId, cancellationToken);
            }

            RaiseProgress(pluginId, InstallPhase.Installing, 0.5, "Restoring backup...");

            // Restore backup
            var pluginPath = plugin.InstallPath;
            if (Directory.Exists(pluginPath))
            {
                Directory.Delete(pluginPath, true);
            }
            CopyDirectory(targetVersionInfo.BackupPath, pluginPath);

            RaiseProgress(pluginId, InstallPhase.Activating, 0.7, "Reloading plugin...");

            // Reload
            await _pluginManager.DiscoverPluginsAsync(cancellationToken);

            // Handle post-rollback
            loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginUpgradeable upgradeableNew)
            {
                await upgradeableNew.OnAfterRollbackAsync(currentVersion, targetVersionInfo.Version, cancellationToken);
            }

            // Reactivate if was active
            if (wasActive)
            {
                await ActivatePluginAsync(pluginId, cancellationToken);
            }

            RaiseProgress(pluginId, InstallPhase.Completed, 1.0, "Rollback completed");

            _logger.LogInformation("Rolled back plugin {PluginId} from v{OldVersion} to v{NewVersion}",
                pluginId, currentVersion, targetVersion);

            return PluginOperationResult.Succeeded(pluginId,
                $"Plugin rolled back from {currentVersion} to {targetVersion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback plugin {PluginId} to {Version}", pluginId, targetVersion);
            RaiseProgress(pluginId, InstallPhase.Failed, 0, $"Rollback failed: {ex.Message}");
            return PluginOperationResult.Failed(pluginId, ex.Message, "ROLLBACK_FAILED", ex);
        }
    }

    public IReadOnlyList<PluginVersionInfo> GetAvailableVersions(string pluginId)
    {
        if (_versionHistory.TryGetValue(pluginId, out var versions))
        {
            var currentPlugin = GetPlugin(pluginId);
            return versions
                .Select(v => v with { IsCurrent = currentPlugin?.Version == v.Version })
                .OrderByDescending(v => v.Version)
                .ToList();
        }
        return Array.Empty<PluginVersionInfo>();
    }

    public async Task<PluginOperationResult> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            var plugin = GetPlugin(pluginId);
            if (plugin == null)
            {
                return PluginOperationResult.Failed(pluginId, "Plugin not found", "NOT_FOUND");
            }

            var wasActive = plugin.State == PluginState.Active;
            IDictionary<string, object>? savedState = null;

            var loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginHotReload hotReload)
            {
                await hotReload.OnBeforeReloadAsync(cancellationToken);
                savedState = await hotReload.GetStateAsync(cancellationToken);
            }

            // Deactivate
            if (wasActive)
            {
                await DeactivatePluginAsync(pluginId, cancellationToken);
            }

            // Reload plugins
            await _pluginManager.DiscoverPluginsAsync(cancellationToken);

            // Restore state
            loaded = _pluginManager.Plugins.GetValueOrDefault(pluginId);
            if (loaded?.Plugin is IPluginHotReload hotReloadNew && savedState != null)
            {
                await hotReloadNew.RestoreStateAsync(savedState, cancellationToken);
                await hotReloadNew.OnAfterReloadAsync(cancellationToken);
            }

            // Reactivate
            if (wasActive)
            {
                await ActivatePluginAsync(pluginId, cancellationToken);
            }

            _logger.LogInformation("Reloaded plugin {PluginId}", pluginId);
            return PluginOperationResult.Succeeded(pluginId, "Plugin reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload plugin {PluginId}", pluginId);
            return PluginOperationResult.Failed(pluginId, ex.Message, "RELOAD_FAILED", ex);
        }
    }

    public PluginHealthStatus GetHealthStatus(string pluginId)
    {
        var cached = _healthMonitor.GetCachedHealth(pluginId);
        if (cached != null)
        {
            return cached;
        }

        var plugin = GetPlugin(pluginId);
        if (plugin == null)
        {
            return new PluginHealthStatus
            {
                PluginId = pluginId,
                PluginName = "Unknown",
                State = HealthState.Unknown,
                Message = "Plugin not found"
            };
        }

        return new PluginHealthStatus
        {
            PluginId = pluginId,
            PluginName = plugin.Name,
            State = plugin.State == PluginState.Active ? HealthState.Healthy : HealthState.Unknown,
            Message = $"Plugin state: {plugin.State}"
        };
    }

    public async Task<IReadOnlyList<PluginHealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<PluginHealthStatus>();

        foreach (var (id, loaded) in _pluginManager.Plugins)
        {
            var pluginInfo = PluginInfo.FromPlugin(loaded.Plugin, loaded.PluginPath);
            var health = await _healthMonitor.CheckHealthAsync(pluginInfo, cancellationToken);
            results.Add(health);
        }

        return results;
    }

    private PluginState GetPluginState(string pluginId)
    {
        return _pluginManager.GetPlugin(pluginId)?.State ?? PluginState.Unknown;
    }

    private void RaiseStateChanged(string pluginId, PluginState oldState, PluginState newState)
    {
        PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs
        {
            PluginId = pluginId,
            OldState = oldState,
            NewState = newState
        });
    }

    private void RaiseProgress(string? pluginId, InstallPhase phase, double progress, string message)
    {
        InstallProgressChanged?.Invoke(this, new PluginInstallProgressEventArgs
        {
            PluginId = pluginId ?? "unknown",
            Phase = phase,
            Progress = progress,
            Message = message
        });
    }

    private void OnHealthChanged(object? sender, PluginHealthChangedEventArgs e)
    {
        _logger.LogInformation("Plugin {PluginId} health changed from {OldState} to {NewState}",
            e.PluginId, e.OldState, e.NewState);
    }

    private async Task<string> CreateBackupAsync(string pluginId, Version version, CancellationToken cancellationToken)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin == null)
            throw new InvalidOperationException($"Plugin not found: {pluginId}");

        var backupDir = Path.Combine(_backupsPath, pluginId, version.ToString());
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }
        Directory.CreateDirectory(backupDir);

        CopyDirectory(plugin.InstallPath, backupDir);

        await Task.CompletedTask;
        return backupDir;
    }

    private async Task<Version> GetVersionFromPackageAsync(string packagePath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var manifestEntry = archive.GetEntry("plugin.json");
        if (manifestEntry == null)
            return new Version(1, 0, 0);

        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return Version.TryParse(manifest?.Version, out var v) ? v : new Version(1, 0, 0);
    }

    private void RecordVersion(string pluginId, Version version, string installPath)
    {
        if (!_versionHistory.ContainsKey(pluginId))
        {
            _versionHistory[pluginId] = new List<PluginVersionInfo>();
        }

        var backupPath = Path.Combine(_backupsPath, pluginId, version.ToString());
        var info = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = version,
            InstalledAt = DateTime.UtcNow,
            BackupPath = backupPath,
            IsCurrent = true,
            SizeBytes = GetDirectorySize(installPath)
        };

        // Update existing or add new
        var existing = _versionHistory[pluginId].FindIndex(v => v.Version == version);
        if (existing >= 0)
        {
            _versionHistory[pluginId][existing] = info;
        }
        else
        {
            _versionHistory[pluginId].Add(info);
        }

        _ = SaveVersionHistoryAsync();
    }

    private void LoadVersionHistory()
    {
        var historyPath = Path.Combine(_backupsPath, "version_history.json");
        if (File.Exists(historyPath))
        {
            try
            {
                var json = File.ReadAllText(historyPath);
                var history = JsonSerializer.Deserialize<Dictionary<string, List<VersionHistoryEntry>>>(json);
                if (history != null)
                {
                    foreach (var (pluginId, entries) in history)
                    {
                        _versionHistory[pluginId] = entries
                            .Select(e => new PluginVersionInfo
                            {
                                PluginId = pluginId,
                                Version = Version.Parse(e.Version),
                                InstalledAt = e.InstalledAt,
                                BackupPath = e.BackupPath,
                                SizeBytes = e.SizeBytes,
                                ReleaseNotes = e.ReleaseNotes
                            })
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load version history");
            }
        }
    }

    private async Task SaveVersionHistoryAsync()
    {
        try
        {
            var historyPath = Path.Combine(_backupsPath, "version_history.json");
            var history = _versionHistory.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(v => new VersionHistoryEntry
                {
                    Version = v.Version.ToString(),
                    InstalledAt = v.InstalledAt,
                    BackupPath = v.BackupPath,
                    SizeBytes = v.SizeBytes,
                    ReleaseNotes = v.ReleaseNotes
                }).ToList()
            );

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(historyPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save version history");
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    public async ValueTask DisposeAsync()
    {
        _healthMonitor.HealthChanged -= OnHealthChanged;
        await _pluginManager.DisposeAsync();
        _healthMonitor.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Moves a directory, supporting cross-volume moves by copying and deleting.
    /// </summary>
    private static void MoveDirectoryCrossVolume(string sourcePath, string destinationPath)
    {
        var sourceRoot = Path.GetPathRoot(sourcePath);
        var destRoot = Path.GetPathRoot(destinationPath);

        if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase))
        {
            // Same volume, use standard move
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            // Different volumes, copy then delete
            CopyDirectoryRecursive(sourcePath, destinationPath);
            Directory.Delete(sourcePath, true);
        }
    }

    /// <summary>
    /// Recursively copies a directory and its contents.
    /// </summary>
    private static void CopyDirectoryRecursive(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        // Copy files
        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // Copy subdirectories
        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            var destDir = Path.Combine(destinationPath, Path.GetFileName(dir));
            CopyDirectoryRecursive(dir, destDir);
        }
    }

    private class VersionHistoryEntry
    {
        public string Version { get; set; } = "";
        public DateTime InstalledAt { get; set; }
        public string BackupPath { get; set; } = "";
        public long SizeBytes { get; set; }
        public string? ReleaseNotes { get; set; }
    }

    private class PluginManifest
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Main { get; set; }
        public string[]? Dependencies { get; set; }
    }
}
