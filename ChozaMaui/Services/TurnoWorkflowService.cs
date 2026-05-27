using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class TurnoWorkflowService
{
    private readonly CajaApiService _cajaApi;
    private readonly CuentaApiService _cuentasApi;
    private readonly PagoApiService _pagosApi;
    private readonly ReporteApiService _reportesApi;

    public TurnoWorkflowService(
        CajaApiService cajaApi,
        CuentaApiService cuentasApi,
        PagoApiService pagosApi,
        ReporteApiService reportesApi)
    {
        _cajaApi = cajaApi;
        _cuentasApi = cuentasApi;
        _pagosApi = pagosApi;
        _reportesApi = reportesApi;
    }

    public async Task<TurnoDashboardSnapshot> CargarDashboardAsync()
    {
        var turnoTask = _cajaApi.ObtenerCajaAbiertaAsync();
        var cuentasTask = _cuentasApi.ObtenerCuentasAbiertasAsync();
        var reporteTask = _reportesApi.GetReporteVentasDiaAsync();

        await Task.WhenAll(turnoTask, cuentasTask, reporteTask);

        return new TurnoDashboardSnapshot(
            turnoTask.Result,
            cuentasTask.Result,
            reporteTask.Result);
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