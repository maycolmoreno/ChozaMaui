using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
    private readonly PosDataService _dataService;
    private readonly PosDraftService _draftService;
    private readonly PosMediaService _mediaService;
    private readonly PosOrderStateService _orderStateService;
    private readonly PosOrderWorkflowService _posWorkflow;
    private readonly ClienteApiService _clientesApi;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;
    private readonly NotificationService _notifications;
    private PedidoResponse? _ultimoPedidoParaRecibo;
    private bool _cargandoDatos;
    private bool _actualizandoCarrito;
    private DateTimeOffset? _ultimaCargaUtc;
    private DateTimeOffset? _ultimaCargaClientesUtc;
    private int? _categoriaSeleccionadaId;
    private CancellationTokenSource? _busquedaProductoCts;
    private CancellationTokenSource? _imagenesProductosCts;
    private IReadOnlyList<ProductoResponse> _productosFiltradosCache = [];
    private int _productosMostrados;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan VentanaCacheClientes = TimeSpan.FromMinutes(3);
    private const int ProductosPageSize = 36;

    // ── Listas base ────────────────────────────────────────────────────
    public ObservableCollection<MesaResponse> Mesas { get; } = [];
    public ObservableCollection<CategoriaResponse> Categorias { get; } = [];
    public ObservableCollection<ProductoResponse> Productos { get; } = [];
    public ObservableCollection<ProductoResponse> ProductosFiltrados { get; } = [];
    public ObservableCollection<ItemCarrito> Carrito { get; } = [];
    public ObservableCollection<ClienteResponse> ClientesEncontrados { get; } = [];
    private List<ClienteResponse> _clientesDisponibles = [];

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
    [NotifyPropertyChangedFor(nameof(PuedeEnviarACocina))]
    [NotifyPropertyChangedFor(nameof(PuedeEjecutarEnviarACocina))]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarBorrador))]
    [NotifyPropertyChangedFor(nameof(PuedeMarcarListo))]
    [NotifyPropertyChangedFor(nameof(MostrarPedidoEnPreparacion))]
    [NotifyPropertyChangedFor(nameof(PuedeEntregarPedido))]
    [NotifyPropertyChangedFor(nameof(MostrarPedidoListo))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrarCuenta))]
    [NotifyPropertyChangedFor(nameof(PuedeCerrarMesa))]
    [NotifyPropertyChangedFor(nameof(PedidoEnCursoTotalTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPedidoActualTexto))]
    [NotifyPropertyChangedFor(nameof(MostrarEstadoVacio))]
    private PedidoResponse? pedidoEnCurso;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTexto))]
    [NotifyPropertyChangedFor(nameof(ClienteNombreTexto))]
    [NotifyPropertyChangedFor(nameof(ClienteEstadoTexto))]
    [NotifyPropertyChangedFor(nameof(TieneClienteSeleccionado))]
    private ClienteResponse? clienteSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPedidoActualTexto))]
    private double total;

    [ObservableProperty] private string observaciones = string.Empty;
    [ObservableProperty] private string busquedaProducto = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeEjecutarEnviarACocina))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeEjecutarEnviarACocina))]
    [NotifyPropertyChangedFor(nameof(EnviarCocinaTexto))]
    private bool isEnviandoACocina;

    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mostrarMensaje;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty] private bool mostrarPedidoSheet;
    [ObservableProperty] private bool mostrarClienteSheet;
    [ObservableProperty] private bool mostrandoCrearCliente;
    [ObservableProperty] private bool isBuscandoClientes;
    [ObservableProperty] private bool imagenesProductosHabilitadas;
    [ObservableProperty] private string busquedaCliente = string.Empty;
    [ObservableProperty] private string clienteNuevoNombre = string.Empty;
    [ObservableProperty] private string clienteNuevoCedula = string.Empty;
    [ObservableProperty] private string clienteNuevoTelefono = string.Empty;
    [ObservableProperty] private string clienteNuevoEmail = string.Empty;
    [ObservableProperty] private string clienteSheetMensaje = string.Empty;
    [ObservableProperty] private string inicialesUsuario = "U";
    [ObservableProperty] private string nombreUsuarioHeader = "Usuario";
    [ObservableProperty] private string rolUsuarioHeader = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Mesa";
    [ObservableProperty] private string headerKpi1Valor = "-";
    [ObservableProperty] private string headerKpi2Titulo = "Estado";
    [ObservableProperty] private string headerKpi2Valor = "Sin pedido";
    [ObservableProperty] private string headerKpi3Titulo = "Total";
    [ObservableProperty] private string headerKpi3Valor = "$0.00";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;

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
        PedidoEnCurso?.EstadoBadgeColor ?? (Carrito.Count > 0 ? "#EA580C" : "#64748B");

    public bool TienePedidoEnCurso => PedidoEnCurso is not null;
    public bool PuedeGuardarBorrador =>
        TieneItems &&
        PedidoEnCurso is null &&
        _capabilities.PuedeCrearPedido(_session.Rol);

    public bool PuedeEnviarACocina =>
        TieneItems &&
        EsPedidoBorradorOEnEdicion &&
        _capabilities.PuedeConfirmarPedido(_session.Rol);

    public bool PuedeEjecutarEnviarACocina => PuedeEnviarACocina && !IsBusy && !IsEnviandoACocina;
    public string EnviarCocinaTexto => IsEnviandoACocina ? "ENVIANDO..." : "ENVIAR A COCINA";

    public bool PuedeMarcarListo =>
        (PedidoEnCurso?.Estado is PedidoEstados.EnCocina or PedidoEstados.EnBar) &&
        _capabilities.PuedeMarcarPedidoListo(_session.Rol);

    public bool MostrarPedidoEnPreparacion => PedidoEnCurso?.EstaEnPreparacion == true;
    public bool PuedeEntregarPedido => PedidoEnCurso?.PuedeEntregarse == true && _capabilities.PuedeEntregarPedido(_session.Rol);
    public bool MostrarPedidoListo => PuedeEntregarPedido;
    public bool PuedeCobrarCuenta =>
        EsPedidoEntregado &&
        _capabilities.PuedeCobrarCuenta(_session.Rol);

    public bool PuedeCerrarMesa =>
        EsPedidoPagado &&
        (_capabilities.PuedeCerrarMesa(_session.Rol)
         || _capabilities.PuedeCobrarCuenta(_session.Rol)
         || _capabilities.PuedeEntregarPedido(_session.Rol));

    public string PedidoEnCursoTotalTexto => PedidoEnCurso is null ? "$0.00" : $"${PedidoEnCurso.Total:0.00}";
    public string TotalPedidoActualTexto => PedidoEnCurso is null ? TotalTexto : PedidoEnCursoTotalTexto;
    public bool MostrarEstadoVacio => !TieneItems && !TienePedidoEnCurso;

    public string ClienteTexto =>
        ClienteSeleccionado is null ? "👤 Asignar cliente" : $"👤 {ClienteSeleccionado.Nombre}  ·  {ClienteSeleccionado.Cedula}";

    public string ClienteNombreTexto => ClienteSeleccionado?.Nombre ?? "Sin asignar";
    public string ClienteEstadoTexto => ClienteSeleccionado is null ? "Cliente" : ClienteSeleccionado.Cedula;
    public bool TieneClienteSeleccionado => ClienteSeleccionado is not null;
    public bool TieneClientesEncontrados => ClientesEncontrados.Count > 0;
    public bool MostrarClienteVacio => MostrarClienteSheet && !IsBuscandoClientes && !MostrandoCrearCliente && !TieneClientesEncontrados;

    public int TotalItems => Carrito.Sum(i => i.Cantidad);
    public string TotalTexto => $"${Total:0.00}";

    // ── Propiedades para el nuevo diseño ──────────────────────────────
    public string ComedorTexto    => MesaSeleccionada?.NombreComedor ?? "";
    public string MesaNumeroTexto => MesaSeleccionada is null ? "Selecciona mesa" : $"Mesa #{MesaSeleccionada.Numero}";
    public bool   TieneItems      => Carrito.Count > 0;
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;
    public bool MostrarCarritoFijo => TieneItems && !MostrarPedidoSheet;
    public string CarritoItemsTexto => TotalItems == 1 ? "1 producto" : $"{TotalItems} productos";
    public string ObservacionesResumenTexto => string.IsNullOrWhiteSpace(Observaciones) ? "Sin observaciones" : "Con observaciones";
    public bool TieneObservacionesPedido => !string.IsNullOrWhiteSpace(Observaciones);

    private bool EsPedidoBorradorOEnEdicion =>
        PedidoEnCurso is null || PedidoEnCurso.Estado == PedidoEstados.Pendiente;

    private bool EsPedidoEntregado =>
        PedidoEnCurso?.Estado is PedidoEstados.Completado or PedidoEstados.Entregado;

    private bool EsPedidoPagado =>
        string.Equals(PedidoEnCurso?.Estado, CuentaEstados.Pagada, StringComparison.OrdinalIgnoreCase)
        || string.Equals(PedidoEnCurso?.Estado, "PAGADO", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty] private bool mostrarExito;
    [ObservableProperty] private string pedidoEntregadoTotalTexto = "$0.00";

    public PosViewModel(
        RoleCapabilityService capabilities,
        PosCatalogService catalogService,
        PosDataService dataService,
        PosDraftService draftService,
        PosMediaService mediaService,
        PosOrderStateService orderStateService,
        ClienteApiService clientesApi,
        SessionService session,
        PosOrderWorkflowService posWorkflow,
        INavigationService navigation,
        NotificationService notifications)
    {
        var sw = Stopwatch.StartNew();
        _capabilities = capabilities;
        _catalogService = catalogService;
        _dataService = dataService;
        _draftService = draftService;
        _mediaService = mediaService;
        _orderStateService = orderStateService;
        _clientesApi = clientesApi;
        _session = session;
        _posWorkflow = posWorkflow;
        _navigation = navigation;
        _notifications = notifications;

        Carrito.CollectionChanged += OnCarritoCollectionChanged;
        ClientesEncontrados.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TieneClientesEncontrados));
            OnPropertyChanged(nameof(MostrarClienteVacio));
        };
        ActualizarHeaderOperativo();
        Debug.WriteLine($"[PERF][POS] Constructor PosViewModel: {sw.ElapsedMilliseconds} ms");
    }

    // Llamado automáticamente cuando Shell establece MesaSeleccionada vía QueryProperty
    partial void OnMesaSeleccionadaChanged(MesaResponse? value)
    {
        ActualizarHeaderOperativo();
        if (value is not null && MesasVisuales.Count > 0)
            _ = CargarPedidoEnCursoAsync();
    }

    partial void OnPedidoIdChanged(int value)
    {
        ActualizarHeaderOperativo();
        if (value > 0 && MesaSeleccionada is not null && MesasVisuales.Count > 0)
            _ = CargarPedidoEnCursoAsync();
    }

    partial void OnObservacionesChanged(string value)
    {
        OnPropertyChanged(nameof(ObservacionesResumenTexto));
        OnPropertyChanged(nameof(TieneObservacionesPedido));
    }

    partial void OnMostrandoCrearClienteChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarClienteVacio));
    }

    partial void OnIsBuscandoClientesChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarClienteVacio));
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
        var sw = Stopwatch.StartNew();
        try
        {
            var snapshot = await _dataService.CargarDatosAsync();
            Debug.WriteLine($"[PERF][POS] API snapshot inicial: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            ReemplazarItems(Mesas, snapshot.Mesas);
            ReemplazarItems(MesasVisuales, snapshot.MesasVisuales);
            AplicarFiltroMesas();

            ReemplazarItems(Categorias, snapshot.Categorias);

            ReemplazarItems(Productos, snapshot.Productos);
            AplicarBusquedaProductos();

            if (MesaSeleccionada is not null)
                await CargarPedidoEnCursoAsync();

            _ultimaCargaUtc = DateTimeOffset.UtcNow;
            ActualizarHeaderOperativo();
            Debug.WriteLine($"[PERF][POS] Render data binding snapshot: {sw.ElapsedMilliseconds} ms");
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
            _categoriaSeleccionadaId = categoria.Idcategoria;
            if (Productos.Count == 0)
            {
                var lista = await _catalogService.ObtenerProductosActivosAsync();
                ReemplazarProductos(lista);
            }

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
            if (Productos.Count == 0)
            {
                var lista = await _catalogService.ObtenerProductosActivosAsync();
                ReemplazarProductos(lista);
            }

            _categoriaSeleccionadaId = null;
            AplicarBusquedaProductos();
        }
        finally { IsBusy = false; }
    }

    partial void OnBusquedaProductoChanged(string value)
    {
        _busquedaProductoCts?.Cancel();
        var cts = new CancellationTokenSource();
        _busquedaProductoCts = cts;
        _ = AplicarBusquedaProductosDiferidaAsync(cts.Token);
    }
    partial void OnBusquedaClienteChanged(string value) => AplicarFiltroClientes();

    private void AplicarBusquedaProductos()
    {
        ProgramarCargaDiferidaImagenes();

        IEnumerable<ProductoResponse> fuente = Productos;
        if (_categoriaSeleccionadaId is int categoriaId)
            fuente = fuente.Where(p => p.CategoriaId == categoriaId);

        _productosFiltradosCache = _catalogService.FiltrarProductos(fuente, BusquedaProducto);
        _productosMostrados = Math.Min(ProductosPageSize, _productosFiltradosCache.Count);

        ReemplazarItems(ProductosFiltrados, _productosFiltradosCache.Take(_productosMostrados));
        Debug.WriteLine($"[PERF][POS] Productos visibles: {_productosMostrados}/{_productosFiltradosCache.Count}");
    }

    [RelayCommand]
    public void CargarMasProductos()
    {
        if (_productosMostrados >= _productosFiltradosCache.Count)
            return;

        var inicio = _productosMostrados;
        var fin = Math.Min(inicio + ProductosPageSize, _productosFiltradosCache.Count);
        for (var i = inicio; i < fin; i++)
            ProductosFiltrados.Add(_productosFiltradosCache[i]);

        _productosMostrados = fin;
        _ = HabilitarImagenesProductosAsync(CancellationToken.None);
        Debug.WriteLine($"[PERF][POS] Productos visibles: {_productosMostrados}/{_productosFiltradosCache.Count}");
    }

    private void ProgramarCargaDiferidaImagenes()
    {
        ImagenesProductosHabilitadas = false;
        _imagenesProductosCts?.Cancel();
        var cts = new CancellationTokenSource();
        _imagenesProductosCts = cts;
        _ = HabilitarImagenesProductosAsync(cts.Token);
    }

    private async Task HabilitarImagenesProductosAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Delay(350, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                ImagenesProductosHabilitadas = true;
                Debug.WriteLine($"[PERF][POS] Habilitar carga progresiva de imagenes: {sw.ElapsedMilliseconds} ms");
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AplicarBusquedaProductosDiferidaAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            MainThread.BeginInvokeOnMainThread(AplicarBusquedaProductos);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void AplicarFiltroClientes()
    {
        var texto = (BusquedaCliente ?? string.Empty).Trim();
        IEnumerable<ClienteResponse> filtrados = _clientesDisponibles;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            filtrados = filtrados.Where(c =>
                Contiene(c.Nombre, texto)
                || Contiene(c.Cedula, texto)
                || Contiene(c.Telefono, texto));
        }

        ReemplazarItems(ClientesEncontrados, filtrados.Take(20));
        OnPropertyChanged(nameof(MostrarClienteVacio));
    }

    private static bool Contiene(string? valor, string filtro)
        => !string.IsNullOrWhiteSpace(valor)
           && valor.Contains(filtro, StringComparison.OrdinalIgnoreCase);

    private void LimpiarFormularioCliente()
    {
        ClienteNuevoNombre = string.Empty;
        ClienteNuevoCedula = string.Empty;
        ClienteNuevoTelefono = string.Empty;
        ClienteNuevoEmail = string.Empty;
        BusquedaCliente = string.Empty;
    }

    private void ReemplazarProductos(IEnumerable<ProductoResponse> productos)
        => ReemplazarItems(Productos, productos);

    // ── Carrito ───────────────────────────────────────────────────────

    [RelayCommand]
    public Task AgregarAlCarritoAsync(ProductoResponse producto)
    {
        var sw = Stopwatch.StartNew();

        if (producto.StockActual <= 0)
        {
            return MostrarMensajeAsync($"{producto.Nombre} no tiene stock disponible.", error: true);
        }

        var existente = Carrito.FirstOrDefault(i => i.Producto.Idproducto == producto.Idproducto);
        if (existente is not null)
        {
            if (existente.Cantidad >= producto.StockActual)
            {
                return MostrarMensajeAsync(
                    $"Solo hay {producto.StockActual} unidad(es) disponibles de {producto.Nombre}.",
                    error: true);
            }

            existente.Cantidad++;
        }
        else
        {
            Carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1 });
        }
        RecalcularTotal();
        Debug.WriteLine($"[PERF][POS] Agregar producto: {sw.ElapsedMilliseconds} ms");
        return Task.CompletedTask;
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
        MostrarPedidoSheet = false;
        NotificarResumenCarritoCompleto();
    }

    private void RecalcularTotal()
    {
        Total = Carrito.Sum(i => i.Subtotal);
        ActualizarHeaderOperativo();
        NotificarResumenCarritoCompleto();
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
        MostrarClienteSheet = true;
        MostrandoCrearCliente = false;
        ClienteSheetMensaje = string.Empty;
        IsBusy = true;
        IsBuscandoClientes = true;
        var sw = Stopwatch.StartNew();
        try
        {
            if (_clientesDisponibles.Count == 0
                || _ultimaCargaClientesUtc is null
                || DateTimeOffset.UtcNow - _ultimaCargaClientesUtc > VentanaCacheClientes)
            {
                _clientesDisponibles = await _clientesApi.GetClientesAsync();
                _ultimaCargaClientesUtc = DateTimeOffset.UtcNow;
                Debug.WriteLine($"[PERF][POS] API clientes: {sw.ElapsedMilliseconds} ms");
            }

            AplicarFiltroClientes();
        }
        catch (Exception ex)
        {
            ClienteSheetMensaje = $"Error cargando clientes: {ex.Message}";
        }
        finally
        {
            IsBuscandoClientes = false;
            IsBusy = false;
            OnPropertyChanged(nameof(MostrarClienteVacio));
        }
    }

    [RelayCommand]
    public void QuitarCliente() => ClienteSeleccionado = null;

    [RelayCommand]
    public async Task SeleccionarClienteAsync(ClienteResponse cliente)
    {
        ClienteSeleccionado = cliente;
        CerrarClienteSheet();
        await MostrarMensajeAsync($"{cliente.Nombre} asignado al pedido.", error: false);
    }

    [RelayCommand]
    public void MostrarCrearCliente()
    {
        MostrandoCrearCliente = true;
        ClienteSheetMensaje = string.Empty;
        if (string.IsNullOrWhiteSpace(ClienteNuevoNombre) && !string.IsNullOrWhiteSpace(BusquedaCliente))
            ClienteNuevoNombre = BusquedaCliente.Trim();
    }

    [RelayCommand]
    public async Task CrearClienteRapidoAsync()
    {
        IsBusy = true;
        ClienteSheetMensaje = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(ClienteNuevoNombre))
            {
                ClienteSheetMensaje = "Ingresa el nombre completo del cliente.";
                return;
            }

            var clienteCreado = await _clientesApi.CrearClienteAsync(new ClienteRequest
            {
                Nombre = ClienteNuevoNombre.Trim(),
                Cedula = string.IsNullOrWhiteSpace(ClienteNuevoCedula)
                    ? $"SC{DateTime.Now:yyyyMMddHHmmssfff}"
                    : ClienteNuevoCedula.Trim(),
                Telefono = string.IsNullOrWhiteSpace(ClienteNuevoTelefono) ? null : ClienteNuevoTelefono.Trim(),
                Email = string.IsNullOrWhiteSpace(ClienteNuevoEmail) ? null : ClienteNuevoEmail.Trim(),
                Estado = true
            });

            ClienteSeleccionado = clienteCreado;
            LimpiarFormularioCliente();
            CerrarClienteSheet();
            await MostrarMensajeAsync("Cliente creado y asignado al pedido", error: false);
        }
        catch (Exception ex)
        {
            ClienteSheetMensaje = $"Error creando cliente: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void CerrarClienteSheet()
    {
        MostrarClienteSheet = false;
        MostrandoCrearCliente = false;
        ClienteSheetMensaje = string.Empty;
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

    [RelayCommand]
    public void VerPedido()
    {
        if (TieneItems)
            MostrarPedidoSheet = true;
    }

    [RelayCommand]
    public void CerrarPedidoSheet()
    {
        MostrarPedidoSheet = false;
    }

    // Volver al mapa de mesas
    [RelayCommand]
    public Task VolverAsync()
        => _navigation.GoToAsync("//mapa");

    [RelayCommand]
    public Task IrNotificacionesAsync()
        => _navigation.GoToAsync("notificacionesPage");

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

        if (PedidoEnCurso is not null)
        {
            await MostrarMensajeAsync("Este pedido ya existe. Usa Enviar a cocina para continuar el flujo.", error: true);
            return;
        }

        var estadoDestino = PedidoEstados.Pendiente;
        var mensajeExito = "Pedido";

        await CrearPedidoAsync(estadoDestino, mensajeExito);
    }

    [RelayCommand]
    public async Task EnviarACocinaAsync()
    {
        if (IsEnviandoACocina)
            return;

        IsEnviandoACocina = true;
        try
        {
            if (!PuedeEnviarACocina)
            {
                await MostrarMensajeAsync("Este pedido ya no puede enviarse a cocina.", error: true);
                return;
            }

            if (PedidoEnCurso is not null)
            {
                await CambiarEstadoPedidoActualAsync(PedidoEstados.EnCocina, "Pedido enviado a cocina.", navegarAMesas: true);
                MostrarPedidoSheet = false;
                return;
            }

            await CrearPedidoAsync(PedidoEstados.EnCocina, "Pedido enviado a cocina");
        }
        finally
        {
            IsEnviandoACocina = false;
        }
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

    [RelayCommand]
    public async Task CobrarCuentaAsync()
    {
        if (PedidoEnCurso is null)
        {
            await MostrarMensajeAsync("No hay pedido entregado para cobrar.", error: true);
            return;
        }

        if (!PuedeCobrarCuenta)
        {
            await MostrarMensajeAsync("Este pedido aun no esta listo para cobro.", error: true);
            return;
        }

        MostrarPedidoSheet = false;
        await _navigation.GoToAsync("pago",
            new Dictionary<string, object> { { "Pedido", PedidoEnCurso } });
    }

    private async Task CrearPedidoAsync(string estadoDestino, string mensajeExito)
    {
        var sw = Stopwatch.StartNew();
        var clientePedido = ClienteSeleccionado ?? await ObtenerClienteGenericoAsync();
        var errorValidacion = _draftService.ValidarPedido(MesaSeleccionada, Carrito, clientePedido);
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
            clientePedido,
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
                await MostrarMensajeAsync($"Pedido guardado localmente, se sincronizara al recuperar conexion. Pendientes: {resultado.Pendientes}.", error: false);
                return;
            }

            var pedido = resultado.Pedido;
            if (pedido is null)
                throw new InvalidOperationException("No se pudo confirmar el pedido enviado.");

            LimpiarCarrito();
            PedidoEnCurso = pedido;
            ClienteSeleccionado = null;
            MostrarPedidoSheet = false;
            Debug.WriteLine($"[PERF][POS] Enviar pedido total VM: {sw.ElapsedMilliseconds} ms");

            if (estadoDestino == PedidoEstados.EnCocina)
            {
                var navSw = Stopwatch.StartNew();
                await _navigation.GoToAsync("//mapa");
                Debug.WriteLine($"[PERF][POS] Navegacion POS -> Mapa: {navSw.ElapsedMilliseconds} ms");
                return;
            }

            await CargarDatosAsync();
            var mensaje = string.IsNullOrWhiteSpace(resultado.VinculoCuentaAdvertencia)
                ? $"{mensajeExito} #{pedido.Idpedido}."
                : $"{mensajeExito} #{pedido.Idpedido}. {resultado.VinculoCuentaAdvertencia}";
            await MostrarMensajeAsync(mensaje, error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync(ApiErrorHelper.ToUserMessage(ex, "enviar pedido"), error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task<ClienteResponse> ObtenerClienteGenericoAsync()
    {
        const string cedulaGenerica = "9999999999";
        var clientes = _clientesDisponibles.Count > 0
            ? _clientesDisponibles
            : await _clientesApi.GetClientesAsync();

        var existente = clientes.FirstOrDefault(c =>
            string.Equals(c.Cedula, cedulaGenerica, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Nombre, "Consumidor final", StringComparison.OrdinalIgnoreCase));

        if (existente is not null)
            return existente;

        return await _clientesApi.CrearClienteAsync(new ClienteRequest
        {
            Nombre = "Consumidor final",
            Cedula = cedulaGenerica,
            Estado = true
        });
    }

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

    private async Task CambiarEstadoPedidoActualAsync(string estado, string mensajeExito, bool navegarAMesas = false)
    {
        if (PedidoEnCurso is null) return;

        var sw = Stopwatch.StartNew();
        IsBusy = true;
        try
        {
            PedidoEnCurso = await _posWorkflow.CambiarEstadoPedidoAsync(PedidoEnCurso, estado);
            Debug.WriteLine($"[PERF][POS] Cambiar estado pedido: {sw.ElapsedMilliseconds} ms");

            if (navegarAMesas)
            {
                var navSw = Stopwatch.StartNew();
                await _navigation.GoToAsync("//mapa");
                Debug.WriteLine($"[PERF][POS] Navegacion POS -> Mapa: {navSw.ElapsedMilliseconds} ms");
                return;
            }

            await CargarDatosAsync();
            await MostrarMensajeAsync(mensajeExito, error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync(ApiErrorHelper.ToUserMessage(ex, "cambiar estado del pedido"), error: true);
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
        ActualizarHeaderOperativo();
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
        OnPropertyChanged(nameof(MostrarCarritoFijo));
        OnPropertyChanged(nameof(CarritoItemsTexto));
        OnPropertyChanged(nameof(PuedeGuardarBorrador));
        OnPropertyChanged(nameof(PuedeEnviarACocina));
        OnPropertyChanged(nameof(PuedeEjecutarEnviarACocina));
        OnPropertyChanged(nameof(MostrarPedidoEnPreparacion));
        OnPropertyChanged(nameof(PuedeEntregarPedido));
        OnPropertyChanged(nameof(PuedeCobrarCuenta));
        OnPropertyChanged(nameof(PuedeCerrarMesa));
    }

    private void NotificarResumenCarritoCompleto()
    {
        NotificarResumenCarritoBase();
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
        OnPropertyChanged(nameof(TotalTexto));
        ActualizarHeaderOperativo();
    }

    partial void OnMostrarPedidoSheetChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarCarritoFijo));
    }

    private void ActualizarHeaderOperativo()
    {
        NombreUsuarioHeader = _session.NombreCompleto ?? _session.Username ?? "Usuario";
        RolUsuarioHeader = FormatearRol(_session.Rol);
        InicialesUsuario = CrearIniciales(NombreUsuarioHeader);
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);

        HeaderKpi1Titulo = "Mesa";
        HeaderKpi1Valor = MesaSeleccionada is null ? "-" : $"#{MesaSeleccionada.Numero}";
        HeaderKpi2Titulo = "Estado";
        HeaderKpi2Valor = EstadoPedidoTexto;
        HeaderKpi3Titulo = "Total";
        HeaderKpi3Valor = TotalPedidoActualTexto;
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
