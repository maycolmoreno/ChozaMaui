using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Clientes.
/// </summary>
public class ClienteApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ClienteApiService(HttpClient http) => _http = http;

    public async Task<List<ClienteResponse>> GetClientesAsync()
    {
        var r = await _http.GetAsync("/api/clientes");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<ClienteResponse>>()) ?? [];
    }

    public async Task<ClienteResponse> CrearClienteAsync(ClienteRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/clientes", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    public async Task<ClienteResponse> ActualizarClienteAsync(int id, ClienteRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/clientes/{id}", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    public async Task EliminarClienteAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/clientes/{id}");
        await ApiErrorHelper.EnsureSuccessAsync(r);
    }
}
