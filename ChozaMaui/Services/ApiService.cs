using System.Net.Http.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Cliente HTTP centralizado para la API Pisip.
/// URL base: http://10.0.2.2:8081 (emulador Android → localhost del PC)
/// El token JWT se inyecta automáticamente por AuthHandler.
/// </summary>
public class ApiService
{
    // ── Cambiar según plataforma de prueba ─────────────────────────────
    //   Android emulador : http://10.0.2.2:8081
    //   Windows / iOS    : http://localhost:8081
    public const string BaseUrl = "http://10.0.2.2:8081";
    // ──────────────────────────────────────────────────────────────────

    private readonly HttpClient _http;

    // HttpClient es inyectado por IHttpClientFactory (MauiProgram).
    // El token y el timeout ya están configurados en AuthHandler y MauiProgram.
    public ApiService(HttpClient http) => _http = http;

    // ── Auth ───────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/usuarios/login",
            new LoginRequest { Username = username, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public async Task CambiarPasswordAsync(string passwordActual, string passwordNuevo)
    {
        var response = await _http.PostAsJsonAsync("/api/usuarios/cambiar-password",
            new CambiarPasswordRequest { PasswordActual = passwordActual, PasswordNuevo = passwordNuevo });
        response.EnsureSuccessStatusCode();
    }

    // ── Mesas ─────────────────────────────────────────────────────────

    public async Task<List<MesaResponse>> GetMesasDisponiblesAsync()
        => await _http.GetFromJsonAsync<List<MesaResponse>>("/api/mesas/disponibles") ?? [];

    public async Task<List<MesaResponse>> GetTodasMesasAsync()
        => await _http.GetFromJsonAsync<List<MesaResponse>>("/api/mesas") ?? [];

    // ── Categorías ────────────────────────────────────────────────────

    public async Task<List<CategoriaResponse>> GetCategoriasActivasAsync()
    {
        var result = await _http.GetFromJsonAsync<List<CategoriaResponse>>("/api/categorias");
        return result?.Where(c => c.Estado).ToList() ?? [];
    }

    // ── Productos ─────────────────────────────────────────────────────

    public async Task<List<ProductoResponse>> GetProductosActivosAsync()
        => await _http.GetFromJsonAsync<List<ProductoResponse>>("/api/productos/activos") ?? [];

    public async Task<List<ProductoResponse>> GetProductosPorCategoriaAsync(int idCategoria)
    {
        var result = await _http.GetFromJsonAsync<List<ProductoResponse>>($"/api/productos/categoria/{idCategoria}");
        return result?.Where(p => p.Estado).ToList() ?? [];
    }

    // ── Categorías CRUD ───────────────────────────────────────────────
    public async Task<List<CategoriaResponse>> GetTodasCategoriasAsync()
        => await _http.GetFromJsonAsync<List<CategoriaResponse>>("/api/categorias") ?? [];

    public async Task<CategoriaResponse> CrearCategoriaAsync(CategoriaRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/categorias", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task<CategoriaResponse> ActualizarCategoriaAsync(int id, CategoriaRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/categorias/{id}", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task EliminarCategoriaAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/categorias/{id}");
        r.EnsureSuccessStatusCode();
    }

    // ── Productos CRUD ────────────────────────────────────────────────
    public async Task<List<ProductoResponse>> GetTodosProductosAsync()
        => await _http.GetFromJsonAsync<List<ProductoResponse>>("/api/productos") ?? [];

    public async Task<ProductoResponse> CrearProductoAsync(ProductoRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/productos", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task<ProductoResponse> ActualizarProductoAsync(int id, ProductoRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/productos/{id}", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task EliminarProductoAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/productos/{id}");
        r.EnsureSuccessStatusCode();
    }

    // ── Pedidos ───────────────────────────────────────────────────────

    public async Task<List<PedidoResponse>> GetPedidosAsync()
        => await _http.GetFromJsonAsync<List<PedidoResponse>>("/api/pedidos") ?? [];

    public async Task<PedidoResponse> GetPedidoPorIdAsync(int id)
        => (await _http.GetFromJsonAsync<PedidoResponse>($"/api/pedidos/{id}"))!;

    public async Task<PedidoResponse> CrearPedidoAsync(PedidoRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/pedidos", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(int id, string nuevoEstado)
    {
        var response = await _http.PatchAsJsonAsync($"/api/pedidos/{id}/estado",
            new CambiarEstadoRequest { Estado = nuevoEstado });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    // ── Caja / Turnos ─────────────────────────────────────────────
    public async Task<CajaTurnoResponse?> ObtenerCajaAbiertaAsync()
    {
        var response = await _http.GetAsync("/api/caja/abierta");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CajaTurnoResponse>();
    }

    public async Task<CajaTurnoResponse> AbrirCajaAsync(decimal monto, string usuario)
    {
        var response = await _http.PostAsJsonAsync("/api/caja/apertura",
            new AperturaCajaRequest { MontoInicial = monto, UsuarioApertura = usuario });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CajaTurnoResponse>())!;
    }

    public async Task<CajaTurnoResponse> CerrarCajaAsync(decimal monto, string usuario)
    {
        var response = await _http.PostAsJsonAsync("/api/caja/cierre",
            new CierreCajaRequest { MontoDeclaradoCierre = monto, UsuarioCierre = usuario });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CajaTurnoResponse>())!;
    }

    // ── Comedores ──────────────────────────────────────────────────
    public async Task<List<ComedorResponse>> GetComedoresAsync()
        => await _http.GetFromJsonAsync<List<ComedorResponse>>("/api/comedores") ?? [];

    public async Task<ComedorResponse> CrearComedorAsync(ComedorRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/comedores", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ComedorResponse>())!;
    }

    public async Task<ComedorResponse> ActualizarComedorAsync(int id, ComedorRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/comedores/{id}", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ComedorResponse>())!;
    }

    public async Task EliminarComedorAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/comedores/{id}");
        r.EnsureSuccessStatusCode();
    }

