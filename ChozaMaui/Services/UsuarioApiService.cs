using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para autenticacion y usuario.
/// </summary>
public class UsuarioApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SettingsService _settings;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public UsuarioApiService(IHttpClientFactory httpClientFactory, SettingsService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    private HttpClient CreateClient()
    {
        if (!_settings.TryGetBaseUri(out var baseUri, out var errorMessage))
            throw new InvalidOperationException(errorMessage);

        var client = _httpClientFactory.CreateClient("ChozaApi");
        client.BaseAddress = baseUri;
        return client;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/usuarios/login",
            new LoginRequest { Username = username, Password = password }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public async Task CambiarPasswordAsync(string passwordActual, string passwordNuevo)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/usuarios/cambiar-password",
            new CambiarPasswordRequest { PasswordActual = passwordActual, PasswordNuevo = passwordNuevo }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
    }
}
