namespace ChozaMaui.Services;

public sealed class RoleCapabilityService
{
    public bool PuedeCrearPedido(string? rol)
        => EsUnoDe(rol, "ADMIN", "CAMARERO", "CAJERO");

    public bool PuedeConfirmarPedido(string? rol)
        => EsUnoDe(rol, "ADMIN", "CAMARERO", "CAJERO");

    public bool PuedeIniciarPreparacion(string? rol)
        => EsUnoDe(rol, "ADMIN", "COCINA");

    public bool PuedeMarcarPedidoListo(string? rol)
        => EsUnoDe(rol, "ADMIN", "COCINA");

    public bool PuedeEntregarPedido(string? rol)
        => EsUnoDe(rol, "ADMIN", "CAMARERO");

    public bool PuedeCancelarPedido(string? rol)
        => EsUnoDe(rol, "ADMIN");

    public bool PuedeCobrarCuenta(string? rol)
        => EsUnoDe(rol, "ADMIN", "CAJERO");

    public bool PuedeGestionarCaja(string? rol)
        => EsUnoDe(rol, "ADMIN", "CAJERO");

    public bool PuedeCerrarMesa(string? rol)
        => EsUnoDe(rol, "ADMIN");

    private static bool EsUnoDe(string? rol, params string[] rolesPermitidos)
        => rolesPermitidos.Any(r => string.Equals(rol, r, StringComparison.OrdinalIgnoreCase));
}