    // ── Mesas ──────────────────────────────────────────────────────
    public async Task<MesaResponse> CrearMesaAsync(MesaUpdateRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/mesas", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    public async Task<MesaResponse> ActualizarMesaAsync(int id, MesaUpdateRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/mesas/{id}", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    public async Task EliminarMesaAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/mesas/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<MesaResponse>> ObtenerMesasAsync()
    {
        var response = await _http.GetAsync("/api/mesas");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<MesaResponse>>()) ?? [];
    }

    public async Task<MesaResponse> ActualizarEstadoMesaAsync(MesaResponse mesa, bool nuevoEstado)
    {
        var response = await _http.PutAsJsonAsync($"/api/mesas/{mesa.Idmesa}",
            new MesaUpdateRequest
            {
                Numero    = mesa.Numero,
                Capacidad = mesa.Capacidad,
                Estado    = nuevoEstado,
                Idcomedor = mesa.Idcomedor
            });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    // ── Cuentas ────────────────────────────────────────────────────
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

    public async Task<CuentaResponse> CrearCuentaAsync(int idMesa, int idCliente, double total = 0)
    {
        var r = await _http.PostAsJsonAsync("/api/cuentas",
            new CuentaRequest { IdMesa = idMesa, IdCliente = idCliente, Total = total });
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
            new { idCliente });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }

    public async Task<CuentaResponse> CerrarCuentaAsync(int idCuenta)
    {
        var r = await _http.PatchAsJsonAsync($"/api/cuentas/{idCuenta}/estado",
            new CambiarEstadoRequest { Estado = "CERRADA" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
    }

    // ── Pagos ──────────────────────────────────────────────────────
    public async Task<PagoResponse> RegistrarPagoAsync(int idCuenta, double monto, string metodo,
        string usuario, string? referencia = null)
    {
        var r = await _http.PostAsJsonAsync($"/api/cuentas/{idCuenta}/pagos",
            new PagoRequest { Monto = monto, Metodo = metodo, Usuario = usuario, Referencia = referencia });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PagoResponse>())!;
    }

    // ── Clientes ───────────────────────────────────────────────────
    public async Task<List<ClienteResponse>> GetClientesAsync()
    {
        var r = await _http.GetAsync("/api/clientes");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<List<ClienteResponse>>()) ?? [];
    }

    public async Task<ClienteResponse> CrearClienteAsync(ClienteRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/clientes", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    public async Task<ClienteResponse> ActualizarClienteAsync(int id, ClienteRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/clientes/{id}", req);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    public async Task EliminarClienteAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/clientes/{id}");
        r.EnsureSuccessStatusCode();
    }


    // ── Reportes -───────────────────────────────────────────────────
    public async Task<ReporteVentasDia> GetReporteVentasDiaAsync(DateTime? fecha = null)
    {
        var query = fecha.HasValue ? $"?fecha={fecha.Value:yyyy-MM-dd}" : string.Empty;
        var r = await _http.GetAsync($"/api/reportes/ventas-dia{query}");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ReporteVentasDia>()) ?? new();
    }
}
