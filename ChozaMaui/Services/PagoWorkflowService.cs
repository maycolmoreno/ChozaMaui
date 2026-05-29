using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PagoWorkflowService
{
    private static readonly TimeSpan CuentaMesaCacheTtl = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ResumenCuentaCacheTtl = TimeSpan.FromSeconds(15);
    private readonly PedidoCuentaWorkflowService _pedidoCuentaWorkflow;
    private readonly PagoApiService _pagosApi;
    private readonly SessionCacheService _cache;
    private readonly MesaStateService _mesas;

    public PagoWorkflowService(PedidoCuentaWorkflowService pedidoCuentaWorkflow, PagoApiService pagosApi, SessionCacheService cache, MesaStateService mesas)
    {
        _pedidoCuentaWorkflow = pedidoCuentaWorkflow;
        _pagosApi = pagosApi;
        _cache = cache;
        _mesas = mesas;
    }

    public async Task<(CuentaResponse? cuenta, double saldoPendiente)> CargarContextoAsync(PedidoResponse pedido)
    {
        var cuenta = pedido.Mesa is null
            ? null
            : await ObtenerCuentaAbiertaPorMesaAsync(pedido.Mesa.Idmesa);

        var saldoPendiente = await ObtenerSaldoPendienteAsync(cuenta, pedido.Total);
        return (cuenta, saldoPendiente);
    }

    public async Task<(CuentaResponse cuenta, double saldoPendiente)> PrepararCuentaParaCobroAsync(
        PedidoResponse pedido,
        CuentaResponse? cuentaActual)
    {
        var cuenta = cuentaActual;
        if (cuenta is null)
        {
            if (pedido.Cliente is null)
                throw new InvalidOperationException("El pedido no tiene cliente asignado.");

            var resolucion = await _pedidoCuentaWorkflow.ResolverCuentaAsync(
                pedido.Mesa?.Idmesa ?? 0,
                pedido.Cliente.Idcliente,
                pedido.Total,
                cuenta);

            cuenta = resolucion.Cuenta;

            if (resolucion.CuentaCreada)
                await GuardarResumenCuentaEnCacheAsync(cuenta.Idcuenta, cuenta.Total);

            if (pedido.Mesa is not null)
                await GuardarCuentaAbiertaEnCacheAsync(pedido.Mesa.Idmesa, cuenta);
        }

        cuenta = await _pedidoCuentaWorkflow.AsegurarPedidoEnCuentaAsync(cuenta, pedido.Idpedido);
        var saldoPendiente = await ObtenerSaldoPendienteAsync(cuenta, pedido.Total);
        return (cuenta, saldoPendiente);
    }

    public async Task<PagoRegistroCobroResult> RegistrarCobroAsync(
        PedidoResponse pedido,
        CuentaResponse? cuentaActual,
        double monto,
        string metodo,
        string usuario,
        string? referencia)
    {
        var (cuenta, saldoPendiente) = await PrepararCuentaParaCobroAsync(pedido, cuentaActual);
        var pago = await RegistrarPagoAsync(cuenta, monto, metodo, usuario, referencia);
        await InvalidarResumenCuentaAsync(cuenta.Idcuenta);

        return new PagoRegistroCobroResult(
            cuenta,
            pago,
            saldoPendiente,
            pago.SaldoPendienteCuenta <= 0);
    }

    public async Task<PagoRegistroCobroResult> RegistrarCobroConComprobanteAsync(
        PedidoResponse pedido,
        CuentaResponse? cuentaActual,
        double monto,
        string metodo,
        string usuario,
        string rutaArchivo,
        string? referencia)
    {
        var (cuenta, saldoPendiente) = await PrepararCuentaParaCobroAsync(pedido, cuentaActual);
        var pago = await _pagosApi.RegistrarPagoConComprobanteAsync(
            cuenta.Idcuenta,
            monto,
            metodo,
            usuario,
            rutaArchivo,
            referencia);

        await InvalidarResumenCuentaAsync(cuenta.Idcuenta);

        if (pedido.Mesa is not null)
            await _cache.RemoveAsync(BuildCuentaMesaKey(pedido.Mesa.Idmesa));

        return new PagoRegistroCobroResult(
            cuenta,
            pago,
            saldoPendiente,
            pago.SaldoPendienteCuenta <= 0);
    }

    public async Task<PagoCierreMesaResult> CerrarMesaCobradaAsync(CuentaResponse cuenta, PedidoResponse pedido)
    {
        var cuentaCerrada = await CerrarCuentaAsync(cuenta);
        await InvalidarMesasSiAplicaAsync(pedido);
        await InvalidarResumenCuentaAsync(cuenta.Idcuenta);

        if (pedido.Mesa is not null)
            await _cache.RemoveAsync(BuildCuentaMesaKey(pedido.Mesa.Idmesa));

        return new PagoCierreMesaResult(cuentaCerrada, pedido.Mesa);
    }

    public Task<PagoResponse> RegistrarPagoAsync(
        CuentaResponse cuenta,
        double monto,
        string metodo,
        string usuario,
        string? referencia)
    {
        return _pagosApi.RegistrarPagoAsync(
            cuenta.Idcuenta,
            monto,
            metodo,
            usuario,
            string.IsNullOrWhiteSpace(referencia) ? null : referencia);
    }

    public Task<CuentaResponse> CerrarCuentaAsync(CuentaResponse cuenta)
        => _pedidoCuentaWorkflow.CerrarCuentaAsync(cuenta.Idcuenta);

    public async Task<MesaResponse?> LiberarMesaAsync(PedidoResponse pedido)
    {
        if (pedido.Mesa is null)
            return null;

        await _mesas.InvalidarAsync();
        return pedido.Mesa;
    }

    private Task InvalidarMesasSiAplicaAsync(PedidoResponse pedido)
        => pedido.Mesa is null ? Task.CompletedTask : _mesas.InvalidarAsync();

    private Task<CuentaResponse?> ObtenerCuentaAbiertaPorMesaAsync(int idMesa)
        => _cache.GetOrCreateAsync(
            BuildCuentaMesaKey(idMesa),
            CuentaMesaCacheTtl,
            () => _pedidoCuentaWorkflow.ObtenerCuentaAbiertaPorMesaAsync(idMesa));

    private Task GuardarCuentaAbiertaEnCacheAsync(int idMesa, CuentaResponse cuenta)
        => _cache.SetAsync(BuildCuentaMesaKey(idMesa), (CuentaResponse?)cuenta, CuentaMesaCacheTtl);

    private static string BuildCuentaMesaKey(int idMesa) => $"cuenta:mesa:{idMesa}";
    private static string BuildResumenCuentaKey(int idCuenta) => $"cuenta:resumen:{idCuenta}";

    private async Task<double> ObtenerSaldoPendienteAsync(CuentaResponse? cuenta, double totalPedido)
    {
        if (cuenta is null)
            return totalPedido;

        try
        {
            var resumen = await ObtenerResumenCuentaAsync(cuenta.Idcuenta);
            return resumen.SaldoPendiente;
        }
        catch
        {
            return Math.Max(cuenta.Total, totalPedido);
        }
    }

    private Task<SaldoCuentaResponse> ObtenerResumenCuentaAsync(int idCuenta)
        => _cache.GetOrCreateAsync(
            BuildResumenCuentaKey(idCuenta),
            ResumenCuentaCacheTtl,
            () => _pagosApi.ObtenerResumenCuentaAsync(idCuenta));

    private Task GuardarResumenCuentaEnCacheAsync(int idCuenta, double saldoPendiente)
        => _cache.SetAsync(
            BuildResumenCuentaKey(idCuenta),
            new SaldoCuentaResponse
            {
                Idcuenta = idCuenta,
                SaldoPendiente = saldoPendiente,
                TotalPagado = 0
            },
            ResumenCuentaCacheTtl);

    private Task InvalidarResumenCuentaAsync(int idCuenta)
        => _cache.RemoveAsync(BuildResumenCuentaKey(idCuenta));
}

public sealed record PagoRegistroCobroResult(
    CuentaResponse Cuenta,
    PagoResponse Pago,
    double SaldoPendiente,
    bool PagoCompleto);

public sealed record PagoCierreMesaResult(
    CuentaResponse Cuenta,
    MesaResponse? MesaLiberada);
