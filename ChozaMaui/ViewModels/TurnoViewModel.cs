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
    [ObservableProperty] private bool isBusy;
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
    public ObservableCollection<PagoResponse>   HistorialPagos    { get; } = [];

    // ── Propiedades computadas ────────────────────────────────────────
    public string EstadoCajaTexto   => TurnoActivo?.Estado == CuentaEstados.Abierta ? "Abierta"
                                       : TurnoActivo is null ? "Sin caja" : "Cerrada";
    public string FondoInicialTexto => TurnoActivo is null ? "$0.00" : $"${TurnoActivo.MontoInicial:0.00}";
    public string HoraAperturaTexto => TurnoActivo?.FechaApertura?.ToString("dd MMM · HH:mm") ?? "--";
    public string NumeroCajaTexto   => TurnoActivo is null ? "Caja #-" : $"Caja #{TurnoActivo.Idcaja}";
    public string NombreUsuario     => _session.NombreCompleto ?? _session.Username ?? "Cajero";

    public bool   TieneTurnoAbierto      => TurnoActivo?.Estado == CuentaEstados.Abierta;
    public bool   PuedeGestionarCaja      => _capabilities.PuedeGestionarCaja(_session.Rol);
    public string TotalPagosTurnoTexto => $"${HistorialPagos.Sum(p => p.Monto):0.00}";

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

            HistorialPagos.Clear();
            foreach (var pago in snapshot.PagosTurno)
                HistorialPagos.Add(pago);
            OnPropertyChanged(nameof(TotalPagosTurnoTexto));

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

}
