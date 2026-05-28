using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;
using System.Collections.ObjectModel;

namespace ChozaMaui.ViewModels;

public partial class TurnoViewModel : ObservableObject
{
    private readonly RoleCapabilityService _capabilities;
    private readonly SessionService _session;
    private readonly TurnoWorkflowService _workflow;
    private DateTimeOffset? _ultimaCargaUtc;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Estado de caja ────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAclaracionMetodos))]
    private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoCajaTexto))]
    [NotifyPropertyChangedFor(nameof(FondoInicialTexto))]
    [NotifyPropertyChangedFor(nameof(HoraAperturaTexto))]
    [NotifyPropertyChangedFor(nameof(NumeroCajaTexto))]
    [NotifyPropertyChangedFor(nameof(TieneTurnoAbierto))]
    private CajaTurnoResponse? turnoActivo;

    // ── Formularios apertura / cierre ─────────────────────────────────
    [ObservableProperty] private string montoInicial = string.Empty;
    [ObservableProperty] private string montoFinal   = string.Empty;
    [ObservableProperty] private bool mostrarFormApertura;
    [ObservableProperty] private bool mostrarFormCierre;

    // ── Colecciones ───────────────────────────────────────────────────
    public ObservableCollection<CuentaResponse> CuentasPendientes { get; } = [];
    public ObservableCollection<PagoResponse>   HistorialPagos    { get; } = [];

    // ── Seleccion y pago rapido ───────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneCuentaSeleccionada))]
    [NotifyPropertyChangedFor(nameof(TotalCuentaTexto))]
    [NotifyPropertyChangedFor(nameof(CambioCalculado))]
    [NotifyPropertyChangedFor(nameof(HayCambio))]
    private CuentaResponse? cuentaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsMetodoEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsMetodoTarjeta))]
    [NotifyPropertyChangedFor(nameof(EsMetodoTransferencia))]
    [NotifyPropertyChangedFor(nameof(EsMetodoMixto))]
    private string metodoPago = "EFECTIVO";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CambioCalculado))]
    [NotifyPropertyChangedFor(nameof(HayCambio))]
    private double montoRecibido;

    [ObservableProperty] private bool   pagoCargando;
    [ObservableProperty] private bool   pagoRegistradoOk;
    [ObservableProperty] private string mensajePago = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAclaracionMetodos))]
    [NotifyPropertyChangedFor(nameof(AclaracionMetodosTexto))]
    private bool resumenTurnoCargado;

    // ── Metricas del turno ────────────────────────────────────────────
    [ObservableProperty] private double metricaVentas;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AclaracionMetodosTexto))]
    private int    metricaPagosCount;
    [ObservableProperty] private double metricaEfectivo;
    [ObservableProperty] private double metricaTarjeta;
    [ObservableProperty] private double metricaTransferencias;
    [ObservableProperty] private double metricaOtros;

    // ── Propiedades computadas ────────────────────────────────────────
    public string EstadoCajaTexto   => TurnoActivo?.Estado == "ABIERTA" ? "Abierta"
                                       : TurnoActivo is null ? "Sin caja" : "Cerrada";
    public string FondoInicialTexto => TurnoActivo is null ? "$0.00" : $"${TurnoActivo.MontoInicial:0.00}";
    public string HoraAperturaTexto => TurnoActivo?.FechaApertura?.ToString("dd MMM · HH:mm") ?? "--";
    public string NumeroCajaTexto   => TurnoActivo is null ? "Caja #-" : $"Caja #{TurnoActivo.Idcaja}";
    public string NombreUsuario     => _session.NombreCompleto ?? _session.Username ?? "Cajero";

    public bool   TieneTurnoAbierto      => TurnoActivo?.Estado == "ABIERTA";
    public bool   TieneCuentaSeleccionada => CuentaSeleccionada is not null;
    public string TotalCuentaTexto        => CuentaSeleccionada is null ? "$0.00"
                                             : $"${CuentaSeleccionada.Total:0.00}";
    public double CambioCalculado         => EsMetodoEfectivo && MontoRecibido > (CuentaSeleccionada?.Total ?? 0)
                                             ? Math.Round(MontoRecibido - (CuentaSeleccionada?.Total ?? 0), 2) : 0;
    public bool   HayCambio               => CambioCalculado > 0;
    public bool   EsMetodoEfectivo        => MetodoPago == "EFECTIVO";
    public bool   EsMetodoTarjeta         => MetodoPago == "TARJETA";
    public bool   EsMetodoTransferencia   => MetodoPago == "TRANSFERENCIA";
    public bool   EsMetodoMixto           => MetodoPago == "MIXTO";
    public bool   PuedeGestionarCaja      => _capabilities.PuedeGestionarCaja(_session.Rol);
    public bool   MostrarAclaracionMetodos => ResumenTurnoCargado && !IsBusy;
    public string AclaracionMetodosTexto => MetricaPagosCount == 0
        ? "Sin pagos registrados hoy. Los montos en $0.00 ya corresponden al cierre real del dia hasta ahora."
        : "Los metodos que siguen en $0.00 no registran movimientos hoy.";

    public TurnoViewModel(RoleCapabilityService capabilities, SessionService session, TurnoWorkflowService workflow)
    {
        _capabilities = capabilities;
        _session = session;
        _workflow = workflow;
    }

    // ── Cargar datos ──────────────────────────────────────────────────
    [RelayCommand]
    public async Task CargarAsync()
    {
        await CargarInternoAsync(force: true);
    }

    public Task CargarSiEsNecesarioAsync()
        => CargarInternoAsync(force: false);

    private async Task CargarInternoAsync(bool force)
    {
        if (!force && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
            return;

        IsBusy  = true;
        Mensaje = string.Empty;
        try
        {
            var snapshot = await _workflow.CargarDashboardAsync();

            TurnoActivo = snapshot.TurnoActivo;

            CuentasPendientes.Clear();
            foreach (var c in snapshot.CuentasPendientes)
                CuentasPendientes.Add(c);

            var r = snapshot.ReporteDia;
            MetricaVentas     = r.TotalVentas;
            MetricaPagosCount = r.NumeroPedidos;
            MetricaEfectivo = r.TotalEfectivo;
            MetricaTarjeta = r.TotalTarjeta;
            MetricaTransferencias = r.TotalTransferencias;
            MetricaOtros = r.TotalOtros;
            ResumenTurnoCargado = true;
            _ultimaCargaUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex) { Mensaje = $"Error al cargar: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Abrir caja ────────────────────────────────────────────────────
    [RelayCommand]
    public async Task AbrirTurnoAsync()
    {
        if (!decimal.TryParse(MontoInicial,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monto))
        { Mensaje = "Monto invalido."; return; }

        IsBusy  = true;
        Mensaje = string.Empty;
        try
        {
            TurnoActivo         = await _workflow.AbrirTurnoAsync(monto, _session.Username ?? "cajero");
            MontoInicial        = string.Empty;
            MostrarFormApertura = false;
            Mensaje             = "Caja abierta correctamente.";
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Cerrar caja ───────────────────────────────────────────────────
    [RelayCommand]
    public async Task CerrarTurnoAsync()
    {
        if (!decimal.TryParse(MontoFinal,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monto))
        { Mensaje = "Monto invalido."; return; }

        IsBusy  = true;
        Mensaje = string.Empty;
        try
        {
            TurnoActivo       = await _workflow.CerrarTurnoAsync(monto, _session.Username ?? "cajero");
            MontoFinal        = string.Empty;
            MostrarFormCierre = false;
            Mensaje           = "Caja cerrada correctamente.";
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Toggles de formularios ────────────────────────────────────────
    [RelayCommand]
    private void ToggleFormApertura()
    {
        MostrarFormApertura = !MostrarFormApertura;
        MostrarFormCierre   = false;
    }

    [RelayCommand]
    private void ToggleFormCierre()
    {
        MostrarFormCierre   = !MostrarFormCierre;
        MostrarFormApertura = false;
    }

    // ── Seleccion de metodo de pago ───────────────────────────────────
    [RelayCommand]
    private void SeleccionarMetodoPago(string metodo)
    {
        MetodoPago    = metodo;
        MontoRecibido = 0;
    }

    // ── Seleccionar cuenta para pago rapido ───────────────────────────
    [RelayCommand]
    private void SeleccionarCuentaPago(CuentaResponse cuenta)
    {
        CuentaSeleccionada = cuenta;
        MontoRecibido      = 0;
    }

    // ── Registrar pago rapido ─────────────────────────────────────────
    [RelayCommand]
    private async Task RegistrarPagoRapidoAsync()
    {
        if (CuentaSeleccionada is null)
        { MensajePago = "Selecciona una cuenta."; return; }

        PagoCargando = true;
        MensajePago  = string.Empty;
        try
        {
            var cuenta = CuentaSeleccionada;
            var monto = cuenta.Total;
            var pago  = await _workflow.RegistrarPagoRapidoAsync(cuenta, MetodoPago, _session.Username ?? "cajero");

            HistorialPagos.Insert(0, pago);
            if (HistorialPagos.Count > 15)
                HistorialPagos.RemoveAt(HistorialPagos.Count - 1);

            CuentasPendientes.Remove(cuenta);

            MetricaVentas     += monto;
            MetricaPagosCount += 1;
            switch (MetodoPago)
            {
                case "EFECTIVO":      MetricaEfectivo       += monto; break;
                case "TARJETA":       MetricaTarjeta        += monto; break;
                case "TRANSFERENCIA": MetricaTransferencias += monto; break;
                default:              MetricaOtros          += monto; break;
            }

            CuentaSeleccionada = null;
            MontoRecibido      = 0;
            PagoRegistradoOk   = true;
            await Task.Delay(2500);
            PagoRegistradoOk   = false;
        }
        catch (Exception ex) { MensajePago = $"Error: {ex.Message}"; }
        finally { PagoCargando = false; }
    }
}
