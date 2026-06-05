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
    private readonly NotificationService _notifications;
    private DateTimeOffset? _ultimaCargaUtc;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Estado de caja ────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoCajaTexto))]
    [NotifyPropertyChangedFor(nameof(PuedeMostrarAccionesCaja))]
    private bool cajaCargada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoCajaTexto))]
    [NotifyPropertyChangedFor(nameof(PuedeMostrarAccionesCaja))]
    private bool errorCargaCaja;

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
    [ObservableProperty] private string inicialesUsuario = "U";
    [ObservableProperty] private string rolUsuarioHeader = "Cajero";
    [ObservableProperty] private string headerKpi1Titulo = "Caja";
    [ObservableProperty] private string headerKpi1Valor = "#-";
    [ObservableProperty] private string headerKpi2Titulo = "Turno";
    [ObservableProperty] private string headerKpi2Valor = "Sin caja";
    [ObservableProperty] private string headerKpi3Titulo = "Ventas";
    [ObservableProperty] private string headerKpi3Valor = "$0.00";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;

    // ── Colecciones ───────────────────────────────────────────────────
    public ObservableCollection<PagoResponse>   HistorialPagos    { get; } = [];

    // ── Propiedades computadas ────────────────────────────────────────
    public string EstadoCajaTexto   => ErrorCargaCaja ? "No disponible"
                                       : !CajaCargada ? "Cargando caja"
                                       : EsCajaAbierta ? "Abierta"
                                       : TurnoActivo is null ? "Sin caja" : "Cerrada";
    public string FondoInicialTexto => TurnoActivo is null ? "$0.00" : $"${TurnoActivo.MontoInicial:0.00}";
    public string HoraAperturaTexto => TurnoActivo?.FechaApertura?.ToString("dd MMM · HH:mm") ?? "--";
    public string NumeroCajaTexto   => TurnoActivo is null ? "Caja #-" : $"Caja #{TurnoActivo.Idcaja}";
    public string NombreUsuario     => _session.NombreCompleto ?? _session.Username ?? "Cajero";

    private bool EsCajaAbierta => string.Equals(TurnoActivo?.Estado, CuentaEstados.Abierta, StringComparison.OrdinalIgnoreCase);
    public bool   TieneTurnoAbierto      => EsCajaAbierta;
    public bool   PuedeGestionarCaja      => _capabilities.PuedeGestionarCaja(_session.Rol);
    public bool   PuedeMostrarAccionesCaja => PuedeGestionarCaja && CajaCargada && !ErrorCargaCaja;
    public string TotalPagosTurnoTexto => $"${HistorialPagos.Sum(p => p.Monto):0.00}";
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;

    public TurnoViewModel(RoleCapabilityService capabilities, SessionService session, TurnoWorkflowService workflow, NotificationService notifications)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _capabilities = capabilities;
        _session = session;
        _workflow = workflow;
        _notifications = notifications;
        ActualizarHeaderOperativo();
        System.Diagnostics.Debug.WriteLine($"[PERF][TurnoViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (!force && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
            return;

        IsBusy  = true;
        Mensaje = string.Empty;
        ErrorCargaCaja = false;
        CajaCargada = false;
        try
        {
            var snapshot = await _workflow.CargarDashboardAsync();

            TurnoActivo = snapshot.TurnoActivo;
            CajaCargada = true;

            HistorialPagos.Clear();
            foreach (var pago in snapshot.PagosTurno)
                HistorialPagos.Add(pago);
            OnPropertyChanged(nameof(TotalPagosTurnoTexto));
            ActualizarHeaderOperativo();

            _ultimaCargaUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            TurnoActivo = null;
            HistorialPagos.Clear();
            OnPropertyChanged(nameof(TotalPagosTurnoTexto));
            ErrorCargaCaja = true;
            Mensaje = $"Error al cargar caja: {ex.Message}";
            ActualizarHeaderOperativo();
        }
        finally
        {
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine($"[PERF][TurnoViewModel] CargarInternoAsync(force={force}): {sw.ElapsedMilliseconds} ms");
        }
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
            ActualizarHeaderOperativo();
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
            ActualizarHeaderOperativo();
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

    [RelayCommand]
    public async Task IrNotificacionesAsync()
    {
        await Shell.Current.GoToAsync("notificacionesPage");
    }

    private void ActualizarHeaderOperativo()
    {
        InicialesUsuario = CrearIniciales(NombreUsuario);
        RolUsuarioHeader = FormatearRol(_session.Rol);
        HeaderKpi1Titulo = "Caja";
        HeaderKpi1Valor = TurnoActivo is null ? "#-" : $"#{TurnoActivo.Idcaja}";
        HeaderKpi2Titulo = "Turno";
        HeaderKpi2Valor = EstadoCajaTexto;
        HeaderKpi3Titulo = "Ventas";
        HeaderKpi3Valor = TotalPagosTurnoTexto;
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);
    }

    private static string CrearIniciales(string nombre)
    {
        var iniciales = string.Concat(nombre
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => p[0].ToString().ToUpperInvariant()));
        return string.IsNullOrWhiteSpace(iniciales) ? "U" : iniciales;
    }

    private static string FormatearRol(string? rol)
        => (rol ?? "USUARIO").ToUpperInvariant() switch
        {
            "CAJERO" => "Cajero",
            "CAMARERO" => "Camarero",
            "COCINA" => "Cocina",
            "ADMIN" => "Administrador",
            _ => "Usuario"
        };
}
