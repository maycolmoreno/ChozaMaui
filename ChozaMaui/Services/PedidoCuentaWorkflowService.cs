using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PedidoCuentaWorkflowService
{
    private readonly CuentaApiService _cuentasApi;

    public PedidoCuentaWorkflowService(CuentaApiService cuentasApi)
    {
        _cuentasApi = cuentasApi;
    }

    public async Task<PedidoCuentaResolutionResult> ResolverCuentaAsync(
        int mesaId,
        int clienteId,
        double totalInicial,
        CuentaResponse? cuentaActual = null)
    {
        if (cuentaActual is not null)
            return PedidoCuentaResolutionResult.Existente(cuentaActual);

        var cuentasAbiertas = await _cuentasApi.ObtenerCuentasAbiertasAsync();
        var cuentaExistente = cuentasAbiertas
            .FirstOrDefault(c => c.Mesa?.Idmesa == mesaId && c.Cliente?.Idcliente == clienteId);

        if (cuentaExistente is not null)
            return PedidoCuentaResolutionResult.Existente(cuentaExistente);

        var nuevaCuenta = await _cuentasApi.CrearCuentaAsync(mesaId, clienteId, totalInicial);
        return PedidoCuentaResolutionResult.Creada(nuevaCuenta);
    }

    public Task<CuentaResponse?> ObtenerCuentaAbiertaPorMesaAsync(int mesaId)
        => _cuentasApi.ObtenerCuentaAbiertaPorMesaAsync(mesaId);

    public Task<CuentaResponse> CerrarCuentaAsync(int cuentaId)
        => _cuentasApi.CerrarCuentaAsync(cuentaId);

    public async Task<CuentaResponse> AsegurarPedidoEnCuentaAsync(CuentaResponse cuenta, int pedidoId)
    {
        try
        {
            return await _cuentasApi.AgregarPedidoACuentaAsync(cuenta.Idcuenta, pedidoId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest)
        {
            return cuenta;
        }
    }
}

public sealed record PedidoCuentaResolutionResult(CuentaResponse Cuenta, bool CuentaCreada)
{
    public static PedidoCuentaResolutionResult Existente(CuentaResponse cuenta) => new(cuenta, false);
    public static PedidoCuentaResolutionResult Creada(CuentaResponse cuenta) => new(cuenta, true);
}