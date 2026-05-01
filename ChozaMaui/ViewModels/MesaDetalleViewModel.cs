using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(Mesa), "Mesa")]
public partial class MesaDetalleViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private MesaResponse? mesa;
    [ObservableProperty] private PedidoResponse? pedidoActivo;
    [ObservableProperty] private bool tienePedidoActivo;

    // Helpers derivados de Mesa
    public string Titulo        => Mesa is null ? "Mesa" : $"Mesa {Mesa.Numero}";
    public string ComEdorTexto  => Mesa?.NombreComedor ?? "Sin comedor";
    public string EstadoTexto   => Mesa?.EstadoTexto ?? "";
    public string EstadoColor   => Mesa?.EstadoColor ?? "#9ca3af";
    public string BotonEstadoTexto => Mesa?.Estado == true ? "Marcar como Ocupada" : "Marcar como Disponible";
    public string BotonEstadoColor => Mesa?.Estado == true
        ? "#e94560"   // rojo → ocupar
        : "#28b779";  // verde → liberar

    partial void OnMesaChanged(MesaResponse? value)
    {
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(ComEdorTexto));
        OnPropertyChanged(nameof(EstadoTexto));
        OnPropertyChanged(nameof(EstadoColor));
        OnPropertyChanged(nameof(BotonEstadoTexto));
        OnPropertyChanged(nameof(BotonEstadoColor));
    }

    public MesaDetalleViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (Mesa is null) return;
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var pedidos = await _api.GetPedidosAsync();
            PedidoActivo = pedidos
                .Where(p => p.Mesa?.Idmesa == Mesa.Idmesa &&
                            p.Estado != "CANCELADO" && p.Estado != "CERRADO" && p.Estado != "ENTREGADO")
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefault();
            TienePedidoActivo = PedidoActivo is not null;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task CambiarEstadoAsync()
    {
        if (Mesa is null) return;
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var actualizada = await _api.ActualizarEstadoMesaAsync(Mesa, !Mesa.Estado);
            Mesa = actualizada;
            Mensaje = $"Mesa marcada como {Mesa.EstadoTexto}.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task VerPedidoAsync()
    {
        if (PedidoActivo is null) return;
        await Shell.Current.GoToAsync("pedidodetalle",
            new Dictionary<string, object> { { "Pedido", PedidoActivo } });
    }
}
