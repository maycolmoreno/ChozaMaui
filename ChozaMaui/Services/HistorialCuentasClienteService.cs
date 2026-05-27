using System.Text.RegularExpressions;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class HistorialCuentasClienteService
{
    private readonly ClienteApiService _clientesApi;
    private readonly CuentaApiService _cuentasApi;

    public HistorialCuentasClienteService(ClienteApiService clientesApi, CuentaApiService cuentasApi)
    {
        _clientesApi = clientesApi;
        _cuentasApi = cuentasApi;
    }

    public Task<List<ClienteResponse>> CargarClientesAsync()
        => _clientesApi.GetClientesAsync();

    public IReadOnlyList<ClienteResponse> FiltrarClientes(IEnumerable<ClienteResponse> clientes, string termino)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
            return [];

        var lower = termino.ToLowerInvariant();
        return clientes
            .Where(c => c.Nombre.ToLowerInvariant().Contains(lower) ||
                        (!string.IsNullOrWhiteSpace(c.Cedula) && c.Cedula.Contains(termino)))
            .Take(20)
            .ToList();
    }

    public HistorialClienteValidationResult ValidarNuevoCliente(string nombre, string cedula, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return HistorialClienteValidationResult.Invalido("El nombre es obligatorio.");

        if (!Regex.IsMatch(cedula.Trim(), @"^\d{10,13}$"))
            return HistorialClienteValidationResult.Invalido("La cédula debe tener entre 10 y 13 dígitos.");

        if (!string.IsNullOrEmpty(telefono) && !Regex.IsMatch(telefono.Trim(), @"^\d{10}$"))
            return HistorialClienteValidationResult.Invalido("El teléfono debe tener 10 dígitos.");

        return HistorialClienteValidationResult.Valido();
    }

    public Task<CuentaResponse> AsignarClienteAsync(int idCuenta, int idCliente)
        => _cuentasApi.AsignarClienteCuentaAsync(idCuenta, idCliente);

    public Task<ClienteResponse> CrearClienteAsync(string nombre, string cedula, string telefono)
    {
        return _clientesApi.CrearClienteAsync(new ClienteRequest
        {
            Nombre = nombre.Trim(),
            Cedula = cedula.Trim(),
            Telefono = string.IsNullOrEmpty(telefono) ? null : telefono.Trim(),
            Estado = true
        });
    }
}

public sealed record HistorialClienteValidationResult(bool EsValido, string Error)
{
    public static HistorialClienteValidationResult Valido() => new(true, string.Empty);
    public static HistorialClienteValidationResult Invalido(string error) => new(false, error);
}