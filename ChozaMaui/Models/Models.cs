using CommunityToolkit.Mvvm.ComponentModel;

namespace ChozaMaui.Models;

public class ComedorResponse
{
    public int Idcomedor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; }
    public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    public string EstadoColor => Estado ? "#28b779" : "#e94560";
}

public class ComedorRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int Idusuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool RequiereCambioPassword { get; set; }
}

public class CategoriaResponse
{
    public int Idcategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public string EstadoTexto => Estado ? "Activa" : "Inactiva";
    public string EstadoColor => Estado ? "#28b779" : "#e94560";
}

public class CategoriaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
}

public class ProductoResponse
{
    public int Idproducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double Precio { get; set; }
    public int StockActual { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public bool Estado { get; set; }
    public int CategoriaId { get; set; }
    public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    public string EstadoColor => Estado ? "#28b779" : "#e94560";
}

public class ProductoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public double Precio { get; set; }
    public int StockActual { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public bool Estado { get; set; } = true;
    public int CategoriaId { get; set; }
}

public class MesaResponse
{
    public int Idmesa { get; set; }
    public int Numero { get; set; }
    public int Capacidad { get; set; }
    public bool Estado { get; set; }
    public int? Idcomedor { get; set; }
    public string? NombreComedor { get; set; }

    public string Etiqueta => $"Mesa {Numero}" + (NombreComedor != null ? $" ({NombreComedor})" : "");

    // Helpers para la UI — estado visual
    public string EstadoTexto => Estado ? "LIBRE" : "OCUPADA";
    public string EstadoColor => Estado ? "#28b779" : "#e94560";
    public string CardBackground => Estado ? "#ffffff" : "#fff1f2";
    public string StrokeColor => Estado ? "#28b779" : "#e94560";
}

public class PedidoDetalleRequest
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public double PrecioUnitario { get; set; }
    public double Subtotal => Cantidad * PrecioUnitario;
}

public class PedidoRequest
{
    public string Fecha { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public int IdUsuario { get; set; }
    public int IdMesa { get; set; }
    public int IdCliente { get; set; }
    public List<PedidoDetalleRequest> Detalles { get; set; } = [];
}

public class PendingOrderDraft
{
    public string LocalId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string EstadoDestino { get; set; } = PedidoEstados.Pendiente;
    public PedidoRequest Request { get; set; } = new();
}

public class PedidoDetalleResponse
{
    public int Idpedidodetalle { get; set; }
    public ProductoResponse? Producto { get; set; }
    public int Cantidad { get; set; }
    public double PrecioUnitario { get; set; }
    public double Subtotal { get; set; }
}

public class UsuarioResponse
{
    public int Idusuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Estado { get; set; }
}

public class ClienteResponse
{
    public int Idcliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; } = true;

    // Helper para listado
    public string NombreCompleto => Nombre;
    public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    public string EstadoColor => Estado ? "#28b779" : "#e94560";
}

public class ClienteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; } = true;
}

