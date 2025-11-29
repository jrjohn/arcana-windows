using System.Net.NetworkInformation;
using Arcana.Core.Common;

namespace Arcana.Infrastructure.Platform;

/// <summary>
/// Network monitor implementation.
/// 網路監控實作
/// </summary>
public class NetworkMonitor : INetworkMonitor, IDisposable
{
    private bool _isOnline;
    private bool _disposed;

    public bool IsOnline => _isOnline;

    public event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    public NetworkMonitor()
    {
        _isOnline = CheckNetworkAvailability();
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    public async Task<bool> IsOnlineAsync()
    {
        return await Task.Run(() =>
        {
            _isOnline = CheckNetworkAvailability();
            return _isOnline;
        });
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        var wasOnline = _isOnline;
        _isOnline = e.IsAvailable && CheckNetworkAvailability();

        if (wasOnline != _isOnline)
        {
            var networkType = GetNetworkType();
            StatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(_isOnline, networkType));
        }
    }

    private static bool CheckNetworkAvailability()
    {
        try
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch
        {
            return false;
        }
    }

    private static NetworkType GetNetworkType()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            if (interfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
            {
                return NetworkType.Ethernet;
            }

            if (interfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
            {
                return NetworkType.WiFi;
            }

            return interfaces.Count > 0 ? NetworkType.Unknown : NetworkType.None;
        }
        catch
        {
            return NetworkType.Unknown;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
