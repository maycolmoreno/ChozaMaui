using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(Mesa), "Mesa")]
public partial class MesaDetalleViewModel : ObservableObject
{
    private readonly MesaDetailWorkflowService _workflow;
    private readonly SessionService _session;
    private DateTimeOffset? _ultimaCargaUtc;
    private int? _mesaCargadaId;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    public ObservableCollection<PedidoResponse> PedidosActivos { get; } = [];
    public ObservableCollection<PedidoResponse> PedidosListos { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titulo))]
    [NotifyPropertyChangedFor(nameof(ComedorTexto))]
    [NotifyPropertyChangedFor(nameof(PuedeCerrarMesa))]
    private MesaResponse? mesa;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalMesaTexto))]
    private double totalMesa;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePedidosActivos))]
    [NotifyPropertyChangedFor(nameof(PuedeCerrarMesa))]
    private int pedidosActivosCount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePedidosListos))]
    private int pedidosListosCount;
    [ObservableProperty] private string tiempoMesaTexto = "0 min";

    public string Titulo => Mesa is null ? "Mesa" : $"Mesa {Mesa.Numero}";
    public string ComedorTexto => Mesa?.NombreComedor ?? "Sin comedor";
    public bool TienePedidosActivos => PedidosActivosCount > 0;
    public bool TienePedidosListos => PedidosListosCount > 0;
    public bool EsAdmin => _session.Rol == "ADMIN";
    public bool PuedeCerrarMesa => Mesa is not null && !TienePedidosActivos && EsAdmin;
    public string TotalMesaTexto => $"${TotalMesa:0.00}";

    public MesaDetalleViewModel(MesaDetailWorkflowService workflow, SessionService session)
    {
        _workflow = workflow;
        _session = session;
    }

    partial void OnMesaChanged(MesaResponse? value)
    {
        _ultimaCargaUtc = null;
        _mesaCargadaId = null;
    }

    partial void OnPedidosActivosCountChanged(int value)
    {
    }

    partial void OnPedidosListosCountChanged(int value)
    {
    }

    partial void OnTotalMesaChanged(double value)
    {
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        await CargarInternoAsync(force: true);
    }

    public Task CargarSiEsNecesarioAsync()
        => CargarInternoAsync(force: false);

    private async Task CargarInternoAsync(bool force)
    {
        if (Mesa is null) return;

        var cambioDeMesa = _mesaCargadaId != Mesa.Idmesa;
        if (!force && !cambioDeMesa && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
            return;

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var snapshot = await _workflow.CargarAsync(Mesa);

            PedidosActivos.Clear();
            foreach (var pedido in snapshot.PedidosActivos)
                PedidosActivos.Add(pedido);

            PedidosListos.Clear();
            foreach (var pedido in snapshot.PedidosListos)
                PedidosListos.Add(pedido);

            PedidosActivosCount = PedidosActivos.Count;
            PedidosListosCount = PedidosListos.Count;
            TotalMesa = snapshot.TotalMesa;
            TiempoMesaTexto = snapshot.TiempoMesaTexto;
            _mesaCargadaId = Mesa.Idmesa;
            _ultimaCargaUtc = DateTimeOffset.UtcNow;
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
            await _workflow.EntregarPedidosAsync(PedidosListos.ToList());

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
            Mesa = await _workflow.CerrarMesaAsync(Mesa);
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
            await _workflow.EntregarPedidoAsync(pedido);
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

}
