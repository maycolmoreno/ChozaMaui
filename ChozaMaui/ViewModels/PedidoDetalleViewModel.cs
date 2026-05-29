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

    [ObservableProperty] private int pedidoId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

    // Datos del pedido
    [ObservableProperty] private string tituloPedido = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeEnviarCocina))]
    [NotifyPropertyChangedFor(nameof(PuedeMarcarListo))]
    [NotifyPropertyChangedFor(nameof(PuedeEntregarCliente))]
    [NotifyPropertyChangedFor(nameof(PuedeCancelarPedido))]
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
    [ObservableProperty] private PedidoResponse? pedidoCompleto;

    [ObservableProperty] private string mensajeCambio = string.Empty;

    public ObservableCollection<PedidoDetalleResponse> Detalles { get; } = [];
    public ObservableCollection<PedidoTimelineItem> Historial { get; } = [];

    public bool PuedeIrAPagar =>
        _capabilities.PuedeCobrarCuenta(_session.Rol);

    public PedidoDetalleViewModel(RoleCapabilityService capabilities, PedidoPresentationService presentation, PosOrderWorkflowService pedidoWorkflow, SessionService session)
    {
        _capabilities = capabilities;
        _presentation = presentation;
        _pedidoWorkflow = pedidoWorkflow;
        _session = session;
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
        if (PedidoId <= 0) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var p = await _pedidoWorkflow.ObtenerPedidoDetalleAsync(PedidoId);
            PedidoCompleto = p;
            AplicarPresentacion(_presentation.BuildDetail(p));

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
        }
    }

    [RelayCommand]
    public async Task CambiarEstadoRapidoAsync(string nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado) || nuevoEstado == Estado)
            return;

        if (nuevoEstado == PedidoEstados.EnCocina && !PuedeEnviarCocina)
        {
            MensajeCambio = "Tu perfil no tiene autorizacion para enviar pedidos a cocina.";
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

    public bool PuedeEnviarCocina =>
        Estado == PedidoEstados.Pendiente &&
        _capabilities.PuedeConfirmarPedido(_session.Rol);
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

}

public class PedidoTimelineItem
{
    public string Hora { get; set; } = "--:-- --";
    public string Evento { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string DotColor { get; set; } = "#d1d5db";
    public bool MostrarLinea { get; set; }
}
