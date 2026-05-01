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
    [ObservableProperty] private string fechaTexto = string.Empty;
    [ObservableProperty] private string mesaTexto = string.Empty;
    [ObservableProperty] private string clienteTexto = string.Empty;
    [ObservableProperty] private string observaciones = string.Empty;
    [ObservableProperty] private double total;
    [ObservableProperty] private PedidoResponse? pedidoCompleto;

    // Cambio de estado
    [ObservableProperty] private string estadoSeleccionado = string.Empty;
    [ObservableProperty] private string mensajeCambio = string.Empty;

    public List<string> EstadosDisponibles { get; } =
        ["PENDIENTE", "EN_PROCESO", "LISTO", "ENTREGADO", "CANCELADO"];

    public ObservableCollection<PedidoDetalleResponse> Detalles { get; } = [];

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
            Estado = p.Estado;
            EstadoColor = p.EstadoBadgeColor;
            FechaTexto = p.Fecha.ToString("dd/MM/yyyy HH:mm");
            MesaTexto = p.Mesa?.Etiqueta ?? "—";
            ClienteTexto = p.Cliente?.NombreCompleto ?? "—";
            Observaciones = p.Observaciones ?? "Sin observaciones";
            Total = p.Total;
            PedidoCompleto = p;

            EstadoSeleccionado = p.Estado;

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
    public async Task CambiarEstadoAsync()
    {
        if (string.IsNullOrWhiteSpace(EstadoSeleccionado) || EstadoSeleccionado == Estado)
            return;

        IsBusy = true;
        MensajeCambio = string.Empty;
        try
        {
            var actualizado = await _api.CambiarEstadoPedidoAsync(PedidoId, EstadoSeleccionado);
            Estado = actualizado.Estado;
            EstadoColor = actualizado.EstadoBadgeColor;
            MensajeCambio = $"Estado actualizado a {Estado}.";
        }
        catch (Exception ex)
        {
            MensajeCambio = $"Error: {ex.Message}";
            EstadoSeleccionado = Estado; // revertir selector
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task IrAPagarAsync()
    {
        if (PedidoCompleto is null) return;
        await Shell.Current.GoToAsync("pago",
            new Dictionary<string, object> { { "Pedido", PedidoCompleto } });
    }
}
