using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class HistorialCuentasPresentationService
{
    public IReadOnlyList<CuentaResponse> Filtrar(
        IEnumerable<CuentaResponse> cuentas,
        string? filtroEstado,
        DateTime fechaDesde,
        DateTime fechaHasta,
        string? textoBusqueda)
    {
        var estado = filtroEstado ?? "TODAS";
        var lista = cuentas.AsEnumerable();

        if (estado == "COBRADAS")
            lista = lista.Where(EsCuentaCobrada);
        else if (estado != "TODAS")
            lista = lista.Where(c => string.Equals(c.Estado, estado, StringComparison.OrdinalIgnoreCase));

        lista = lista.Where(c =>
            c.FechaApertura.HasValue &&
            c.FechaApertura.Value.Date >= fechaDesde.Date &&
            c.FechaApertura.Value.Date <= fechaHasta.Date);

        if (!string.IsNullOrWhiteSpace(textoBusqueda))
        {
            lista = lista.Where(c =>
                (c.MesaTexto ?? string.Empty).Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                (c.ClienteTexto ?? string.Empty).Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Idcuenta.ToString().Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        return lista
            .OrderByDescending(c => c.FechaApertura)
            .ToList();
    }

    public HistorialCuentasStats CalcularStats(IEnumerable<CuentaResponse> cuentas)
    {
        var snapshot = cuentas.ToList();
        return new HistorialCuentasStats(
            snapshot.Count,
            snapshot.Count(EsCuentaPendiente),
            snapshot.Count(EsCuentaCobrada),
            snapshot.Where(EsCuentaCobrada).Sum(c => c.Total));
    }

    public bool EsCuentaPendiente(CuentaResponse cuenta)
        => string.Equals(cuenta.Estado, CuentaEstados.Abierta, StringComparison.OrdinalIgnoreCase);

    public bool EsCuentaCobrada(CuentaResponse cuenta)
        => string.Equals(cuenta.Estado, CuentaEstados.Pagada, StringComparison.OrdinalIgnoreCase)
        || string.Equals(cuenta.Estado, "CERRADA", StringComparison.OrdinalIgnoreCase);
}

public sealed record HistorialCuentasStats(
    int TotalCuentas,
    int CuentasAbiertas,
    int CuentasCerradas,
    double TotalFacturado);
