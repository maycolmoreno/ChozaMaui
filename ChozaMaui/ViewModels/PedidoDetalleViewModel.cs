using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(PedidoId), "id")]
public partial class PedidoDetalleViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    [ObservableProperty] private int pedidoId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

    // Datos del pedido
    [ObservableProperty] private string tituloPedido = string.Empty;
    [ObservableProperty] private string estado = string.Empty;
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
    [ObservableProperty] private PedidoResponse? pedidoCompleto;

    [ObservableProperty] private string mensajeCambio = string.Empty;

    public const double TasaImpuesto = 0.12;

    public ObservableCollection<PedidoDetalleResponse> Detalles { get; } = [];
    public ObservableCollection<PedidoTimelineItem> Historial { get; } = [];

    public bool PuedeIrAPagar =>
        !string.Equals(_session.Rol, "CAMARERO", StringComparison.OrdinalIgnoreCase);

    public PedidoDetalleViewModel(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    partial void OnPedidoIdChanged(int value)
    {
        if (value > 0)
            MainThread.BeginInvokeOnMainThread(async () => await CargarAsync());
    }

    partial void OnEstadoChanged(string value)
    {
        EstadoBadgeTexto = MapearEstadoVisual(value);
        NotificarEstadoAcciones();
        ConstruirHistorialVisual();
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (PedidoId <= 0) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var p = await _api.GetPedidoPorIdAsync(PedidoId);
            TituloPedido = $"Pedido #{p.Idpedido}";
            SubtituloPedido = $"{p.Mesa?.Etiqueta ?? "Mesa —"}" +
                              $"  ·  {(string.IsNullOrWhiteSpace(p.Mesa?.NombreComedor) ? "Comedor" : p.Mesa.NombreComedor)}";
            Estado = p.Estado;
            EstadoColor = p.EstadoBadgeColor;
            EstadoBadgeTexto = MapearEstadoVisual(p.Estado);
            FechaTexto = p.Fecha.ToString("dd/MM/yyyy HH:mm");
            MesaTexto = p.Mesa?.Etiqueta ?? "—";
            MesaCapacidadTexto = p.Mesa is null ? "—" : $"{p.Mesa.Capacidad} personas";
            ClienteTexto = p.Cliente?.NombreCompleto ?? "Sin cliente";
            ClienteTelefonoTexto = string.IsNullOrWhiteSpace(p.Cliente?.Telefono) ? "Sin teléfono" : p.Cliente.Telefono!;
            MeseroTexto = string.IsNullOrWhiteSpace(p.Usuario?.NombreCompleto) ? "Sin asignar" : p.Usuario.NombreCompleto;
            MeseroHoraTexto = p.Fecha.ToString("hh:mm tt");
            Observaciones = p.Observaciones ?? "Sin observaciones";
            Total = p.Total;
            Subtotal = Math.Round(Total / (1 + TasaImpuesto), 2);
            Impuestos = Math.Round(Total - Subtotal, 2);
            PedidoCompleto = p;

            Detalles.Clear();
            foreach (var d in p.Detalle ?? [])
                Detalles.Add(d);

            ConstruirHistorialVisual();
            NotificarEstadoAcciones();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cargando detalle: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CambiarEstadoRapidoAsync(string nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado) || nuevoEstado == Estado)
            return;

        IsBusy = true;
        MensajeCambio = string.Empty;
        try
        {
            var actualizado = await _api.CambiarEstadoPedidoAsync(PedidoId, nuevoEstado);
            Estado = actualizado.Estado;
            EstadoColor = actualizado.EstadoBadgeColor;
            EstadoBadgeTexto = MapearEstadoVisual(Estado);
            PedidoCompleto = actualizado;
            Total = actualizado.Total;
            Subtotal = Math.Round(Total / (1 + TasaImpuesto), 2);
            Impuestos = Math.Round(Total - Subtotal, 2);
            MensajeCambio = $"Estado actualizado: {EstadoBadgeTexto}";
            ConstruirHistorialVisual();
            NotificarEstadoAcciones();
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
        await CambiarEstadoRapidoAsync("CANCELADO");
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

    public bool PuedeEnviarCocina => Estado == "PENDIENTE";
    public bool PuedeMarcarListo => Estado is "EN_COCINA" or "EN_BAR" or "EN_PROCESO";
    public bool PuedeEntregarCliente => Estado is "LISTO" or "LISTO_PARA_ENTREGA";
    public bool PuedeCancelarPedido => Estado is not ("COMPLETADO" or "ENTREGADO" or "CANCELADO");

    private static string MapearEstadoVisual(string estado) => estado switch
    {
        "PENDIENTE" => "PENDIENTE",
        "EN_COCINA" or "EN_BAR" or "EN_PROCESO" => "EN PREPARACION",
        "LISTO" or "LISTO_PARA_ENTREGA" => "LISTO PARA ENTREGA",
        "COMPLETADO" or "ENTREGADO" => "ENTREGADO",
        "CANCELADO" => "CANCELADO",
        _ => estado
    };

    private void NotificarEstadoAcciones()
    {
        OnPropertyChanged(nameof(PuedeEnviarCocina));
        OnPropertyChanged(nameof(PuedeMarcarListo));
        OnPropertyChanged(nameof(PuedeEntregarCliente));
        OnPropertyChanged(nameof(PuedeCancelarPedido));
    }

    private void ConstruirHistorialVisual()
    {
        Historial.Clear();

        var hora = string.IsNullOrWhiteSpace(MeseroHoraTexto) ? "--:-- --" : MeseroHoraTexto;
        var responsable = string.IsNullOrWhiteSpace(MeseroTexto) ? "Sin asignar" : MeseroTexto;

        Historial.Add(new PedidoTimelineItem
        {
            Hora = hora,
            Evento = "Pedido creado",
            Responsable = responsable,
            DotColor = "#f59e0b",
            MostrarLinea = true
        });

        var enCocina = Estado is not "PENDIENTE" and not "CANCELADO";
        Historial.Add(new PedidoTimelineItem
        {
            Hora = enCocina ? hora : "--:-- --",
            Evento = "Enviado a cocina",
            Responsable = enCocina ? responsable : string.Empty,
            DotColor = enCocina ? "#3b82f6" : "#d1d5db",
            MostrarLinea = true
        });

        var listo = Estado is "LISTO" or "LISTO_PARA_ENTREGA" or "COMPLETADO" or "ENTREGADO";
        Historial.Add(new PedidoTimelineItem
        {
            Hora = listo ? hora : "--:-- --",
            Evento = "Listo para entregar",
            Responsable = listo ? responsable : string.Empty,
            DotColor = listo ? "#f59e0b" : "#d1d5db",
            MostrarLinea = true
        });

        var entregado = Estado is "COMPLETADO" or "ENTREGADO";
        Historial.Add(new PedidoTimelineItem
        {
            Hora = entregado ? hora : "--:-- --",
            Evento = "Entregado al cliente",
            Responsable = entregado ? responsable : string.Empty,
            DotColor = entregado ? "#10b981" : "#d1d5db",
            MostrarLinea = false
        });
    }
}

public class PedidoTimelineItem
{
    public string Hora { get; set; } = "--:-- --";
    public string Evento { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string DotColor { get; set; } = "#d1d5db";
    public bool MostrarLinea { get; set; }
}
