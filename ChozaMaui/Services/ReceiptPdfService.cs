using ChozaMaui.Models;

#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
#endif

namespace ChozaMaui.Services;

public class ReceiptPdfService
{
    public async Task<string> GenerarReciboPedidoAsync(PedidoResponse pedido, string mesero)
    {
        var nombre = $"recibo_pedido_{pedido.Idpedido}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var ruta = System.IO.Path.Combine(FileSystem.CacheDirectory, nombre);

#if ANDROID
        await Task.Run(() => GenerarPdfAndroid(pedido, mesero, ruta));
        return ruta;
#else
        throw new PlatformNotSupportedException("La generación de PDF está implementada para Android.");
#endif
    }

#if ANDROID
    private static void GenerarPdfAndroid(PedidoResponse pedido, string mesero, string ruta)
    {
        const int ancho = 420;
        const int alto = 720;
        const int margen = 28;

        using var documento = new PdfDocument();
        var paginaInfo = new PdfDocument.PageInfo.Builder(ancho, alto, 1).Create();
        var pagina = documento.StartPage(paginaInfo)!;
        var canvas = pagina.Canvas!;

        using var tituloPaint = new AndroidPaint { Color = AndroidColor.Rgb(17, 24, 39), TextSize = 22, FakeBoldText = true };
        using var subtituloPaint = new AndroidPaint { Color = AndroidColor.Rgb(107, 114, 128), TextSize = 11 };
        using var textoPaint = new AndroidPaint { Color = AndroidColor.Rgb(31, 41, 55), TextSize = 12 };
        using var boldPaint = new AndroidPaint { Color = AndroidColor.Rgb(17, 24, 39), TextSize = 13, FakeBoldText = true };
        using var totalPaint = new AndroidPaint { Color = AndroidColor.Rgb(244, 81, 30), TextSize = 18, FakeBoldText = true };
        using var linePaint = new AndroidPaint { Color = AndroidColor.Rgb(229, 231, 235), StrokeWidth = 1 };

        var y = 38;
        canvas.DrawText("LA CHOZA", margen, y, tituloPaint);
        y += 18;
        canvas.DrawText("Recibo de consumo", margen, y, subtituloPaint);
        y += 26;
        canvas.DrawLine(margen, y, ancho - margen, y, linePaint);
        y += 24;

        DrawLabel(canvas, "Pedido", $"#{pedido.Idpedido}", margen, ref y, boldPaint, textoPaint);
        DrawLabel(canvas, "Mesa", pedido.Mesa?.Etiqueta ?? "Sin mesa", margen, ref y, boldPaint, textoPaint);
        DrawLabel(canvas, "Cliente", pedido.Cliente?.NombreCompleto ?? "Consumidor final", margen, ref y, boldPaint, textoPaint);
        DrawLabel(canvas, "Mesero", string.IsNullOrWhiteSpace(mesero) ? "No registrado" : mesero, margen, ref y, boldPaint, textoPaint);
        DrawLabel(canvas, "Fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), margen, ref y, boldPaint, textoPaint);

        y += 8;
        canvas.DrawLine(margen, y, ancho - margen, y, linePaint);
        y += 24;
        canvas.DrawText("Detalle", margen, y, boldPaint);
        y += 20;

        foreach (var item in pedido.Detalle ?? [])
        {
            var nombre = item.Producto?.Nombre ?? "Producto";
            canvas.DrawText($"{item.Cantidad}x {Truncar(nombre, 25)}", margen, y, textoPaint);
            canvas.DrawText($"${item.Subtotal:0.00}", ancho - margen - 74, y, textoPaint);
            y += 18;

            if (y > alto - 90)
                break;
        }

        y += 8;
        canvas.DrawLine(margen, y, ancho - margen, y, linePaint);
        y += 28;
        canvas.DrawText("TOTAL", margen, y, boldPaint);
        canvas.DrawText($"${pedido.Total:0.00}", ancho - margen - 95, y, totalPaint);

        y += 44;
        canvas.DrawText("Gracias por su visita.", margen, y, subtituloPaint);
        y += 16;
        canvas.DrawText("Comprobante generado desde Choza POS.", margen, y, subtituloPaint);

        documento.FinishPage(pagina);

        using var stream = File.Create(ruta);
        documento.WriteTo(stream);
    }

    private static void DrawLabel(Canvas canvas, string label, string value, int x, ref int y, AndroidPaint labelPaint, AndroidPaint valuePaint)
    {
        canvas.DrawText($"{label}:", x, y, labelPaint);
        canvas.DrawText(value, x + 78, y, valuePaint);
        y += 18;
    }

    private static string Truncar(string texto, int max)
        => texto.Length <= max ? texto : texto[..Math.Max(0, max - 3)] + "...";
#endif
}
