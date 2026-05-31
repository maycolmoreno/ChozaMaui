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
    private readonly SessionService _session;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PedidoApiService(HttpClient http, SessionService session)
    {
        _http = http;
        _session = session;
    }

    public async Task<List<PedidoResponse>> GetPedidosAsync()
    {
        var response = await _http.GetAsync("/api/pedidos");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<PedidoResponse>>()) ?? [];
    }

    public async Task<PedidoResponse> GetPedidoPorIdAsync(int id)
    {
        var response = await _http.GetAsync($"/api/pedidos/{id}");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<List<PedidoHistorialResponse>> ObtenerHistorialPedidoAsync(int id)
    {
        var response = await _http.GetAsync($"/api/pedidos/{id}/historial");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<PedidoHistorialResponse>>()) ?? [];
    }

    public async Task<PedidoResponse> GetPedidoRecientePorCuentaAsync(int idCuenta)
    {
        var response = await _http.GetAsync($"/api/pedidos/cuenta/{idCuenta}/reciente");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CrearPedidoAsync(PedidoRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/pedidos", request, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CrearPedidoConCuentaAsync(PedidoRequest request, string estadoDestino)
    {
        var estado = Uri.EscapeDataString((estadoDestino ?? string.Empty).Trim().ToUpperInvariant());
        var response = await _http.PostAsJsonAsync($"/api/pedidos/con-cuenta?estadoDestino={estado}", request, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(int id, string nuevoEstado)
    {
        var estado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
        var rutaSemantica = estado switch
        {
            PedidoEstados.EnCocina => string.Equals(_session.Rol, "COCINA", StringComparison.OrdinalIgnoreCase)
                ? $"/api/pedidos/{id}/preparando"
                : $"/api/pedidos/{id}/confirmar",
            PedidoEstados.Listo or PedidoEstados.ListoParaEntrega => $"/api/pedidos/{id}/listo",
            PedidoEstados.Completado or PedidoEstados.Entregado => $"/api/pedidos/{id}/entregado",
            PedidoEstados.Cancelado => $"/api/pedidos/{id}/cancelar",
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

        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }
}
