namespace ChozaMaui.Services;

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
        if (!_settings.TryGetBaseUri(out var baseUri, out var errorMessage))
            return (false, $"Servidor invalido: {errorMessage}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var client = _httpClientFactory.CreateClient("ChozaApi");
            client.BaseAddress = baseUri;

            var response = await client.GetAsync("/actuator/health", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return body.Contains("UP", StringComparison.OrdinalIgnoreCase)
                    ? (true, "Servidor en linea ✔")
                    : (false, "Servidor respondio pero no esta saludable");
            }

            return (false, $"Error HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return (false, "Tiempo de espera agotado (5 s)");
        }
        catch (Exception ex)
        {
            return (false, $"Sin conexion: {ex.Message}");
        }
    }
}