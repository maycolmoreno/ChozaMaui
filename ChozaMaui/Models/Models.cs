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
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public double Total { get; set; }
    public int CantidadProductos { get; set; }
    public UsuarioResponse? Usuario { get; set; }
    public MesaResponse? Mesa { get; set; }
    public ClienteResponse? Cliente { get; set; }
    public List<PedidoDetalleResponse>? Detalle { get; set; }

    public string EstadoBadgeColor => Estado switch
    {
        "PENDIENTE"  => "#f59e0b",
        "EN_PROCESO" => "#3b82f6",
        "LISTO"      => "#10b981",
        "ENTREGADO"  => "#6b7280",
        "CANCELADO"  => "#ef4444",
        _ => "#6b7280"
    };

    public string EstadoBorderColor => EstadoBadgeColor;
    public string EstadoBadgeBackground => EstadoBadgeColor;

    public double BarraProgreso => Estado switch
    {
        "PENDIENTE"  => 0.15,
        "EN_PROCESO" => 0.60,
        "LISTO"      => 1.0,
        "ENTREGADO"  => 1.0,
        "CANCELADO"  => 0.0,
        _ => 0
    };

    public string BarraProgresoColor => EstadoBadgeColor;

    public string EtapaActualLabel => Estado switch
    {
        "PENDIENTE"  => "RECIBIDO",
        "EN_PROCESO" => "PREPARANDO",
        "LISTO"      => "LISTO ✓",
        "ENTREGADO"  => "ENTREGADO ✓",
        "CANCELADO"  => "CANCELADO",
        _ => "---"
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
    public bool EsCobrable => Estado == "LISTO" || Estado == "ENTREGADO" ||
                              Estado == "LISTO_PARA_ENTREGA" || Estado == "COMPLETADO";
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
                OnPropertyChanged(nameof(Etiqueta));
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
                OnPropertyChanged(nameof(Etiqueta));
            }
        }
    }

    public double Subtotal => Producto.Precio * Cantidad;
    public string Etiqueta => $"{Producto.Nombre}  x{Cantidad}  ${Subtotal:0.00}";
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
        "ABIERTA"  => Color.FromArgb("#28b779"),
        "CERRADA"  => Color.FromArgb("#6b7280"),
        "ANULADA"  => Color.FromArgb("#e94560"),
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
    public int TotalProductos { get; set; }
    public List<ResumenProductoVenta> Productos { get; set; } = new();
}
