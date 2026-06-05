using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(PedidoId), "id")]
public partial class PedidoDetalleViewModel : ObservableObject
{
    private readonly RoleCapabilityService _capabilities;
    private readonly PedidoPresentationService _presentation;
    private readonly PosOrderWorkflowService _pedidoWorkflow;
    private readonly SessionService _session;
    private readonly NotificationService _notifications;

    [ObservableProperty] private int pedidoId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string inicialesUsuario = "U";
    [ObservableProperty] private string nombreUsuarioHeader = "Usuario";
    [ObservableProperty] private string rolUsuarioHeader = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Mesa";
    [ObservableProperty] private string headerKpi1Valor = "-";
    [ObservableProperty] private string headerKpi2Titulo = "Estado";
    [ObservableProperty] private string headerKpi2Valor = "-";
    [ObservableProperty] private string headerKpi3Titulo = "Total";
    [ObservableProperty] private string headerKpi3Valor = "$0.00";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;

    // Datos del pedido
    [ObservableProperty] private string tituloPedido = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeEnviarCocina))]
    [NotifyPropertyChangedFor(nameof(PuedeIniciarPreparacion))]
    [NotifyPropertyChangedFor(nameof(PuedeMarcarListo))]
    [NotifyPropertyChangedFor(nameof(PuedeEntregarCliente))]
    [NotifyPropertyChangedFor(nameof(PuedeCancelarPedido))]
    [NotifyPropertyChangedFor(nameof(PuedeIrAPagar))]
    private string estado = string.Empty;
    [ObservableProperty] private string estadoColor = "#6b7280";
    [ObservableProperty] private string estadoBadgeTexto = string.Empty;
    [ObservableProperty] private string fechaTexto = string.Empty;
    [ObservableProperty] private string subtituloPedido = string.Empty;
    [ObservableProperty] private string mesaTexto = string.Empty;
    [ObservableProperty] private string mesaCapacidadTexto = "-";
    [ObservableProperty] private string clienteTexto = string.Empty;
    [ObservableProperty] private string clienteTelefonoTexto = "-";
    [ObservableProperty] private string meseroTexto = "Sin asignar";
    [ObservableProperty] private string meseroHoraTexto = "-";
    [ObservableProperty] private string observaciones = string.Empty;
    [ObservableProperty] private double total;
    [ObservableProperty] private double subtotal;
    [ObservableProperty] private double impuestos;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeIrAPagar))]
    private PedidoResponse? pedidoCompleto;

    [ObservableProperty] private string mensajeCambio = string.Empty;

    public ObservableCollection<PedidoDetalleResponse> Detalles { get; } = [];
    public ObservableCollection<PedidoTimelineItem> Historial { get; } = [];

    public bool PuedeIrAPagar =>
        _capabilities.PuedeCobrarCuenta(_session.Rol)
        && (Estado is PedidoEstados.Completado or PedidoEstados.Entregado);
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;

    public PedidoDetalleViewModel(RoleCapabilityService capabilities, PedidoPresentationService presentation, PosOrderWorkflowService pedidoWorkflow, SessionService session, NotificationService notifications)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _capabilities = capabilities;
        _presentation = presentation;
        _pedidoWorkflow = pedidoWorkflow;
        _session = session;
        _notifications = notifications;
        ActualizarHeaderOperativo();
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidoDetalleViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    partial void OnPedidoIdChanged(int value)
    {
        if (value > 0)
            MainThread.BeginInvokeOnMainThread(async () => await CargarAsync());
    }

    partial void OnEstadoChanged(string value)
    {
        EstadoBadgeTexto = _presentation.MapearEstadoVisual(value);
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (PedidoId <= 0) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var p = await _pedidoWorkflow.ObtenerPedidoDetalleAsync(PedidoId);
            PedidoCompleto = p;
            AplicarPresentacion(_presentation.BuildDetail(p));
            await CargarHistorialRealAsync();
            ActualizarHeaderOperativo();

            Detalles.Clear();
            foreach (var d in p.Detalle ?? [])
                Detalles.Add(d);

        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cargando detalle: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine($"[PERF][PedidoDetalleViewModel] CargarAsync pedido {PedidoId}: {sw.ElapsedMilliseconds} ms");
        }
    }

    [RelayCommand]
    public async Task CambiarEstadoRapidoAsync(string nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado) || nuevoEstado == Estado)
            return;

        if (nuevoEstado == PedidoEstados.EnCocina && !PuedeEnviarCocina && !PuedeIniciarPreparacion)
        {
            MensajeCambio = "Tu perfil no tiene autorizacion para pasar este pedido a preparacion.";
            return;
        }

        if (nuevoEstado == PedidoEstados.ListoParaEntrega && !PuedeMarcarListo)
        {
            MensajeCambio = "Solo cocina o admin pueden despachar pedidos de cocina.";
            return;
        }

        IsBusy = true;
        MensajeCambio = string.Empty;
        try
        {
            var actualizado = await _pedidoWorkflow.CambiarEstadoPedidoAsync(PedidoId, nuevoEstado);
            PedidoCompleto = actualizado;
            AplicarPresentacion(_presentation.BuildDetail(actualizado));
            await CargarHistorialRealAsync();
            ActualizarHeaderOperativo();
            MensajeCambio = $"Estado actualizado: {EstadoBadgeTexto}";
        }
        catch (Exception ex)
        {
            MensajeCambio = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CancelarPedidoAsync()
    {
        await CambiarEstadoRapidoAsync(PedidoEstados.Cancelado);
    }

    [RelayCommand]
    public async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task IrAPagarAsync()
    {
        if (PedidoCompleto is null) return;
        await Shell.Current.GoToAsync("pago",
            new Dictionary<string, object> { { "Pedido", PedidoCompleto } });
    }

    [RelayCommand]
    public async Task IrNotificacionesAsync()
    {
        await Shell.Current.GoToAsync("notificacionesPage");
    }

    public bool PuedeEnviarCocina =>
        Estado == PedidoEstados.Pendiente &&
        _capabilities.PuedeConfirmarPedido(_session.Rol);
    public bool PuedeIniciarPreparacion =>
        Estado == PedidoEstados.Pendiente &&
        _capabilities.PuedeIniciarPreparacion(_session.Rol);
    public bool PuedeMarcarListo =>
        (Estado is PedidoEstados.EnCocina or PedidoEstados.EnBar or PedidoEstados.EnProceso) &&
        _capabilities.PuedeMarcarPedidoListo(_session.Rol);
    public bool PuedeEntregarCliente =>
        (Estado is PedidoEstados.Listo or PedidoEstados.ListoParaEntrega) &&
        _capabilities.PuedeEntregarPedido(_session.Rol);
    public bool PuedeCancelarPedido =>
        Estado is not (PedidoEstados.Completado or PedidoEstados.Entregado or PedidoEstados.Cancelado) && _capabilities.PuedeCancelarPedido(_session.Rol);

    private void AplicarPresentacion(PedidoDetailPresentationModel model)
    {
        TituloPedido = model.TituloPedido;
        SubtituloPedido = model.SubtituloPedido;
        Estado = model.Estado;
        EstadoColor = model.EstadoColor;
        EstadoBadgeTexto = model.EstadoBadgeTexto;
        FechaTexto = model.FechaTexto;
        MesaTexto = model.MesaTexto;
        MesaCapacidadTexto = model.MesaCapacidadTexto;
        ClienteTexto = model.ClienteTexto;
        ClienteTelefonoTexto = model.ClienteTelefonoTexto;
        MeseroTexto = model.MeseroTexto;
        MeseroHoraTexto = model.MeseroHoraTexto;
        Observaciones = model.Observaciones;
        Total = model.Total;
        Subtotal = model.Subtotal;
        Impuestos = model.Impuestos;

        Historial.Clear();
        foreach (var item in model.Historial)
            Historial.Add(item);
    }

    private async Task CargarHistorialRealAsync()
    {
        List<PedidoHistorialResponse> registros;
        try
        {
            registros = await _pedidoWorkflow.ObtenerHistorialPedidoAsync(PedidoId);
        }
        catch
        {
            return;
        }

        if (registros.Count == 0)
            return;

        Historial.Clear();
        for (var i = 0; i < registros.Count; i++)
        {
            var registro = registros[i];
            Historial.Add(new PedidoTimelineItem
            {
                Hora = registro.Fecha.ToString("HH:mm"),
                FechaTexto = registro.Fecha.ToString("dd/MM/yyyy"),
                Evento = registro.Accion,
                Responsable = FormatearResponsable(registro),
                EstadoCambioTexto = FormatearCambioEstado(registro),
                Observacion = registro.Observacion ?? string.Empty,
                DotColor = ColorPorAccion(registro),
                MostrarLinea = i < registros.Count - 1
            });
        }
    }

    private static string FormatearResponsable(PedidoHistorialResponse registro)
    {
        var nombre = string.IsNullOrWhiteSpace(registro.UsuarioNombre) ? "SISTEMA" : registro.UsuarioNombre;
        var rol = string.IsNullOrWhiteSpace(registro.UsuarioRol) ? "SISTEMA" : FormatearRol(registro.UsuarioRol);
        return $"{nombre} · {rol}";
    }

    private static string FormatearCambioEstado(PedidoHistorialResponse registro)
    {
        if (string.IsNullOrWhiteSpace(registro.EstadoAnterior) && string.IsNullOrWhiteSpace(registro.EstadoNuevo))
            return string.Empty;
        if (string.Equals(registro.EstadoAnterior, registro.EstadoNuevo, StringComparison.OrdinalIgnoreCase))
            return registro.EstadoNuevo ?? string.Empty;
        return $"{registro.EstadoAnterior ?? "Sin estado"} -> {registro.EstadoNuevo ?? "Sin estado"}";
    }

    private static string ColorPorAccion(PedidoHistorialResponse registro)
    {
        var accion = registro.Accion.ToUpperInvariant();
        if (accion.Contains("COCINA") || accion.Contains("PREPARACION")) return "#2563EB";
        if (accion.Contains("LISTO")) return "#7C3AED";
        if (accion.Contains("ENTREGADO") || accion.Contains("COBRADA") || accion.Contains("CERRADA")) return "#16A34A";
        if (accion.Contains("CANCELADO")) return "#DC2626";
        return "#EA580C";
    }

    private void ActualizarHeaderOperativo()
    {
        NombreUsuarioHeader = _session.NombreCompleto ?? _session.Username ?? "Usuario";
        RolUsuarioHeader = FormatearRol(_session.Rol);
        InicialesUsuario = CrearIniciales(NombreUsuarioHeader);
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);

        HeaderKpi1Titulo = "Mesa";
        HeaderKpi1Valor = string.IsNullOrWhiteSpace(MesaTexto) ? "-" : MesaTexto.Replace("Mesa", "#").Trim();
        HeaderKpi2Titulo = "Estado";
        HeaderKpi2Valor = string.IsNullOrWhiteSpace(EstadoBadgeTexto) ? "-" : EstadoBadgeTexto;
        HeaderKpi3Titulo = "Total";
        HeaderKpi3Valor = $"${Total:0.00}";
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
            "ROLE_CAJERO" => "Cajero",
            "CAJERO" => "Cajero",
            "ROLE_CAMARERO" => "Camarero",
            "CAMARERO" => "Camarero",
            "ROLE_COCINA" => "Cocina",
            "COCINA" => "Cocina",
            "ROLE_ADMIN" => "Administrador",
            "ADMIN" => "Administrador",
            _ => "Usuario"
        };

}

public class PedidoTimelineItem
{
    public string Hora { get; set; } = "--:-- --";
    public string FechaTexto { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string EstadoCambioTexto { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public string DotColor { get; set; } = "#d1d5db";
    public bool MostrarLinea { get; set; }
}
