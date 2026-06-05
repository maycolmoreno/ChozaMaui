using System.Net;
using System.Net.Http;

namespace ChozaMaui.Services;

public enum BackendConnectivityFailure
{
    None,
    NoInternet,
    InvalidServer,
    Timeout,
    ServerUnavailable,
    HttpError
}

public sealed record BackendHealthCheckResult(
    bool IsOnline,
    bool HasInternet,
    BackendConnectivityFailure Failure,
    string Message)
{
    public static BackendHealthCheckResult Online(string message)
        => new(true, true, BackendConnectivityFailure.None, message);

    public static BackendHealthCheckResult Offline(
        BackendConnectivityFailure failure,
        string message,
        bool hasInternet = true)
        => new(false, hasInternet, failure, message);
}

public sealed class ServerConnectionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SettingsService _settings;

    public ServerConnectionService(IHttpClientFactory httpClientFactory, SettingsService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async Task<(bool ok, string estado)> PingAsync()
    {
        var result = await CheckAsync();
        return (result.IsOnline, result.Message);
    }

    public async Task<BackendHealthCheckResult> CheckAsync()
    {
        if (!_settings.TryGetBaseUri(out var baseUri, out var errorMessage))
            return BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.InvalidServer,
                $"Servidor invalido: {errorMessage}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("ChozaApi");
            client.BaseAddress = baseUri;

            var response = await client.GetAsync("/actuator/health", cts.Token);
            System.Diagnostics.Debug.WriteLine($"[PERF][Connectivity] Health check API: {sw.ElapsedMilliseconds} ms");

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return body.Contains("UP", StringComparison.OrdinalIgnoreCase)
                    ? BackendHealthCheckResult.Online("Servidor en linea")
                    : BackendHealthCheckResult.Offline(
                        BackendConnectivityFailure.HttpError,
                        "Servidor respondio pero no esta saludable");
            }

            if (response.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                return BackendHealthCheckResult.Online("Servidor disponible");
            }

            return BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.HttpError,
                $"Servidor no disponible. HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[PERF][Connectivity] Health check API timeout: {sw.ElapsedMilliseconds} ms");
            return BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.Timeout,
                "Tiempo de espera agotado conectando con el servidor.");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR][Connectivity] Servidor no disponible: {ex.Message}");
            return BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.ServerUnavailable,
                "Servidor no disponible. Verifica que la API este encendida y la URL sea correcta.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR][Connectivity] Health check error: {ex}");
            return BackendHealthCheckResult.Offline(
                BackendConnectivityFailure.ServerUnavailable,
                $"Servidor no disponible: {ex.Message}");
        }
    }
}
