using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosClientService
{
    private readonly ClienteApiService _clientesApi;

    public PosClientService(ClienteApiService clientesApi)
    {
        _clientesApi = clientesApi;
    }

    public async Task<PosClientSelectionResult> SeleccionarClienteAsync()
    {
        var clientes = await _clientesApi.GetClientesAsync();
        if (clientes.Count == 0)
            return PosClientSelectionResult.SinResultados("No hay clientes registrados.");

        var shell = Shell.Current ?? throw new InvalidOperationException("La navegación no está disponible.");
        var opciones = clientes.Select(c => $"{c.Nombre}  ({c.Cedula})").ToArray();
        var elegida = await shell.DisplayActionSheetAsync("Seleccionar cliente", "Cancelar", null, opciones);

        if (string.IsNullOrWhiteSpace(elegida) || elegida == "Cancelar")
            return PosClientSelectionResult.CanceladoPorUsuario();

        var idx = Array.IndexOf(opciones, elegida);
        return idx >= 0
            ? PosClientSelectionResult.Seleccionado(clientes[idx])
            : PosClientSelectionResult.CanceladoPorUsuario();
    }

    public async Task<PosClientCreationResult> CrearClienteRapidoAsync()
    {
        var shell = Shell.Current ?? throw new InvalidOperationException("La navegación no está disponible.");

        var nombre = await shell.DisplayPromptAsync(
            "Nuevo cliente",
            "Nombre del cliente",
            accept: "Siguiente",
            cancel: "Cancelar",
            placeholder: "Ej: Juan Pérez");

        if (string.IsNullOrWhiteSpace(nombre))
            return PosClientCreationResult.CanceladoPorUsuario();

        var cedula = await shell.DisplayPromptAsync(
            "Nuevo cliente",
            "Cédula del cliente",
            accept: "Crear",
            cancel: "Cancelar",
            placeholder: "Ej: 0102030405",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(cedula))
            return PosClientCreationResult.CanceladoPorUsuario();

        var cliente = await _clientesApi.CrearClienteAsync(new ClienteRequest
        {
            Nombre = nombre.Trim(),
            Cedula = cedula.Trim(),
            Estado = true
        });

        return PosClientCreationResult.Creado(cliente);
    }
}

public sealed record PosClientSelectionResult(ClienteResponse? Cliente, bool Cancelado, string? Mensaje)
{
    public static PosClientSelectionResult Seleccionado(ClienteResponse cliente) => new(cliente, false, null);
    public static PosClientSelectionResult CanceladoPorUsuario() => new(null, true, null);
    public static PosClientSelectionResult SinResultados(string mensaje) => new(null, false, mensaje);
}

public sealed record PosClientCreationResult(ClienteResponse? Cliente, bool Cancelado)
{
    public static PosClientCreationResult Creado(ClienteResponse cliente) => new(cliente, false);
    public static PosClientCreationResult CanceladoPorUsuario() => new(null, true);
}