using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosMediaService
{
    private readonly PhotoCaptureService _photoCaptureService;
    private readonly ReceiptPdfService _receiptPdfService;

    public PosMediaService(PhotoCaptureService photoCaptureService, ReceiptPdfService receiptPdfService)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _photoCaptureService = photoCaptureService;
        _receiptPdfService = receiptPdfService;
        System.Diagnostics.Debug.WriteLine($"[PERF][PosMediaService] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    public Task<FotoAdjunta?> CapturarFotoAsync()
        => _photoCaptureService.CapturarFotoAsync();

    public async Task CompartirReciboAsync(PedidoResponse pedido, string mesero)
    {
        var rutaPdf = await _receiptPdfService.GenerarReciboPedidoAsync(pedido, mesero);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Recibo pedido #{pedido.Idpedido}",
            File = new ShareFile(rutaPdf, "application/pdf")
        });
    }
}
