using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class NotificacionesViewModel : ObservableObject
{
    private readonly NotificationService _notificationService;
    private bool _escuchandoCambios;

    [ObservableProperty] private ObservableCollection<Notificacion> notificaciones = new();
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int totalNoLeidas;
    [ObservableProperty] private bool hayNotificaciones;

    public NotificacionesViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
        Iniciar();
        CargarDesdeStore();
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
        // Snapshot thread-safe: Historial solo se modifica en MainThread
        var lista = _notificationService.Historial.ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReemplazarItems(Notificaciones, lista);
            TotalNoLeidas     = Notificaciones.Count(n => !n.Leida);
            HayNotificaciones = Notificaciones.Count > 0;
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
    }

    [RelayCommand]
    private void MarcarLeida(Notificacion? notif)
    {
        if (notif is null) return;
        notif.Leida   = true;
        TotalNoLeidas = Notificaciones.Count(n => !n.Leida);
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
}
