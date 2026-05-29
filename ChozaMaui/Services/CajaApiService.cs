using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Caja / Turnos.
/// </summary>
public class CajaApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SettingsService _settings;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CajaApiService(IHttpClientFactory httpClientFactory, SettingsService settings)
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

    public async Task<CajaTurnoResponse?> ObtenerCajaAbiertaAsync()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/caja/abierta");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CajaTurnoResponse>();
    }

    public async Task<CajaTurnoResponse> AbrirCajaAsync(decimal monto, string usuario)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/caja/apertura",
            new AperturaCajaRequest { MontoInicial = monto, UsuarioApertura = usuario }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CajaTurnoResponse>())!;
    }

    public async Task<CajaTurnoResponse> CerrarCajaAsync(decimal monto, string usuario)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/caja/cierre",
            new CierreCajaRequest { MontoDeclaradoCierre = monto, UsuarioCierre = usuario }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CajaTurnoResponse>())!;
    }

    public async Task<List<PagoResponse>> ListarPagosCajaAsync(int idCaja)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/api/caja/{idCaja}/pagos");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<PagoResponse>>(_camelCase)) ?? [];
    }
}
