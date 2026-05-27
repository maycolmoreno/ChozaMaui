using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Cuentas.
/// </summary>
public class CuentaApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CuentaApiService(HttpClient http) => _http = http;

    public async Task<List<CuentaResponse>> GetTodasCuentasAsync()
    {
        var r = await _http.GetAsync("/api/cuentas");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<List<CuentaResponse>>()) ?? [];
    }

    public async Task<List<CuentaResponse>> ObtenerCuentasAbiertasAsync()
    {
        var r = await _http.GetAsync("/api/cuentas/abiertas");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<List<CuentaResponse>>()) ?? [];
    }

    public async Task<CuentaResponse?> ObtenerCuentaAbiertaPorMesaAsync(int idMesa)
    {
        var r = await _http.GetAsync($"/api/cuentas/mesa/{idMesa}/abierta");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CuentaResponse>();
    }

    public async Task<CuentaResponse> CrearCuentaAsync(int idMesa, int idCliente, double total = 0)
    {
        var r = await _http.PostAsJsonAsync("/api/cuentas",
            new CuentaRequest { IdMesa = idMesa, IdCliente = idCliente, Total = total }, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }

    public async Task<CuentaResponse> AgregarPedidoACuentaAsync(int idCuenta, int idPedido)
    {
        var r = await _http.PostAsync($"/api/cuentas/{idCuenta}/pedidos/{idPedido}", null);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }

    public async Task<CuentaResponse> AsignarClienteCuentaAsync(int idCuenta, int idCliente)
    {
        var r = await _http.PatchAsJsonAsync($"/api/cuentas/{idCuenta}/cliente",
            new { idCliente }, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }

    public async Task<CuentaResponse> CerrarCuentaAsync(int idCuenta)
    {
        var r = await _http.PatchAsJsonAsync($"/api/cuentas/{idCuenta}/estado",
            new CambiarEstadoRequest { Estado = "PAGADA" }, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }
}
