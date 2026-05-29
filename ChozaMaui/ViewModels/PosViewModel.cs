using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(MesaSeleccionada), "Mesa")]
[QueryProperty(nameof(PedidoId), "PedidoId")]
public partial class PosViewModel : ObservableObject
{
    private readonly RoleCapabilityService _capabilities;
    private readonly PosCatalogService _catalogService;
    private readonly PosClientService _clientService;
    private readonly PosDataService _dataService;
    private readonly PosDraftService _draftService;
    private readonly PosMediaService _mediaService;
    private readonly PosOrderStateService _orderStateService;
    private readonly PosOrderWorkflowService _posWorkflow;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;
    private PedidoResponse? _ultimoPedidoParaRecibo;
    private bool _cargandoDatos;
    private bool _actualizandoCarrito;
    private DateTimeOffset? _ultimaCargaUtc;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Listas base ────────────────────────────────────────────────────
    public ObservableCollection<MesaResponse> Mesas { get; } = [];
    public ObservableCollection<CategoriaResponse> Categorias { get; } = [];
    public ObservableCollection<ProductoResponse> Productos { get; } = [];
    public ObservableCollection<ProductoResponse> ProductosFiltrados { get; } = [];
    public ObservableCollection<ItemCarrito> Carrito { get; } = [];

    // ── Listas POS split-panel ─────────────────────────────────────────
    public ObservableCollection<MesaVisual> MesasVisuales { get; } = [];
    public ObservableCollection<MesaVisual> MesasFiltradasList { get; } = [];


    // ── Selecciones ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MesaSeleccionadaTexto))]
    [NotifyPropertyChangedFor(nameof(MesaHeaderTexto))]
    [NotifyPropertyChangedFor(nameof(MesaNumeroTexto))]
    [NotifyPropertyChangedFor(nameof(ComedorTexto))]
    private MesaResponse? mesaSeleccionada;

