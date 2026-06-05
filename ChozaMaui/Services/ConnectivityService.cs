using CommunityToolkit.Mvvm.ComponentModel;

namespace ChozaMaui.Services;

public class ConnectivityService : ObservableObject, IDisposable
{
    private static readonly TimeSpan HealthCacheTtl = TimeSpan.FromSeconds(5);
    private readonly ServerConnectionService _serverConnection;
    private readonly PendingOrderService _pendingOrders;
    private readonly SessionService _session;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _disposed;
    private bool _offlineAlertShown;
    private bool _isOnline;
    private string _statusText = "Verificando conexion...";
    private string _statusColor = "#f59e0b";
    private bool _isChecking;
    private BackendHealthCheckResult? _lastHealthCheck;
    private DateTimeOffset _lastHealthCheckAt;

    public bool IsOnline
    {
        get => _isOnline;
        private set => SetProperty(ref _isOnline, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        private set => SetProperty(ref _statusColor, value);
    }

    public bool IsChecking
    {
        get => _isChecking;
        private set => SetProperty(ref _isChecking, value);
    }

    public ConnectivityService(ServerConnectionService serverConnection, PendingOrderService pendingOrders, SessionService session)
    {
        _serverConnection = serverConnection;
        _pendingOrders = pendingOrders;
        _session = session;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public Task InitializeAsync()
        => RefreshStatusAsync(showOfflineAlert: false);

    public async Task RefreshStatusAsync(bool showOfflineAlert = true)
    {
        await _refreshLock.WaitAsync();
        try
        {
            IsChecking = true;

            var health = await CheckBackendAsync(force: true);
            if (health.IsOnline)
            {
                IsOnline = true;
                StatusText = health.Message;
                StatusColor = "#16a34a";
                _offlineAlertShown = false;
                await _pendingOrders.TrySyncPendingOrdersAsync();
                return;
            }

            SetOffline(health.Message, showOfflineAlert);
        }
        finally
        {
            IsChecking = false;
            _refreshLock.Release();
        }
    }

    public async Task<BackendHealthCheckResult> CheckBackendAsync(bool force = false)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return CacheHealthResult(BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.NoInternet,
                "Sin internet. Revisa Wi-Fi o datos moviles.",
                hasInternet: false), sw);

        if (!force
            && _lastHealthCheck is not null
            && DateTimeOffset.UtcNow - _lastHealthCheckAt < HealthCacheTtl)
        {
            System.Diagnostics.Debug.WriteLine($"[PERF][Connectivity] Health check cache: {sw.ElapsedMilliseconds} ms");
            return _lastHealthCheck;
        }

        var result = await _serverConnection.CheckAsync();
        return CacheHealthResult(result, sw);
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                await RefreshStatusAsync(showOfflineAlert: false);
                return;
            }

            SetOffline("Sin internet. Revisa Wi-Fi o datos moviles.", showOfflineAlert: true);
        });
    }

    private void SetOffline(string message, bool showOfflineAlert)
    {
        IsOnline = false;
        StatusText = string.IsNullOrWhiteSpace(message) ? "Sin internet" : message;
        StatusColor = "#dc2626";

        if (!showOfflineAlert || _offlineAlertShown || !_session.EstaAutenticado)
            return;

        _offlineAlertShown = true;
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                var titulo = message.Contains("internet", StringComparison.OrdinalIgnoreCase)
                    ? "Sin internet"
                    : "Servidor no disponible";
                await page.DisplayAlertAsync(titulo, message, "OK");
            }
        });
    }

    private BackendHealthCheckResult CacheHealthResult(BackendHealthCheckResult result, System.Diagnostics.Stopwatch sw)
    {
        _lastHealthCheck = result;
        _lastHealthCheckAt = DateTimeOffset.UtcNow;
        System.Diagnostics.Debug.WriteLine($"[PERF][Connectivity] Validar conectividad real: {sw.ElapsedMilliseconds} ms | {result.Failure} | {result.Message}");
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        _refreshLock.Dispose();
        _disposed = true;
    }
}
