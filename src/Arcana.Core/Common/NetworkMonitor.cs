namespace Arcana.Core.Common;

/// <summary>
/// Monitors network connectivity status.
/// </summary>
public interface NetworkMonitor
{
    /// <summary>
    /// Gets whether the device is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Checks if the device is online asynchronously.
    /// </summary>
    Task<bool> IsOnlineAsync();

    /// <summary>
    /// Event raised when connectivity status changes.
    /// </summary>
    event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// Event args for network status changes.
/// </summary>
public class NetworkStatusChangedEventArgs : EventArgs
{
    public bool IsOnline { get; }
    public NetworkType NetworkType { get; }

    public NetworkStatusChangedEventArgs(bool isOnline, NetworkType networkType = NetworkType.Unknown)
    {
        IsOnline = isOnline;
        NetworkType = networkType;
    }
}

/// <summary>
/// Types of network connections.
/// </summary>
public enum NetworkType
{
    Unknown,
    None,
    Ethernet,
    WiFi,
    Cellular
}
