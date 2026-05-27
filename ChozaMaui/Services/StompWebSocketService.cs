using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Cliente STOMP ligero que se conecta al broker WebSocket del backend.
/// La URL WS se resuelve en cada conexión desde SettingsService.
/// Protocolo: STOMP 1.2 sobre WebSocket nativo (sin SockJS).
///
/// Uso:
///   await _ws.ConectarAsync(token);
///   _ws.SuscribirA("/topic/camarero", msg => ...);
///   await _ws.DesconectarAsync();
/// </summary>
public sealed class StompWebSocketService : IAsyncDisposable
{
    // ─── Configuración ────────────────────────────────────────────────────────
    private const int BufSize     = 8192;
    private const int HeartbeatMs = 20_000; // cada 20 s

    private readonly SettingsService _settings;

    // URL calculada en cada conexión para reflejar cambios de IP/puerto
    private Uri WsUri => new(_settings.WsUrl);

    public StompWebSocketService(SettingsService settings)
    {
        _settings = settings;
    }

    // ─── Estado interno ───────────────────────────────────────────────────────
    private ClientWebSocket?            _ws;
    private CancellationTokenSource?    _cts;
    private readonly object             _lock = new();
    private readonly SemaphoreSlim      _connectionGate = new(1, 1);

    // topic → callbacks registrados localmente
    private readonly Dictionary<string, HashSet<Action<NotificacionPedidoWs>>> _subscriptions = new();
    private readonly HashSet<string> _activeTopicSubscriptions = new();

    public bool Conectado => _ws?.State == WebSocketState.Open;

    // ─── Conexión ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Abre la conexión WebSocket, envía el frame STOMP CONNECT y lanza
    /// el bucle receptor en background.
    /// </summary>
    public async Task ConectarAsync(string jwtToken, CancellationToken ct = default)
    {
        await _connectionGate.WaitAsync(ct);
        try
        {
            if (Conectado)
                return;

            _ws  = new ClientWebSocket();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            await _ws.ConnectAsync(WsUri, _cts.Token);

            // Frame STOMP CONNECT
            var connectFrame = BuildFrame("CONNECT", new Dictionary<string, string>
            {
                ["accept-version"] = "1.2",
                ["heart-beat"]     = $"{HeartbeatMs},{HeartbeatMs}",
                ["Authorization"]  = $"Bearer {jwtToken}"
            });
            await EnviarFrameAsync(connectFrame);

            // Esperar CONNECTED
            var resp = await RecibirFrameAsync(_cts.Token);
            if (!resp.StartsWith("CONNECTED", StringComparison.Ordinal))
                throw new InvalidOperationException($"STOMP no respondió CONNECTED: {resp[..Math.Min(200, resp.Length)]}");

            lock (_lock)
                _activeTopicSubscriptions.Clear();

            await ReenviarSuscripcionesAsync();

            // Iniciar loop de lectura
            _ = Task.Run(() => BucleLecturaAsync(_cts.Token), _cts.Token);
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    /// <summary>
    /// Suscribe un callback al topic indicado.
    /// Envía el frame STOMP SUBSCRIBE si la conexión está activa.
    /// </summary>
    public void SuscribirA(string topic, Action<NotificacionPedidoWs> callback)
    {
        var shouldSubscribe = false;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(topic, out var lista))
            {
                lista = [];
                _subscriptions[topic] = lista;
            }

            if (lista.Add(callback) && !_activeTopicSubscriptions.Contains(topic))
                shouldSubscribe = true;
        }

        if (Conectado && shouldSubscribe)
            _ = SuscribirTopicEnServidorAsync(topic);
    }

    public void DesuscribirDe(string topic, Action<NotificacionPedidoWs> callback)
    {
        var shouldUnsubscribe = false;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(topic, out var lista))
                return;

            lista.Remove(callback);
            if (lista.Count > 0)
                return;

