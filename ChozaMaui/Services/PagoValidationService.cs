namespace ChozaMaui.Services;

public sealed class PagoValidationService
{
    public PagoMontoParseResult ParsearMonto(string monto)
    {
        var raw = monto.Replace(",", ".");
        var ok = double.TryParse(
            raw,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var valor);

        if (!ok || valor <= 0)
            return PagoMontoParseResult.Invalido("Ingrese un monto válido.");

        return PagoMontoParseResult.Valido(valor);
    }

    public PagoCobroValidationResult ValidarCobro(PagoCobroValidationInput input)
    {
        if (!input.HayPedido)
            return PagoCobroValidationResult.Invalido("No hay pedido para cobrar.");

        if (input.TotalCobro <= 0)
            return PagoCobroValidationResult.Invalido("La cuenta ya no tiene saldo pendiente.");

        if (input.EsMetodoEfectivo)
        {
            if (input.MontoRecibido <= 0)
                return PagoCobroValidationResult.Invalido("Ingresa el monto recibido en efectivo.");

            if (input.MontoRecibido < input.TotalCobro)
            {
                return PagoCobroValidationResult.Invalido(
                    $"Faltan ${input.FaltaPorRecibir:F2} para completar el cobro en efectivo.");
            }
        }

        if (input.EsMetodoTransferencia && !input.TieneComprobante)
        {
            return PagoCobroValidationResult.Invalido(
                "Debes tomar o seleccionar la foto del comprobante de transferencia antes de cobrar.");
        }

        return PagoCobroValidationResult.Valido();
    }
}

public sealed record PagoCobroValidationInput(
    bool HayPedido,
    double TotalCobro,
    bool EsMetodoEfectivo,
    double MontoRecibido,
    double FaltaPorRecibir,
    bool EsMetodoTransferencia,
    bool TieneComprobante);

public sealed record PagoCobroValidationResult(bool EsValido, string Mensaje)
{
    public static PagoCobroValidationResult Valido() => new(true, string.Empty);
    public static PagoCobroValidationResult Invalido(string mensaje) => new(false, mensaje);
}

public sealed record PagoMontoParseResult(bool EsValido, double Monto, string Mensaje)
{
    public static PagoMontoParseResult Valido(double monto) => new(true, monto, string.Empty);
    public static PagoMontoParseResult Invalido(string mensaje) => new(false, 0, mensaje);
}