public class PedidoResponse
{
    public int Idpedido { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaEnCocina { get; set; }
    public DateTime? FechaListoParaEntrega { get; set; }
    public DateTime? FechaEntregado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public double Subtotal { get; set; }
    public double Impuestos { get; set; }
    public double Total { get; set; }
    public int CantidadProductos { get; set; }
    public UsuarioResponse? Usuario { get; set; }
    public MesaResponse? Mesa { get; set; }
    public ClienteResponse? Cliente { get; set; }
    public List<PedidoDetalleResponse>? Detalle { get; set; }

    public string EstadoBadgeColor => Estado switch
    {
        PedidoEstados.Pendiente          => "#0ea5e9",
        PedidoEstados.EnCocina           => "#f59e0b",
        PedidoEstados.EnBar              => "#f59e0b",
        PedidoEstados.EnProceso          => "#f59e0b",
        PedidoEstados.Listo              => "#ef4444",
        PedidoEstados.ListoParaEntrega   => "#ef4444",
        PedidoEstados.Entregado          => "#6b7280",
        PedidoEstados.Completado         => "#6b7280",
        PedidoEstados.Cancelado          => "#ef4444",
        _ => "#6b7280"
    };

    public string EstadoBorderColor => EstadoBadgeColor;
    public string EstadoBadgeBackground => EstadoBadgeColor;
    public bool EsActivo => Estado is not (PedidoEstados.Completado or PedidoEstados.Entregado or PedidoEstados.Cancelado or "CERRADO");
    public bool EstaEnPreparacion => Estado is PedidoEstados.EnCocina or PedidoEstados.EnBar or PedidoEstados.EnProceso;
    public bool EstaListoParaEntrega => Estado is PedidoEstados.ListoParaEntrega or PedidoEstados.Listo;
    public bool PuedeEntregarse => EstaListoParaEntrega;

    public string EstadoTextoVisual => Estado switch
    {
        PedidoEstados.EnCocina         => "EN PREPARACION",
        PedidoEstados.EnBar            => "EN BAR",
        PedidoEstados.EnProceso        => "EN PREPARACION",
        PedidoEstados.ListoParaEntrega => "LISTO",
        PedidoEstados.Completado       => "COMPLETADO",
        _                    => Estado
    };

    public string TiempoTranscurrido
    {
        get
        {
            var diff = DateTime.Now - Fecha;
            if (diff.TotalHours >= 1) return $"{(int)diff.TotalHours}h {diff.Minutes:D2}m";
            return $"{(int)diff.TotalMinutes:D2}:{diff.Seconds:D2}";
        }
    }

    public string ResumenItems
    {
        get
        {
            if (Detalle is null || Detalle.Count == 0) return string.Empty;
            var partes = Detalle.Take(3).Select(d => $"{d.Cantidad}x {d.Producto?.Nombre}");
            var texto = string.Join(", ", partes);
            return Detalle.Count > 3 ? texto + "..." : texto;
        }
    }

    /// <summary>True cuando el pedido puede cobrarse (estado LISTO o ENTREGADO).</summary>
    public bool EsCobrable => Estado == PedidoEstados.Listo || Estado == PedidoEstados.Entregado ||
                              Estado == PedidoEstados.ListoParaEntrega || Estado == PedidoEstados.Completado;

    // ── Propiedades para tarjetas de PedidosPage ──────────────────────

    /// Color del círculo de ícono por estado.
    public string CircleColor => Estado switch
    {
        PedidoEstados.Pendiente                     => "#0ea5e9",
        PedidoEstados.EnCocina or PedidoEstados.EnBar
            or PedidoEstados.EnProceso              => "#f59e0b",
        PedidoEstados.Listo or PedidoEstados.ListoParaEntrega => "#28b779",
        PedidoEstados.Completado or PedidoEstados.Entregado   => "#28b779",
        PedidoEstados.Cancelado                     => "#ef4444",
        _ => "#6b7280"
    };

    /// Fondo claro del badge de estado.
    public string BadgeLightBackground => Estado switch
    {
        PedidoEstados.EnCocina or PedidoEstados.EnBar or PedidoEstados.EnProceso => "#FFF3E0",
        PedidoEstados.Listo or PedidoEstados.ListoParaEntrega                    => "#E8F5E9",
        PedidoEstados.Completado or PedidoEstados.Entregado                      => "#E8F5E9",
        PedidoEstados.Cancelado                                                  => "#FFEBEE",
        PedidoEstados.Pendiente                                                  => "#E3F2FD",
        _ => "#F5F5F5"
    };

    /// Color del texto del badge (mismo tono que el color principal del estado).
    public string BadgeTextColor => Estado switch
    {
        PedidoEstados.EnCocina or PedidoEstados.EnBar or PedidoEstados.EnProceso => "#F59E0B",
        PedidoEstados.Listo or PedidoEstados.ListoParaEntrega                    => "#28B779",
        PedidoEstados.Completado or PedidoEstados.Entregado                      => "#28B779",
        PedidoEstados.Cancelado                                                  => "#EF4444",
        PedidoEstados.Pendiente                                                  => "#0EA5E9",
        _ => "#6B7280"
    };

    /// Texto con ícono para el badge de estado.
    public string BadgeTexto => Estado switch
    {
        PedidoEstados.EnCocina or PedidoEstados.EnBar
            or PedidoEstados.EnProceso             => "🍳 EN PREPARACIÓN",
        PedidoEstados.Listo or PedidoEstados.ListoParaEntrega => "✓ LISTO",
        PedidoEstados.Completado or PedidoEstados.Entregado   => "✓ ENTREGADO",
        PedidoEstados.Cancelado                              => "✗ CANCELADO",
        PedidoEstados.Pendiente                              => "⏱ PENDIENTE",
        _ => Estado
    };

    /// Nombre del cliente asignado o texto por defecto.
    public string ClienteNombreTexto => Cliente?.Nombre ?? "Sin cliente";

    /// Fecha formateada + número de pedido.
    public string FechaYNumTexto => $"{Fecha:hh:mm tt}  ·  Pedido #{Idpedido}";

    /// Texto de cantidad de productos.
    public string NumProductosTexto => CantidadProductos == 1 ? "1 producto" : $"{CantidadProductos} productos";

    /// Texto del botón principal de acción.
    public string TextoBotonPrincipal => EsActivo ? "Ver pedido" : "Ver detalle";

    /// True si se puede "Abrir" el pedido en POS (está activo y en cocina/bar/pendiente).
    public bool EsAbrible => Estado is PedidoEstados.EnCocina or PedidoEstados.EnBar or PedidoEstados.EnProceso or PedidoEstados.Pendiente;

    /// True si se puede entregar directamente (listo para entrega).
    public bool EsEntregable => PuedeEntregarse;
}

public class CambiarPasswordRequest
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNuevo { get; set; } = string.Empty;
}

