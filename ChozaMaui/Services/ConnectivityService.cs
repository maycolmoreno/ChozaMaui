using CommunityToolkit.Mvvm.ComponentModel;

namespace ChozaMaui.Services;

public class ConnectivityService : ObservableObject, IDisposable
{
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

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                SetOffline("Sin conexion a internet", showOfflineAlert);
                return;
            }

            var (ok, estado) = await _serverConnection.PingAsync();
            if (ok)
            {
                IsOnline = true;
                StatusText = estado;
                StatusColor = "#16a34a";
                _offlineAlertShown = false;
                await _pendingOrders.TrySyncPendingOrdersAsync();
                return;
            }

            SetOffline(estado, showOfflineAlert);
        }
        finally
        {
            IsChecking = false;
            _refreshLock.Release();
        }
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

            SetOffline("Sin conexion a internet", showOfflineAlert: true);
        });
    }

    private void SetOffline(string message, bool showOfflineAlert)
    {
        IsOnline = false;
        StatusText = string.IsNullOrWhiteSpace(message) ? "Sin conexion" : message;
        StatusColor = "#dc2626";

        if (!showOfflineAlert || _offlineAlertShown || !_session.EstaAutenticado)
            return;

        _offlineAlertShown = true;
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlertAsync("Sin conexion", "No hay conexion disponible. Los cambios pendientes se guardaran localmente.", "OK");
        });
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