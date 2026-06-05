using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class NotificacionesViewModel : ObservableObject
{
    private readonly NotificationService _notificationService;
    private readonly SessionService _session;
    private bool _escuchandoCambios;

    [ObservableProperty] private ObservableCollection<Notificacion> notificaciones = new();
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int totalNoLeidas;
    [ObservableProperty] private bool hayNotificaciones;
    [ObservableProperty] private string inicialesUsuario = "U";
    [ObservableProperty] private string nombreUsuarioHeader = "Usuario";
    [ObservableProperty] private string rolUsuarioHeader = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Total";
    [ObservableProperty] private string headerKpi1Valor = "0";
    [ObservableProperty] private string headerKpi2Titulo = "No leídas";
    [ObservableProperty] private string headerKpi2Valor = "0";
    [ObservableProperty] private string headerKpi3Titulo = "Leídas";
    [ObservableProperty] private string headerKpi3Valor = "0";

    public NotificacionesViewModel(NotificationService notificationService, SessionService session)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _notificationService = notificationService;
        _session = session;
        ActualizarHeader();
        System.Diagnostics.Debug.WriteLine($"[PERF][NotificacionesViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    // ── Carga desde el store singleton ───────────────────────────────────

    [RelayCommand]
    private void Cargar() => CargarDesdeStore();

    public void Iniciar()
    {
        if (_escuchandoCambios)
            return;

        _notificationService.Cambiaron += OnHistorialCambiado;
        _escuchandoCambios = true;
    }

    private void OnHistorialCambiado() => CargarDesdeStore();

    private void CargarDesdeStore()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // Snapshot thread-safe: Historial solo se modifica en MainThread
        var lista = _notificationService.Historial.ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReemplazarItems(Notificaciones, lista);
            TotalNoLeidas     = Notificaciones.Count(n => !n.Leida);
            HayNotificaciones = Notificaciones.Count > 0;
            ActualizarHeader();
            System.Diagnostics.Debug.WriteLine($"[PERF][NotificacionesViewModel] CargarDesdeStore: {sw.ElapsedMilliseconds} ms");
        });
    }

    // ── Acciones ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void MarcarTodasLeidas()
    {
        foreach (var n in Notificaciones)
            n.Leida = true;

        TotalNoLeidas = 0;
        // Notificacion no implementa cambio por item; reinyectar la misma lista
        // mantiene el ItemsSource y fuerza refresco visual del template.
        ReemplazarItems(Notificaciones, Notificaciones.ToList());
        HayNotificaciones = Notificaciones.Count > 0;
        ActualizarHeader();
    }

    [RelayCommand]
    private void MarcarLeida(Notificacion? notif)
    {
        if (notif is null) return;
        notif.Leida   = true;
        TotalNoLeidas = Notificaciones.Count(n => !n.Leida);
        ActualizarHeader();
    }

    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AbrirAccionAsync(Notificacion? notif)
    {
        if (notif is null) return;
        MarcarLeida(notif);
        if (!string.IsNullOrEmpty(notif.Accion))
            await Shell.Current.GoToAsync(notif.Accion);
    }

    // ── Limpieza ─────────────────────────────────────────────────────────

    /// <summary>Llamar desde OnDisappearing para evitar fugas de memoria.</summary>
    public void Detener()
    {
        if (!_escuchandoCambios)
            return;

        _notificationService.Cambiaron -= OnHistorialCambiado;
        _escuchandoCambios = false;
    }

    private static void ReemplazarItems<T>(ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }

    private void ActualizarHeader()
    {
        NombreUsuarioHeader = _session.NombreCompleto ?? _session.Username ?? "Usuario";
        RolUsuarioHeader = FormatearRol(_session.Rol);
        InicialesUsuario = CrearIniciales(NombreUsuarioHeader);
        HeaderKpi1Titulo = "Total";
        HeaderKpi1Valor = Notificaciones.Count.ToString();
        HeaderKpi2Titulo = "No leídas";
        HeaderKpi2Valor = TotalNoLeidas.ToString();
        HeaderKpi3Titulo = "Leídas";
        HeaderKpi3Valor = Math.Max(0, Notificaciones.Count - TotalNoLeidas).ToString();
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
