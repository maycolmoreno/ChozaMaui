using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class HistorialCuentasLoadService
{
    private readonly CajaApiService _caja;
    private readonly CuentaApiService _cuentas;

    public HistorialCuentasLoadService(CajaApiService caja, CuentaApiService cuentas)
    {
        _caja = caja;
        _cuentas = cuentas;
    }

    public async Task<HistorialCuentasLoadResult> CargarAsync(string? rol)
    {
        if (string.Equals(rol, "CAJERO", StringComparison.OrdinalIgnoreCase))
        {
            var caja = await _caja.ObtenerCajaAbiertaAsync();
            if (caja is null)
            {
                return HistorialCuentasLoadResult.RequiereCaja(
                    "Debes aperturar la caja antes de operar.");
            }
        }

        var cuentas = await _cuentas.GetTodasCuentasAsync();
        return HistorialCuentasLoadResult.Exitoso(cuentas);
    }
}

public sealed record HistorialCuentasLoadResult(
    bool RequiereAperturaCaja,
    string Mensaje,
    IReadOnlyList<CuentaResponse> Cuentas)
{
    public static HistorialCuentasLoadResult Exitoso(IReadOnlyList<CuentaResponse> cuentas)
        => new(false, string.Empty, cuentas);

    public static HistorialCuentasLoadResult RequiereCaja(string mensaje)
        => new(true, mensaje, []);
}