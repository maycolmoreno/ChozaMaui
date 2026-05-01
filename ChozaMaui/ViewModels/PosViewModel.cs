using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(MesaSeleccionada), "Mesa")]
public partial class PosViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly ReceiptPdfService _receiptPdf;
    private PedidoResponse? _ultimoPedidoParaRecibo;

    // ── Listas base ────────────────────────────────────────────────────
    public ObservableCollection<MesaResponse> Mesas { get; } = [];
    public ObservableCollection<CategoriaResponse> Categorias { get; } = [];
    public ObservableCollection<ProductoResponse> Productos { get; } = [];
    public ObservableCollection<ItemCarrito> Carrito { get; } = [];

    // ── Selecciones ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MesaSeleccionadaTexto))]
    [NotifyPropertyChangedFor(nameof(MesaHeaderTexto))]
    private MesaResponse? mesaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoPedidoTexto))]
    [NotifyPropertyChangedFor(nameof(EstadoPedidoColor))]
    [NotifyPropertyChangedFor(nameof(TienePedidoEnCurso))]
    [NotifyPropertyChangedFor(nameof(PuedeMarcarListo))]
    [NotifyPropertyChangedFor(nameof(PuedeEntregarPedido))]
    [NotifyPropertyChangedFor(nameof(PedidoEnCursoTotalTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPedidoActualTexto))]
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
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mostrarMensaje;
    [ObservableProperty] private bool mensajeEsError;

    // ── Cámara ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneFoto))]
    private FotoAdjunta? fotoAdjunta;

    public bool TieneFoto => FotoAdjunta is not null;
    public string FotoTexto => FotoAdjunta is null ? "Adjuntar foto" : $"📷 {FotoAdjunta.NombreArchivo}";

    [ObservableProperty] private bool mostrarCarrito;

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
    public bool PuedeMarcarListo => PedidoEnCurso?.Estado is "EN_COCINA" or "EN_BAR";
    public bool PuedeEntregarPedido => PedidoEnCurso?.PuedeEntregarse == true;
    public string PedidoEnCursoTotalTexto => PedidoEnCurso is null ? "$0.00" : $"${PedidoEnCurso.Total:0.00}";
    public string TotalPedidoActualTexto => PedidoEnCurso is null ? TotalTexto : PedidoEnCursoTotalTexto;

    public string ClienteTexto =>
        ClienteSeleccionado is null ? "👤 Asignar cliente" : $"👤 {ClienteSeleccionado.Nombre}  ·  {ClienteSeleccionado.Cedula}";

    public string ClienteNombreTexto => ClienteSeleccionado?.Nombre ?? "Sin asignar";
    public string ClienteEstadoTexto => ClienteSeleccionado is null ? "Cliente" : ClienteSeleccionado.Cedula;

    public int TotalItems => Carrito.Sum(i => i.Cantidad);
    public string TotalTexto => $"${Total:0.00}";
    public string BienvenidaTexto => $"Bienvenido, {_session.NombreCompleto ?? _session.Username}";

    // ── Propiedades para el nuevo diseño ──────────────────────────────
    public string ComedorTexto    => MesaSeleccionada?.NombreComedor ?? "";
    public string MesaNumeroTexto => MesaSeleccionada is null ? "Selecciona mesa" : $"Mesa #{MesaSeleccionada.Numero}";
    public bool   TieneItems      => Carrito.Count > 0;

    [ObservableProperty] private bool mostrarExito;

    private string _pedidoEntregadoTotalTexto = "$0.00";
    public string PedidoEntregadoTotalTexto => _pedidoEntregadoTotalTexto;

    public PosViewModel(ApiService api, SessionService session, ReceiptPdfService receiptPdf)
    {
        _api = api;
        _session = session;
        _receiptPdf = receiptPdf;
    }

    // Llamado automáticamente cuando Shell establece MesaSeleccionada vía QueryProperty
    partial void OnMesaSeleccionadaChanged(MesaResponse? value)
    {
        OnPropertyChanged(nameof(MesaNumeroTexto));
        OnPropertyChanged(nameof(ComedorTexto));
        if (value is not null)
            _ = CargarDatosAsync();
    }

    // ── Carga inicial ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        IsBusy = true;
        try
        {
            var catTask  = _api.GetCategoriasActivasAsync();
            var prodTask = _api.GetProductosActivosAsync();

            await Task.WhenAll(catTask, prodTask);

            Categorias.Clear();
            foreach (var c in catTask.Result) Categorias.Add(c);

            Productos.Clear();
            foreach (var p in prodTask.Result) Productos.Add(p);

            if (MesaSeleccionada is not null)
                await CargarPedidoEnCursoAsync();
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando datos: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Filtro por categoría ──────────────────────────────────────────

    [RelayCommand]
    public async Task FiltrarPorCategoriaAsync(CategoriaResponse categoria)
    {
        IsBusy = true;
        try
        {
            var lista = await _api.GetProductosPorCategoriaAsync(categoria.Idcategoria);
            Productos.Clear();
            foreach (var p in lista) Productos.Add(p);
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
            var lista = await _api.GetProductosActivosAsync();
            Productos.Clear();
            foreach (var p in lista) Productos.Add(p);
        }
        finally { IsBusy = false; }
    }

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
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
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
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
    }

    [RelayCommand]
    public void LimpiarCarrito()
    {
        Carrito.Clear();
        Total = 0;
        FotoAdjunta = null;
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TieneItems));
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
    }

    private void RecalcularTotal()
    {
        Total = Carrito.Sum(i => i.Subtotal);
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TieneItems));
    }

    [RelayCommand]
    public async Task SeleccionarMesaAsync(MesaResponse mesa)
    {
        MesaSeleccionada = mesa;
        await CargarPedidoEnCursoAsync();
    }

    [RelayCommand]
    public void ToggleCarrito() => MostrarCarrito = !MostrarCarrito;

    [RelayCommand]
    public async Task BuscarClienteAsync()
    {
        IsBusy = true;
        List<ClienteResponse> clientes;
        try
        {
            clientes = await _api.GetClientesAsync();
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando clientes: {ex.Message}", error: true);
            return;
        }
        finally
        {
            // IsBusy se libera aquí, antes del ActionSheet, para no bloquear la UI
            IsBusy = false;
        }

        if (clientes.Count == 0)
        {
            await MostrarMensajeAsync("No hay clientes registrados.", error: true);
            return;
        }

        var opciones = clientes.Select(c => $"{c.Nombre}  ({c.Cedula})").ToArray();
        var elegida = await Shell.Current.DisplayActionSheetAsync(
            "Seleccionar cliente", "Cancelar", null, opciones);

        if (elegida is null || elegida == "Cancelar") return;

        var idx = Array.IndexOf(opciones, elegida);
        if (idx >= 0) ClienteSeleccionado = clientes[idx];
    }

    [RelayCommand]
    public void QuitarCliente() => ClienteSeleccionado = null;

    [RelayCommand]
    public async Task CrearClienteRapidoAsync()
    {
        var nombre = await Shell.Current.DisplayPromptAsync(
            "Nuevo cliente",
            "Nombre del cliente",
            accept: "Siguiente",
            cancel: "Cancelar",
            placeholder: "Ej: Juan Pérez");

        if (string.IsNullOrWhiteSpace(nombre)) return;

        var cedula = await Shell.Current.DisplayPromptAsync(
            "Nuevo cliente",
            "Cédula del cliente",
            accept: "Crear",
            cancel: "Cancelar",
            placeholder: "Ej: 0102030405",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(cedula)) return;

        IsBusy = true;
        try
        {
            ClienteSeleccionado = await _api.CrearClienteAsync(new ClienteRequest
            {
                Nombre = nombre.Trim(),
                Cedula = cedula.Trim(),
                Estado = true
            });

            await MostrarMensajeAsync($"Cliente {ClienteSeleccionado.Nombre} creado y asignado.", error: false);
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
            // Verificar soporte
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await MostrarMensajeAsync("Cámara no disponible en este dispositivo.", error: true);
                return;
            }

            var permiso = await Permissions.RequestAsync<Permissions.Camera>();
            if (permiso != PermissionStatus.Granted)
            {
                await MostrarMensajeAsync("Permiso de cámara denegado.", error: true);
                return;
            }

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            if (foto is null) return;

            // Guardar en almacenamiento local de la app
            var carpeta = FileSystem.AppDataDirectory;
            var nombre = $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var destino = Path.Combine(carpeta, nombre);

            using var origen = await foto.OpenReadAsync();
            using var archivo = File.OpenWrite(destino);
            await origen.CopyToAsync(archivo);

            FotoAdjunta = new FotoAdjunta { RutaLocal = destino };
            OnPropertyChanged(nameof(FotoTexto));
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
        OnPropertyChanged(nameof(FotoTexto));
    }

    // Elimina completamente un ítem del carrito (botón 🗑)
    [RelayCommand]
    public void EliminarItemCarrito(ItemCarrito item)
    {
        Carrito.Remove(item);
        RecalcularTotal();
        OnPropertyChanged(nameof(EstadoPedidoTexto));
        OnPropertyChanged(nameof(EstadoPedidoColor));
    }

    // Volver al mapa de mesas
    [RelayCommand]
    public async Task VolverAsync()
        => await Shell.Current.GoToAsync("//mapa");

    // Cerrar mesa tras entrega: oculta éxito, limpia y vuelve al mapa
    [RelayCommand]
    public async Task CerrarMesaAsync()
    {
        MostrarExito = false;
        LimpiarCarrito();
        PedidoEnCurso   = null;
        MesaSeleccionada = null;
        await Shell.Current.GoToAsync("//mapa");
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
            var rutaPdf = await _receiptPdf.GenerarReciboPedidoAsync(pedido, MeseroTexto);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Recibo pedido #{pedido.Idpedido}",
                File = new ShareFile(rutaPdf, "application/pdf")
            });
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"No se pudo generar el recibo: {ex.Message}", error: true);
        }
    }

    // ── Enviar pedido ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task EnviarPedidoAsync()
        => await CrearPedidoAsync("PENDIENTE", "Pedido");

    [RelayCommand]
    public async Task EnviarACocinaAsync()
        => await CrearPedidoAsync("EN_COCINA", "Pedido enviado a cocina");

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
            await MostrarMensajeAsync("Solo puedes marcar listo un pedido en cocina o bar.", error: true);
            return;
        }

        await CambiarEstadoPedidoActualAsync("LISTO_PARA_ENTREGA", "Pedido marcado como listo.");
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
            await MostrarMensajeAsync("Solo se puede entregar un pedido listo.", error: true);
            return;
        }

        _ultimoPedidoParaRecibo = PedidoEnCurso;
        _pedidoEntregadoTotalTexto = $"${PedidoEnCurso.Total:0.00}";
        OnPropertyChanged(nameof(PedidoEntregadoTotalTexto));

        await CambiarEstadoPedidoActualAsync("COMPLETADO", "Pedido entregado al cliente.");
        MostrarExito = true;
    }

    private async Task CrearPedidoAsync(string estadoDestino, string mensajeExito)
    {
        if (MesaSeleccionada is null)
        {
            await MostrarMensajeAsync("Selecciona una mesa antes de enviar.", error: true);
            return;
        }
        if (Carrito.Count == 0)
        {
            await MostrarMensajeAsync("El carrito está vacío.", error: true);
            return;
        }
        if (ClienteSeleccionado is null)
        {
            await MostrarMensajeAsync("Asigna un cliente antes de enviar el pedido.", error: true);
            return;
        }

        var errorStock = await ValidarStockCarritoAsync();
        if (errorStock is not null)
        {
            await MostrarMensajeAsync(errorStock, error: true);
            return;
        }

        IsBusy = true;
        try
        {
            // Incluir referencia a foto en las observaciones si existe
            var obs = Observaciones;
            if (FotoAdjunta is not null)
                obs = string.IsNullOrWhiteSpace(obs)
                    ? $"[Foto: {FotoAdjunta.NombreArchivo}]"
                    : $"{obs} [Foto: {FotoAdjunta.NombreArchivo}]";

            var request = new PedidoRequest
            {
                Fecha = DateTime.Now.ToString("s"),  // asignado justo al enviar
                IdUsuario = _session.UserId,
                IdMesa = MesaSeleccionada.Idmesa,
                IdCliente = ClienteSeleccionado.Idcliente,
                Observaciones = obs,
                Detalles = Carrito.Select(i => new PedidoDetalleRequest
                {
                    IdProducto = i.Producto.Idproducto,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.Producto.Precio
                }).ToList()
            };

            var pedido = await _api.CrearPedidoAsync(request);

            // Vincular pedido a una cuenta abierta (crear si no existe)
            int idCliente = ClienteSeleccionado.Idcliente;
            var cuentasAbiertas = await _api.ObtenerCuentasAbiertasAsync();
            var cuentaExistente = cuentasAbiertas
                .FirstOrDefault(c => c.Mesa?.Idmesa == MesaSeleccionada.Idmesa
                                  && c.Cliente?.Idcliente == idCliente);

            if (cuentaExistente is not null)
            {
                await _api.AgregarPedidoACuentaAsync(cuentaExistente.Idcuenta, pedido.Idpedido);
            }
            else
            {
                var nuevaCuenta = await _api.CrearCuentaAsync(MesaSeleccionada.Idmesa, idCliente, pedido.Total);
                await _api.AgregarPedidoACuentaAsync(nuevaCuenta.Idcuenta, pedido.Idpedido);
            }

            if (estadoDestino != "PENDIENTE")
                pedido = await _api.CambiarEstadoPedidoAsync(pedido.Idpedido, estadoDestino);

            LimpiarCarrito();
            MostrarCarrito = false;
            PedidoEnCurso = pedido;
            ClienteSeleccionado = null;
            Observaciones = string.Empty;
            await CargarDatosAsync();
            await MostrarMensajeAsync($"{mensajeExito} #{pedido.Idpedido}.", error: false);
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
            PedidoEnCurso = null;
            return;
        }

        try
        {
            var pedidos = await _api.GetPedidosAsync();
            PedidoEnCurso = pedidos
                .Where(p => p.EsActivo && p.Mesa?.Idmesa == MesaSeleccionada.Idmesa)
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefault();
        }
        catch
        {
            PedidoEnCurso = null;
        }
    }

    private async Task CambiarEstadoPedidoActualAsync(string estado, string mensajeExito)
    {
        if (PedidoEnCurso is null) return;

        IsBusy = true;
        try
        {
            PedidoEnCurso = await _api.CambiarEstadoPedidoAsync(PedidoEnCurso.Idpedido, estado);
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

    private async Task<string?> ValidarStockCarritoAsync()
    {
        try
        {
            var productosActuales = await _api.GetProductosActivosAsync();
            foreach (var item in Carrito)
            {
                var productoActual = productosActuales
                    .FirstOrDefault(p => p.Idproducto == item.Producto.Idproducto);

                if (productoActual is null || !productoActual.Estado)
                    return $"{item.Producto.Nombre} ya no está disponible.";

                if (item.Cantidad > productoActual.StockActual)
                    return $"Stock insuficiente para {productoActual.Nombre}. Disponible: {productoActual.StockActual}.";
            }

            return null;
        }
        catch (Exception ex)
        {
            return $"No se pudo validar el stock: {ex.Message}";
        }
    }
}