public class CambiarEstadoRequest
{
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Item en el carrito del POS (en memoria).</summary>
public class ItemCarrito : ObservableObject
{
    private ProductoResponse producto = null!;
    private int cantidad;

    public ProductoResponse Producto
    {
        get => producto;
        set
        {
            if (SetProperty(ref producto, value))
            {
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    public int Cantidad
    {
        get => cantidad;
        set
        {
            if (SetProperty(ref cantidad, value))
            {
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    public double Subtotal => Producto.Precio * Cantidad;
}

/// <summary>Foto tomada con la cámara y adjuntada a un pedido.</summary>
public class FotoAdjunta
{
    public string RutaLocal { get; set; } = string.Empty;
    public DateTime FechaToma { get; set; } = DateTime.Now;
    public string NombreArchivo => System.IO.Path.GetFileName(RutaLocal);
}

// ── Caja / Turnos ───────────────────────────────────────────────
public class AperturaCajaRequest
{
    public decimal MontoInicial { get; set; }
    public string UsuarioApertura { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class CierreCajaRequest
{
    public decimal MontoDeclaradoCierre { get; set; }
    public string UsuarioCierre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class CajaTurnoResponse
{
    public int Idcaja { get; set; }
    public DateTime? FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal? MontoDeclaradoCierre { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string UsuarioApertura { get; set; } = string.Empty;
    public string? UsuarioCierre { get; set; }
    public string? Observaciones { get; set; }
}

// ── Grupo de comedores (agrupación para UI) ─────────────────────
public class GrupoComedor : List<MesaResponse>
{
    public string Nombre { get; }
    public GrupoComedor(string nombre, IEnumerable<MesaResponse> items) : base(items)
        => Nombre = nombre;
}

public class MesaVisual
{
    public MesaResponse Mesa { get; init; } = null!;
    public List<PedidoResponse> PedidosActivos { get; init; } = [];

    public int Idmesa => Mesa.Idmesa;
    public int Numero => Mesa.Numero;
    public int Capacidad => Mesa.Capacidad;
    public string NombreComedor => Mesa.NombreComedor ?? "Sin comedor";
    public string NumeroTexto => Mesa.Numero.ToString();
    public int CantidadPedidos => PedidosActivos.Count;
    public int PedidosListos => PedidosActivos.Count(p => p.EstaListoParaEntrega);

    public string EstadoVisual
    {
        get
        {
            if (PedidosActivos.Any(p => p.Estado == PedidoEstados.Entregado)) return "Pendiente de pago";
            if (PedidosActivos.Any(p => p.EstaListoParaEntrega)) return "Lista para entregar";
            if (PedidosActivos.Any(p => p.EstaEnPreparacion)) return "En preparacion";
            if (PedidosActivos.Count > 0 || !Mesa.Estado) return "Ocupada";
            return "Disponible";
        }
    }

    public string EstadoColor => EstadoVisual switch
    {
        "Disponible"            => "#28b779",
        "Ocupada"               => "#0ea5e9",
        "En preparacion"        => "#f59e0b",
        "Lista para entregar"   => "#ef4444",
        "Pendiente de pago"     => "#8b5cf6",
        _                       => "#6b7280"
    };

    public string EstadoIcono => EstadoVisual switch
    {
        "En preparacion"      => "♨",
        "Lista para entregar" => "🔔",
        "Pendiente de pago"   => "$",
        _                     => string.Empty
    };

    public string PedidoBadgeTexto => PedidosListos > 0
        ? $"Listo ({PedidosListos})"
        : CantidadPedidos > 0 ? CantidadPedidos.ToString() : string.Empty;
}

public class GrupoMesaVisual : List<MesaVisual>
{
    public string Nombre { get; }
    public int TotalMesas => Count;

    public GrupoMesaVisual(string nombre, IEnumerable<MesaVisual> items) : base(items)
        => Nombre = nombre;
}

public class MesaUpdateRequest
{
    public int Numero { get; set; }
    public int Capacidad { get; set; }
    public bool Estado { get; set; }
    public int? Idcomedor { get; set; }
}

// ── Cuentas / Facturación ─────────────────────────────────────────
public class CuentaResponse
{
    public int Idcuenta { get; set; }
    public DateTime? FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string Estado { get; set; } = string.Empty;
    public double Total { get; set; }
    public MesaResponse? Mesa { get; set; }
    public ClienteResponse? Cliente { get; set; }

    public Color EstadoBadgeColor => Estado switch
    {
        CuentaEstados.Abierta  => Color.FromArgb("#28b779"),
        "CERRADA"  => Color.FromArgb("#6b7280"),
        CuentaEstados.Anulada  => Color.FromArgb("#e94560"),
        _          => Color.FromArgb("#fb8c00")
    };
    public string MesaTexto => Mesa is not null ? $"Mesa {Mesa.Numero}" : "Sin mesa";
    public string ClienteTexto => Cliente?.Nombre ?? "Sin cliente";
}

public class CuentaRequest
{
    public int IdMesa { get; set; }
    public int IdCliente { get; set; }
    public double Total { get; set; }
}

public class PagoRequest
{
    public double Monto { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

public class PagoResponse
{
    public int Idpago { get; set; }
    public DateTime Fecha { get; set; }
    public double Monto { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int Idcuenta { get; set; }
    public double TotalPagadoCuenta { get; set; }
    public double SaldoPendienteCuenta { get; set; }
}

public class SaldoCuentaResponse
{
    public int Idcuenta { get; set; }
    public double TotalPagado { get; set; }
    public double SaldoPendiente { get; set; }
}

// ── Reportes / Admin ──────────────────────────────────────────────
public class ResumenProductoVenta
{
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int CantidadVendida { get; set; }
    public double TotalVendido { get; set; }
}

public class ReporteVentasDia
{
    public DateTime Fecha { get; set; }
    public double TotalVentas { get; set; }
    public int NumeroPedidos { get; set; }
    public double TicketPromedio { get; set; }
    public double TotalEfectivo { get; set; }
    public double TotalTarjeta { get; set; }
    public double TotalTransferencias { get; set; }
    public double TotalOtros { get; set; }
    public int TotalProductos { get; set; }
    public List<ResumenProductoVenta> Productos { get; set; } = new();
}

// ── WebSocket / Notificaciones en tiempo real ─────────────────────

/// <summary>Payload recibido desde el topic STOMP /topic/camarero o /topic/cocina.</summary>
public class NotificacionPedidoWs
{
    public int      PedidoId    { get; set; }
    public string   Evento      { get; set; } = string.Empty;  // CONFIRMAR|LISTO|ENTREGADO|CANCELADO
    public string   EstadoNuevo { get; set; } = string.Empty;
    public string   Mensaje     { get; set; } = string.Empty;
    public string   Emisor      { get; set; } = string.Empty;
    public DateTime Fecha       { get; set; }
}

/// <summary>
/// Notificación visible en el historial de la pantalla de notificaciones.
/// Instancias creadas por NotificationService y leídas por NotificacionesViewModel.
/// </summary>
public class Notificacion
{
    public int      Id          { get; set; }
    public string   Titulo      { get; set; } = string.Empty;
    public string   Descripcion { get; set; } = string.Empty;
    public string   Tipo        { get; set; } = string.Empty; // PEDIDO, PAGO, CAJA, SISTEMA
    public DateTime Fecha       { get; set; }
    public bool     Leida       { get; set; }
    public string?  Accion      { get; set; }

    public string TiempoTexto
    {
        get
        {
            if (Fecha == default) return "";
            var diff = DateTime.Now - Fecha;
            if (diff.TotalMinutes < 1) return "ahora mismo";
            if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours  < 24) return $"hace {(int)diff.TotalHours} h";
            return $"hace {(int)diff.TotalDays} días";
        }
    }

    public string IconoTipo => Tipo switch
    {
        "PEDIDO"  => "🍽",
        "PAGO"    => "💳",
        "CAJA"    => "🏦",
        "SISTEMA" => "⚙️",
        _         => "🔔"
    };
}

/// <summary>Respuesta del backend al subir un comprobante de pago.</summary>
public class ComprobanteResponse
{
    public int      Idcomprobante   { get; set; }
    public int      Idpago          { get; set; }
    public string   NombreArchivo   { get; set; } = string.Empty;
    public string   UrlDescarga     { get; set; } = string.Empty;
    public string   ContentType     { get; set; } = string.Empty;
    public long     Tamano          { get; set; }
    public string   UsuarioRegistro { get; set; } = string.Empty;
    public DateTime FechaSubida     { get; set; }
}

public class DropboxEstadoResponse
{
    public bool   Disponible { get; set; }
    public string Mensaje    { get; set; } = string.Empty;
}
