using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class LiveRefreshCoordinator
{
    private readonly SessionService _session;
    private readonly StompWebSocketService _stomp;
    private CancellationTokenSource? _pollingCts;
    private List<string> _topics = [];
    private Action<NotificacionPedidoWs>? _subscriptionCallback;
    private DateTimeOffset? _lastRefreshUtc;

    public LiveRefreshCoordinator(SessionService session, StompWebSocketService stomp)
    {
        _session = session;
        _stomp = stomp;
    }

    public async Task StartAsync(
        Func<Task> refreshAsync,
        IEnumerable<string> topics,
        Action<NotificacionPedidoWs>? onNotification = null,
        int pollingIntervalSeconds = 30,
        int minInitialRefreshIntervalSeconds = 10)
    {
        Stop();

        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;
        _topics = [.. topics.Distinct()];

        _subscriptionCallback = notif =>
        {
            onNotification?.Invoke(notif);
            _ = ExecuteRefreshAsync(refreshAsync);
        };

        if (DebeRefrescarAlReaparecer(minInitialRefreshIntervalSeconds))
            await ExecuteRefreshAsync(refreshAsync);

        var webSocketDisponible = await ConectarWebSocketAsync(token);
        if (webSocketDisponible)
            return;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollingIntervalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                    await ExecuteRefreshAsync(refreshAsync);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public void Stop()
    {
        if (_subscriptionCallback is not null)
        {
            foreach (var topic in _topics)
                _stomp.DesuscribirDe(topic, _subscriptionCallback);
        }

        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;

        _subscriptionCallback = null;
        _topics.Clear();
        _ = _stomp.DesconectarSiEstaInactivoAsync();
    }

    private async Task<bool> ConectarWebSocketAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_session.Token) || _subscriptionCallback is null || _topics.Count == 0)
            return false;

        try
        {
            if (!_stomp.Conectado)
                await _stomp.ConectarAsync(_session.Token!, ct);

            foreach (var topic in _topics)
                _stomp.SuscribirA(topic, _subscriptionCallback);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WS][RefreshCoordinator] No se pudo conectar: {ex.Message}");
            return false;
        }
    }

    private bool DebeRefrescarAlReaparecer(int minInitialRefreshIntervalSeconds)
    {
        if (_lastRefreshUtc is null)
            return true;

        return DateTimeOffset.UtcNow - _lastRefreshUtc.Value >= TimeSpan.FromSeconds(minInitialRefreshIntervalSeconds);
    }

    private async Task ExecuteRefreshAsync(Func<Task> refreshAsync)
    {
        await refreshAsync();
        _lastRefreshUtc = DateTimeOffset.UtcNow;
    }
}