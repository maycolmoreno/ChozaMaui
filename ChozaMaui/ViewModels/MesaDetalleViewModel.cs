using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(Mesa), "Mesa")]
public partial class MesaDetalleViewModel : ObservableObject
{
    private readonly ApiService _api;

    public ObservableCollection<PedidoResponse> PedidosActivos { get; } = [];
    public ObservableCollection<PedidoResponse> PedidosListos { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty] private MesaResponse? mesa;
    [ObservableProperty] private double totalMesa;
    [ObservableProperty] private int pedidosActivosCount;
    [ObservableProperty] private int pedidosListosCount;
    [ObservableProperty] private string tiempoMesaTexto = "0 min";

    public string Titulo => Mesa is null ? "Mesa" : $"Mesa {Mesa.Numero}";
    public string ComedorTexto => Mesa?.NombreComedor ?? "Sin comedor";
    public bool TienePedidosActivos => PedidosActivosCount > 0;
    public bool TienePedidosListos => PedidosListosCount > 0;
    public bool PuedeCerrarMesa => Mesa is not null && !TienePedidosActivos;
    public string TotalMesaTexto => $"${TotalMesa:0.00}";

    public MesaDetalleViewModel(ApiService api) => _api = api;

    partial void OnMesaChanged(MesaResponse? value)
    {
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(ComedorTexto));
        OnPropertyChanged(nameof(PuedeCerrarMesa));
    }

    partial void OnPedidosActivosCountChanged(int value)
    {
        OnPropertyChanged(nameof(TienePedidosActivos));
        OnPropertyChanged(nameof(PuedeCerrarMesa));
    }

    partial void OnPedidosListosCountChanged(int value)
    {
        OnPropertyChanged(nameof(TienePedidosListos));
    }

    partial void OnTotalMesaChanged(double value)
    {
        OnPropertyChanged(nameof(TotalMesaTexto));
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (Mesa is null) return;

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var pedidos = await _api.GetPedidosAsync();
            var activosMesa = pedidos
                .Where(p => p.EsActivo && p.Mesa?.Idmesa == Mesa.Idmesa)
                .OrderBy(p => p.Fecha)
                .ToList();

            PedidosActivos.Clear();
            foreach (var pedido in activosMesa)
                PedidosActivos.Add(pedido);

            PedidosListos.Clear();
            foreach (var pedido in activosMesa.Where(p => p.EstaListoParaEntrega))
                PedidosListos.Add(pedido);

            PedidosActivosCount = PedidosActivos.Count;
            PedidosListosCount = PedidosListos.Count;
            TotalMesa = PedidosActivos.Sum(p => p.Total);
            TiempoMesaTexto = CalcularTiempoMesa(activosMesa);
        }
        catch (Exception ex)
        {
            MensajeEsError = true;
            Mensaje = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task EntregarPedidoAsync(PedidoResponse pedido)
    {
        if (!pedido.PuedeEntregarse)
        {
            MensajeEsError = true;
            Mensaje = "Solo se pueden entregar pedidos en estado LISTO_PARA_ENTREGA.";
            return;
        }

        await CambiarPedidoACompletadoAsync(pedido);
        await CargarAsync();
    }

    [RelayCommand]
    public async Task EntregarTodoAsync()
    {
        if (PedidosListos.Count == 0)
        {
            MensajeEsError = true;
            Mensaje = "No hay pedidos listos para entregar.";
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            foreach (var pedido in PedidosListos.ToList())
                await _api.CambiarEstadoPedidoAsync(pedido.Idpedido, "COMPLETADO");

            MensajeEsError = false;
            Mensaje = "Pedidos listos entregados correctamente.";
        }
        catch (Exception ex)
        {
            MensajeEsError = true;
            Mensaje = $"Error entregando pedidos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        await CargarAsync();
    }

    [RelayCommand]
    public async Task CerrarMesaAsync()
    {
        if (Mesa is null) return;

        if (TienePedidosActivos)
        {
            MensajeEsError = true;
            Mensaje = "Primero entrega o cancela los pedidos activos antes de cerrar la mesa.";
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            Mesa = await _api.ActualizarEstadoMesaAsync(Mesa, true);
            MensajeEsError = false;
            Mensaje = "Mesa cerrada y marcada como disponible.";
        }
        catch (Exception ex)
        {
            MensajeEsError = true;
            Mensaje = $"Error cerrando mesa: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task CambiarPedidoACompletadoAsync(PedidoResponse pedido)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.CambiarEstadoPedidoAsync(pedido.Idpedido, "COMPLETADO");
            MensajeEsError = false;
            Mensaje = $"Pedido #{pedido.Idpedido} entregado.";
        }
        catch (Exception ex)
        {
            MensajeEsError = true;
            Mensaje = $"Error entregando pedido: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string CalcularTiempoMesa(List<PedidoResponse> pedidos)
    {
        if (pedidos.Count == 0) return "0 min";

        var primerPedido = pedidos.Min(p => p.Fecha);
        var diff = DateTime.Now - primerPedido;
        if (diff.TotalHours >= 1)
            return $"{(int)diff.TotalHours}h {diff.Minutes:D2}m";

        return $"{Math.Max(1, (int)diff.TotalMinutes)} min";
    }
}
