using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class TurnoWorkflowService
{
    private readonly CajaApiService _cajaApi;
    private readonly CuentaApiService _cuentasApi;
    private readonly PagoApiService _pagosApi;
    private readonly ReporteApiService _reportesApi;
    private readonly SessionService _session;

    public TurnoWorkflowService(
        CajaApiService cajaApi,
        CuentaApiService cuentasApi,
        PagoApiService pagosApi,
        ReporteApiService reportesApi,
        SessionService session)
    {
        _cajaApi = cajaApi;
        _cuentasApi = cuentasApi;
        _pagosApi = pagosApi;
        _reportesApi = reportesApi;
        _session = session;
    }

    public async Task<TurnoDashboardSnapshot> CargarDashboardAsync()
    {
        var turnoTask = _cajaApi.ObtenerCajaAbiertaAsync();
        var cuentasTask = _cuentasApi.ObtenerCuentasAbiertasAsync();

        await Task.WhenAll(turnoTask, cuentasTask);

        return new TurnoDashboardSnapshot(
            turnoTask.Result,
            cuentasTask.Result,
            await CargarReportePermitidoAsync());
    }

    private async Task<ReporteVentasDia> CargarReportePermitidoAsync()
    {
        if (!string.Equals(_session.Rol, "ADMIN", StringComparison.OrdinalIgnoreCase))
            return new ReporteVentasDia();

        try
        {
            return await _reportesApi.GetReporteVentasDiaAsync();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return new ReporteVentasDia();
        }
    }

    public Task<CajaTurnoResponse> AbrirTurnoAsync(decimal monto, string usuario)
        => _cajaApi.AbrirCajaAsync(monto, usuario);

    public Task<CajaTurnoResponse> CerrarTurnoAsync(decimal monto, string usuario)
        => _cajaApi.CerrarCajaAsync(monto, usuario);

    public Task<PagoResponse> RegistrarPagoRapidoAsync(CuentaResponse cuenta, string metodo, string usuario)
        => _pagosApi.RegistrarPagoAsync(cuenta.Idcuenta, cuenta.Total, metodo, usuario);
}

public sealed record TurnoDashboardSnapshot(
    CajaTurnoResponse? TurnoActivo,
    IReadOnlyList<CuentaResponse> CuentasPendientes,
    ReporteVentasDia ReporteDia);
