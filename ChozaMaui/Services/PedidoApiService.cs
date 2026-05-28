using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Pedidos.
/// </summary>
public class PedidoApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PedidoApiService(HttpClient http) => _http = http;

    public async Task<List<PedidoResponse>> GetPedidosAsync()
        => await _http.GetFromJsonAsync<List<PedidoResponse>>("/api/pedidos") ?? [];

    public async Task<PedidoResponse> GetPedidoPorIdAsync(int id)
        => (await _http.GetFromJsonAsync<PedidoResponse>($"/api/pedidos/{id}"))!;

    public async Task<PedidoResponse> GetPedidoRecientePorCuentaAsync(int idCuenta)
        => (await _http.GetFromJsonAsync<PedidoResponse>($"/api/pedidos/cuenta/{idCuenta}/reciente"))!;

    public async Task<PedidoResponse> CrearPedidoAsync(PedidoRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/pedidos", request, _camelCase);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CrearPedidoConCuentaAsync(PedidoRequest request, string estadoDestino)
    {
        var estado = Uri.EscapeDataString((estadoDestino ?? string.Empty).Trim().ToUpperInvariant());
        var response = await _http.PostAsJsonAsync($"/api/pedidos/con-cuenta?estadoDestino={estado}", request, _camelCase);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(int id, string nuevoEstado)
    {
        var estado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
        var rutaSemantica = estado switch
        {
            "EN_COCINA" => $"/api/pedidos/{id}/confirmar",
            "LISTO" or "LISTO_PARA_ENTREGA" => $"/api/pedidos/{id}/listo",
            "COMPLETADO" or "ENTREGADO" => $"/api/pedidos/{id}/entregado",
            "CANCELADO" => $"/api/pedidos/{id}/cancelar",
            _ => null
        };

        HttpResponseMessage response;
        if (rutaSemantica is not null)
        {
            response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Patch, rutaSemantica));
        }
        else
        {
            response = await _http.PatchAsJsonAsync(
                $"/api/pedidos/{id}/estado",
                new CambiarEstadoRequest { Estado = estado },
                _camelCase);
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }
}