            _subscriptions.Remove(topic);
            shouldUnsubscribe = _activeTopicSubscriptions.Remove(topic);
        }

        if (Conectado && shouldUnsubscribe)
            _ = EnviarFrameAsync(BuildFrame("UNSUBSCRIBE", new Dictionary<string, string>
            {
                ["id"] = SubscriptionId(topic)
            }));
    }

    public async Task DesconectarAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            var ws = _ws;
            var cts = _cts;

            if (ws?.State == WebSocketState.Open)
            {
                await EnviarFrameAsync(BuildFrame("DISCONNECT", new()));
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }

            cts?.Cancel();
            ws?.Dispose();
            _ws = null;
            _cts = null;

            lock (_lock)
                _activeTopicSubscriptions.Clear();
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public Task DesconectarSiEstaInactivoAsync()
    {
        lock (_lock)
        {
            if (_subscriptions.Count > 0)
                return Task.CompletedTask;
        }

        return DesconectarAsync();
    }

    // ─── Loop lector ─────────────────────────────────────────────────────────

    private async Task BucleLecturaAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && Conectado)
            {
                var frame = await RecibirFrameAsync(ct);
                if (string.IsNullOrWhiteSpace(frame) || frame == "\n") continue; // heartbeat
                ProcesarFrame(frame);
            }
        }
        catch (OperationCanceledException) { /* normal al desconectar */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[STOMP] Error en bucle: {ex.Message}");
        }
    }

    private void ProcesarFrame(string frame)
    {
        if (!frame.StartsWith("MESSAGE", StringComparison.Ordinal)) return;

        // Extraer destination y body
        var lines = frame.Split('\n');
        string? destination = null;
        int bodyStart = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("destination:", StringComparison.Ordinal))
                destination = lines[i]["destination:".Length..].Trim();
            if (lines[i].Length == 0 && bodyStart == -1)
                bodyStart = i + 1;
        }

        if (destination == null || bodyStart < 0 || bodyStart >= lines.Length) return;

        var body = string.Join("\n", lines[bodyStart..]).TrimEnd('\0');

        NotificacionPedidoWs? notif = null;
        try { notif = JsonSerializer.Deserialize<NotificacionPedidoWs>(body, _jsonOpts); }
        catch { return; }
        if (notif == null) return;

        HashSet<Action<NotificacionPedidoWs>>? callbacks;
        lock (_lock) { _subscriptions.TryGetValue(destination, out callbacks); }

        if (callbacks == null) return;
        foreach (var cb in callbacks)
        {
            try { MainThread.BeginInvokeOnMainThread(() => cb(notif)); }
            catch { /* no propagar */ }
        }
    }

    // ─── Utilidades STOMP ─────────────────────────────────────────────────────

    private static string BuildFrame(string command, Dictionary<string, string> headers, string body = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine(command);
        foreach (var (k, v) in headers)
            sb.Append(k).Append(':').AppendLine(v);
        sb.AppendLine();  // línea en blanco separa encabezados de cuerpo
        sb.Append(body);
        sb.Append('\0');  // frame terminator
        return sb.ToString();
    }

    private async Task EnviarFrameAsync(string frame)
    {
        if (_ws is null || _ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(frame);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReenviarSuscripcionesAsync()
    {
        List<string> topics;
        lock (_lock)
            topics = [.. _subscriptions.Keys];

        foreach (var topic in topics)
            await SuscribirTopicEnServidorAsync(topic);
    }

    private async Task SuscribirTopicEnServidorAsync(string topic)
    {
        lock (_lock)
        {
            if (!_activeTopicSubscriptions.Add(topic))
                return;
        }

        await EnviarFrameAsync(BuildFrame("SUBSCRIBE", new Dictionary<string, string>
        {
            ["destination"] = topic,
            ["id"] = SubscriptionId(topic)
        }));
    }

    private static string SubscriptionId(string topic) => "sub-" + topic.GetHashCode();

    private async Task<string> RecibirFrameAsync(CancellationToken ct)
    {
        var buf    = new byte[BufSize];
        var sb     = new StringBuilder();
        WebSocketReceiveResult result;
        do
        {
            result = await _ws!.ReceiveAsync(new ArraySegment<byte>(buf), ct);
            if (result.MessageType == WebSocketMessageType.Close) return string.Empty;
            sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
        }
        while (!result.EndOfMessage);
        return sb.ToString();
    }

    // ─── JSON ─────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─── IAsyncDisposable ─────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        await DesconectarAsync();
    }
}
