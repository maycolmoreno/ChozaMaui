using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class TurnoWorkflowService
{
    private readonly CajaApiService _cajaApi;

    public TurnoWorkflowService(
        CajaApiService cajaApi)
    {
        _cajaApi = cajaApi;
    }

    public async Task<TurnoDashboardSnapshot> CargarDashboardAsync()
    {
        var turno = await _cajaApi.ObtenerCajaAbiertaAsync();
        var pagos = turno is null
            ? []
            : await _cajaApi.ListarPagosCajaAsync(turno.Idcaja);

        return new TurnoDashboardSnapshot(
            turno,
            pagos.OrderByDescending(p => p.Fecha).ToList());
    }

    public Task<CajaTurnoResponse> AbrirTurnoAsync(decimal monto, string usuario)
        => _cajaApi.AbrirCajaAsync(monto, usuario);

    public Task<CajaTurnoResponse> CerrarTurnoAsync(decimal monto, string usuario)
        => _cajaApi.CerrarCajaAsync(monto, usuario);

}

public sealed record TurnoDashboardSnapshot(
    CajaTurnoResponse? TurnoActivo,
    IReadOnlyList<PagoResponse> PagosTurno);