    [ObservableProperty]
    private int pedidoId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoPedidoTexto))]
    [NotifyPropertyChangedFor(nameof(EstadoPedidoColor))]
    [NotifyPropertyChangedFor(nameof(TienePedidoEnCurso))]
    [NotifyPropertyChangedFor(nameof(PuedeMarcarListo))]
    [NotifyPropertyChangedFor(nameof(PuedeEntregarPedido))]
    [NotifyPropertyChangedFor(nameof(MostrarPedidoListo))]
    [NotifyPropertyChangedFor(nameof(PedidoEnCursoTotalTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPedidoActualTexto))]
    [NotifyPropertyChangedFor(nameof(MostrarEstadoVacio))]
    private PedidoResponse? pedidoEnCurso;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTexto))]
    [NotifyPropertyChangedFor(nameof(ClienteNombreTexto))]
    [NotifyPropertyChangedFor(nameof(ClienteEstadoTexto))]
    private ClienteResponse? clienteSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPedidoActualTexto))]
    private double total;

    [ObservableProperty] private string observaciones = string.Empty;
    [ObservableProperty] private string busquedaProducto = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mostrarMensaje;
    [ObservableProperty] private bool mensajeEsError;

    // ── Filtro de mesas ───────────────────────────────────────────────
    [ObservableProperty] private string filtroMesas = "Todas";

    // ── Cámara ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneFoto))]
    private FotoAdjunta? fotoAdjunta;

    public bool TieneFoto => FotoAdjunta is not null;

    public string MesaSeleccionadaTexto =>
        MesaSeleccionada is null ? "Sin mesa" : MesaSeleccionada.Etiqueta;

    public string MesaHeaderTexto =>
        MesaSeleccionada is null ? "Selecciona mesa" : $"Mesa #{MesaSeleccionada.Numero}";

    public string MeseroTexto => _session.NombreCompleto ?? _session.Username ?? "Mesero";

    public string EstadoPedidoTexto =>
        PedidoEnCurso?.EstadoTextoVisual ?? (Carrito.Count > 0 ? "NUEVO PEDIDO" : "SIN PEDIDO");

    public string EstadoPedidoColor =>
        PedidoEnCurso?.EstadoBadgeColor ?? (Carrito.Count > 0 ? "#f59e0b" : "#6b7280");

    public bool TienePedidoEnCurso => PedidoEnCurso is not null;
    public bool PuedeEnviarACocina => _capabilities.PuedeConfirmarPedido(_session.Rol);
    public bool PuedeMarcarListo =>
        (PedidoEnCurso?.Estado is PedidoEstados.EnCocina or PedidoEstados.EnBar) &&
        _capabilities.PuedeMarcarPedidoListo(_session.Rol);
    public bool PuedeEntregarPedido => PedidoEnCurso?.PuedeEntregarse == true && _capabilities.PuedeEntregarPedido(_session.Rol);
    public bool MostrarPedidoListo => PuedeEntregarPedido;
    public string PedidoEnCursoTotalTexto => PedidoEnCurso is null ? "$0.00" : $"${PedidoEnCurso.Total:0.00}";
    public string TotalPedidoActualTexto => PedidoEnCurso is null ? TotalTexto : PedidoEnCursoTotalTexto;
    public bool MostrarEstadoVacio => !TieneItems && !TienePedidoEnCurso;

    public string ClienteTexto =>
        ClienteSeleccionado is null ? "👤 Asignar cliente" : $"👤 {ClienteSeleccionado.Nombre}  ·  {ClienteSeleccionado.Cedula}";

    public string ClienteNombreTexto => ClienteSeleccionado?.Nombre ?? "Sin asignar";
    public string ClienteEstadoTexto => ClienteSeleccionado is null ? "Cliente" : ClienteSeleccionado.Cedula;

    public int TotalItems => Carrito.Sum(i => i.Cantidad);
    public string TotalTexto => $"${Total:0.00}";

    // ── Propiedades para el nuevo diseño ──────────────────────────────
    public string ComedorTexto    => MesaSeleccionada?.NombreComedor ?? "";
    public string MesaNumeroTexto => MesaSeleccionada is null ? "Selecciona mesa" : $"Mesa #{MesaSeleccionada.Numero}";
    public bool   TieneItems      => Carrito.Count > 0;

    [ObservableProperty] private bool mostrarExito;
    [ObservableProperty] private string pedidoEntregadoTotalTexto = "$0.00";

    public PosViewModel(
        RoleCapabilityService capabilities,
        PosCatalogService catalogService,
        PosClientService clientService,
        PosDataService dataService,
        PosDraftService draftService,
        PosMediaService mediaService,
        PosOrderStateService orderStateService,
        SessionService session,
        PosOrderWorkflowService posWorkflow,
        INavigationService navigation)
    {
        _capabilities = capabilities;
        _catalogService = catalogService;
        _clientService = clientService;
        _dataService = dataService;
        _draftService = draftService;
        _mediaService = mediaService;
        _orderStateService = orderStateService;
        _session = session;
        _posWorkflow = posWorkflow;
        _navigation = navigation;

        Carrito.CollectionChanged += OnCarritoCollectionChanged;
    }

    // Llamado automáticamente cuando Shell establece MesaSeleccionada vía QueryProperty
    partial void OnMesaSeleccionadaChanged(MesaResponse? value)
    {
        if (value is not null && MesasVisuales.Count > 0)
            _ = CargarPedidoEnCursoAsync();
    }

    partial void OnPedidoIdChanged(int value)
    {
        if (value > 0 && MesaSeleccionada is not null && MesasVisuales.Count > 0)
            _ = CargarPedidoEnCursoAsync();
    }

    // ── Carga inicial ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        await CargarDatosInternoAsync(force: true);
    }

    public Task CargarSiEsNecesarioAsync()
        => CargarDatosInternoAsync(force: false);

    private async Task CargarDatosInternoAsync(bool force)
    {
        if (_cargandoDatos)
            return;

        var tieneDatosBase = MesasVisuales.Count > 0 && Categorias.Count > 0 && Productos.Count > 0;
        if (!force && tieneDatosBase && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
            return;

        _cargandoDatos = true;
        IsBusy = true;
        try
        {
            var snapshot = await _dataService.CargarDatosAsync();

            ReemplazarItems(Mesas, snapshot.Mesas);
            ReemplazarItems(MesasVisuales, snapshot.MesasVisuales);
            AplicarFiltroMesas();

            ReemplazarItems(Categorias, snapshot.Categorias);

            ReemplazarItems(Productos, snapshot.Productos);
            AplicarBusquedaProductos();

            if (MesaSeleccionada is not null)
                await CargarPedidoEnCursoAsync();

            _ultimaCargaUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando datos: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
            _cargandoDatos = false;
        }
    }

    // ── Filtro por categoría ──────────────────────────────────────────

    [RelayCommand]
    public async Task FiltrarPorCategoriaAsync(CategoriaResponse categoria)
    {
        IsBusy = true;
        try
        {
            var lista = await _catalogService.ObtenerProductosPorCategoriaAsync(categoria.Idcategoria);
            ReemplazarProductos(lista);
            AplicarBusquedaProductos();
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error filtrando: {ex.Message}", error: true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task MostrarTodosAsync()
    {
        IsBusy = true;
        try
        {
            var lista = await _catalogService.ObtenerProductosActivosAsync();
            ReemplazarProductos(lista);
            AplicarBusquedaProductos();
        }
        finally { IsBusy = false; }
    }

    partial void OnBusquedaProductoChanged(string value) => AplicarBusquedaProductos();

    private void AplicarBusquedaProductos()
    {
        var productos = _catalogService.FiltrarProductos(Productos, BusquedaProducto);

        ReemplazarItems(ProductosFiltrados, productos);
    }

    private void ReemplazarProductos(IEnumerable<ProductoResponse> productos)
        => ReemplazarItems(Productos, productos);

    // ── Carrito ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task AgregarAlCarritoAsync(ProductoResponse producto)
    {
        if (producto.StockActual <= 0)
        {
            await MostrarMensajeAsync($"{producto.Nombre} no tiene stock disponible.", error: true);
            return;
        }

        var existente = Carrito.FirstOrDefault(i => i.Producto.Idproducto == producto.Idproducto);
        if (existente is not null)
        {
            if (existente.Cantidad >= producto.StockActual)
            {
                await MostrarMensajeAsync(
                    $"Solo hay {producto.StockActual} unidad(es) disponibles de {producto.Nombre}.",
                    error: true);
                return;
            }

            existente.Cantidad++;
        }
        else
        {
            Carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1 });
        }
        RecalcularTotal();
    }

    [RelayCommand]
    public void QuitarDelCarrito(ItemCarrito item)
    {
        if (item.Cantidad > 1)
        {
            item.Cantidad--;
        }
        else
        {
            Carrito.Remove(item);
        }
        RecalcularTotal();
    }

    [RelayCommand]
    public void LimpiarCarrito()
    {
        Carrito.Clear();
        Total = 0;
        Observaciones = string.Empty;
        FotoAdjunta = null;
    }

    private void RecalcularTotal()
    {
        Total = Carrito.Sum(i => i.Subtotal);
    }

    [RelayCommand]
    public async Task SeleccionarMesaAsync(MesaResponse mesa)
    {
        PedidoId = 0;
        MesaSeleccionada = mesa;
    }

    [RelayCommand]
    public void SeleccionarFiltroMesa(string filtro)
    {
        FiltroMesas = filtro;
        AplicarFiltroMesas();
    }

    [RelayCommand]
    public async Task SeleccionarMesaVisualAsync(MesaVisual mesaVisual)
    {
        PedidoId = 0;
        MesaSeleccionada = mesaVisual.Mesa;
        LimpiarCarrito();
        ClienteSeleccionado = null;
        PedidoEnCurso = null;
    }

    private void AplicarFiltroMesas()
    {
        var userId = _session.UserId;
        IEnumerable<MesaVisual> filtered = FiltroMesas switch
        {
            "Disponibles" => MesasVisuales.Where(m => m.EstadoVisual == "Disponible"),
            "Activas"     => MesasVisuales.Where(m => m.EstadoVisual != "Disponible"),
            "Mis mesas"   => MesasVisuales.Where(m => m.PedidosActivos.Any(p => p.Usuario?.Idusuario == userId)),
            _             => MesasVisuales
        };
        ReemplazarItems(MesasFiltradasList, filtered);
    }

    [RelayCommand]
    public async Task BuscarClienteAsync()
    {
        IsBusy = true;
        try
        {
            var resultado = await _clientService.SeleccionarClienteAsync();
            if (resultado.Cliente is not null)
                ClienteSeleccionado = resultado.Cliente;

            if (!string.IsNullOrWhiteSpace(resultado.Mensaje))
                await MostrarMensajeAsync(resultado.Mensaje, error: true);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando clientes: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void QuitarCliente() => ClienteSeleccionado = null;

    [RelayCommand]
    public async Task CrearClienteRapidoAsync()
    {
        IsBusy = true;
        try
        {
            var resultado = await _clientService.CrearClienteRapidoAsync();
            if (resultado.Cancelado || resultado.Cliente is null)
                return;

            var clienteCreado = resultado.Cliente;
            ClienteSeleccionado = clienteCreado;

            await MostrarMensajeAsync($"Cliente {clienteCreado.Nombre} creado y asignado.", error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error creando cliente: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Cámara (sensor del dispositivo) ──────────────────────────────

    [RelayCommand]
    public async Task TomarFotoAsync()
    {
        try
        {
            var foto = await _mediaService.CapturarFotoAsync();
            if (foto is null)
                return;

            FotoAdjunta = foto;
            await MostrarMensajeAsync("Foto guardada correctamente.", error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error con cámara: {ex.Message}", error: true);
        }
    }

    [RelayCommand]
    public void QuitarFoto()
    {
        FotoAdjunta = null;
    }

    // Elimina completamente un ítem del carrito (botón 🗑)
    [RelayCommand]
    public void EliminarItemCarrito(ItemCarrito item)
    {
        Carrito.Remove(item);
        RecalcularTotal();
    }

    // Volver al mapa de mesas
    [RelayCommand]
    public Task VolverAsync()
        => _navigation.GoToAsync("//mapa");

    [RelayCommand]
    public Task IrMesasAsync()
        => _navigation.GoToAsync("//mapa");

    [RelayCommand]
    public Task IrPedidosAsync()
        => _navigation.GoToAsync("//pedidos");

    [RelayCommand]
    public Task IrCocinaAsync()
        => _navigation.GoToAsync("//pedidos");

    [RelayCommand]
    public Task IrPerfilAsync()
        => _navigation.GoToAsync("//perfil");

    // Ver detalle del pedido en curso
    [RelayCommand]
    public async Task VerDetallePedidoAsync()
    {
        if (PedidoEnCurso is null) return;
        await _navigation.GoToAsync($"pedidodetalle?id={PedidoEnCurso.Idpedido}");
    }

    // Cerrar mesa tras entrega: oculta éxito, limpia y vuelve al mapa
    [RelayCommand]
    public async Task CerrarMesaAsync()
    {
        MostrarExito = false;
        LimpiarCarrito();
        PedidoEnCurso   = null;
        MesaSeleccionada = null;
        await _navigation.GoToAsync("//mapa");
    }

    [RelayCommand]
    public async Task ImprimirCuentaAsync()
    {
        var pedido = PedidoEnCurso ?? _ultimoPedidoParaRecibo;
        if (pedido is null)
        {
            await MostrarMensajeAsync("No hay pedido disponible para imprimir.", error: true);
            return;
        }

        try
        {
            await _mediaService.CompartirReciboAsync(pedido, MeseroTexto);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"No se pudo generar el recibo: {ex.Message}", error: true);
        }
    }

    // ── Enviar pedido ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task EnviarPedidoAsync()
    {
        if (!_capabilities.PuedeCrearPedido(_session.Rol))
        {
            await MostrarMensajeAsync("Tu perfil no tiene autorizacion para crear pedidos.", error: true);
            return;
        }

        var estadoDestino = PedidoEstados.Pendiente;
        var mensajeExito = "Pedido";

        await CrearPedidoAsync(estadoDestino, mensajeExito);
    }

    [RelayCommand]
    public async Task EnviarACocinaAsync()
    {
        if (!PuedeEnviarACocina)
        {
            await MostrarMensajeAsync("Tu perfil no tiene autorizacion para enviar pedidos a cocina.", error: true);
            return;
        }

        await CrearPedidoAsync(PedidoEstados.EnCocina, "Pedido enviado a cocina");
    }

    [RelayCommand]
    public async Task MarcarComoListoAsync()
    {
        if (PedidoEnCurso is null)
        {
            await MostrarMensajeAsync("No hay pedido activo para marcar como listo.", error: true);
            return;
        }

        if (!PuedeMarcarListo)
        {
            await MostrarMensajeAsync("Tu perfil no tiene autorizacion para marcar pedidos como listos.", error: true);
            return;
        }

        await CambiarEstadoPedidoActualAsync(PedidoEstados.ListoParaEntrega, "Pedido marcado como listo.");
    }

    [RelayCommand]
    public async Task EntregarPedidoActualAsync()
    {
        if (PedidoEnCurso is null)
        {
            await MostrarMensajeAsync("No hay pedido activo para entregar.", error: true);
            return;
        }

        if (!PuedeEntregarPedido)
        {
            await MostrarMensajeAsync("Tu perfil no tiene autorizacion para entregar este pedido.", error: true);
            return;
        }

        _ultimoPedidoParaRecibo = PedidoEnCurso;
        PedidoEntregadoTotalTexto = $"${PedidoEnCurso.Total:0.00}";

        await CambiarEstadoPedidoActualAsync(PedidoEstados.Completado, "Pedido entregado al cliente.");
        MostrarExito = true;
    }

    private async Task CrearPedidoAsync(string estadoDestino, string mensajeExito)
    {
        var errorValidacion = _draftService.ValidarPedido(MesaSeleccionada, Carrito, ClienteSeleccionado);
        if (errorValidacion is not null)
        {
            await MostrarMensajeAsync(errorValidacion, error: true);
            return;
        }

        var errorStock = await _posWorkflow.ValidarStockCarritoAsync(Carrito);
        if (errorStock is not null)
        {
            await MostrarMensajeAsync(errorStock, error: true);
            return;
        }

        var request = _draftService.CrearPedidoRequest(
            _session.UserId,
            MesaSeleccionada!,
            ClienteSeleccionado!,
            Carrito,
            Observaciones,
            FotoAdjunta);

        IsBusy = true;
        try
        {
            var resultado = await _posWorkflow.SubmitPedidoAsync(request, estadoDestino);

            if (resultado.SeEncoloOffline)
            {
                LimpiarCarrito();
                PedidoEnCurso = null;
                ClienteSeleccionado = null;
                await MostrarMensajeAsync($"Sin conexion. Pedido guardado localmente. Pendientes: {resultado.Pendientes}.", error: false);
                return;
            }

            var pedido = resultado.Pedido;
            if (pedido is null)
                throw new InvalidOperationException("No se pudo confirmar el pedido enviado.");

            LimpiarCarrito();
            PedidoEnCurso = pedido;
            ClienteSeleccionado = null;
            await CargarDatosAsync();
            var mensaje = string.IsNullOrWhiteSpace(resultado.VinculoCuentaAdvertencia)
                ? $"{mensajeExito} #{pedido.Idpedido}."
                : $"{mensajeExito} #{pedido.Idpedido}. {resultado.VinculoCuentaAdvertencia}";
            await MostrarMensajeAsync(mensaje, error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error al enviar pedido: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task MostrarMensajeAsync(string texto, bool error)
    {
        MensajeEsError = error;
        Mensaje = texto;
        MostrarMensaje = true;
        await Task.Delay(3000);
        MostrarMensaje = false;
        Mensaje = string.Empty;
    }

    private async Task CargarPedidoEnCursoAsync()
    {
        if (MesaSeleccionada is null)
        {
            LimpiarPedidoActual();
            return;
        }

        try
        {
            var pedidoCompleto = await _posWorkflow.ObtenerPedidoEnCursoAsync(MesaSeleccionada, PedidoId);
            SincronizarPedidoActual(pedidoCompleto);
        }
        catch
        {
            LimpiarPedidoActual();
        }
    }

    private async Task CambiarEstadoPedidoActualAsync(string estado, string mensajeExito)
    {
        if (PedidoEnCurso is null) return;

        IsBusy = true;
        try
        {
            PedidoEnCurso = await _posWorkflow.CambiarEstadoPedidoAsync(PedidoEnCurso, estado);
            await CargarDatosAsync();
            await MostrarMensajeAsync(mensajeExito, error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cambiando estado: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SincronizarPedidoActual(PedidoResponse? pedido)
    {
        AplicarEstadoPedido(_orderStateService.CrearSnapshot(pedido));
    }

    private void AplicarEstadoPedido(PosOrderStateSnapshot snapshot)
    {
        PedidoEnCurso = snapshot.Pedido;
        PedidoId = snapshot.PedidoId;

        _actualizandoCarrito = true;
        try
        {
            Carrito.Clear();
            foreach (var item in snapshot.Carrito)
                Carrito.Add(item);
        }
        finally
        {
            _actualizandoCarrito = false;
        }

        NotificarResumenCarritoBase();

        ClienteSeleccionado = snapshot.Cliente;
        Observaciones = snapshot.Observaciones;
        Total = snapshot.Total;
    }

    private void LimpiarPedidoActual()
    {
        AplicarEstadoPedido(_orderStateService.CrearSnapshotVacio());
    }

    private void OnCarritoCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ItemCarrito item in e.OldItems)
                item.PropertyChanged -= OnCarritoItemPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (ItemCarrito item in e.NewItems)
                item.PropertyChanged += OnCarritoItemPropertyChanged;
        }

        if (_actualizandoCarrito)
            return;

        NotificarResumenCarritoCompleto();
    }

    private void OnCarritoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_actualizandoCarrito)
            return;

        if (e.PropertyName is nameof(ItemCarrito.Cantidad)
            or nameof(ItemCarrito.Producto)
            or nameof(ItemCarrito.Subtotal))
        {
            NotificarResumenCarritoCompleto();
        }
    }

    private static void ReemplazarItems<T>(ObservableCollection<T> destino, IEnumerable<T> items)
    {
        destino.Clear();
        foreach (var item in items)
            destino.Add(item);
    }

    private void NotificarResumenCarritoBase()
    {
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TieneItems));
        OnPropertyChanged(nameof(MostrarEstadoVacio));
    }

    private void NotificarResumenCarritoCompleto()
    {
        NotificarResumenCarritoBase();
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
    }
}
