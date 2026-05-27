using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

public class PendingOrderService : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly OrderWorkflowService _orderWorkflow;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private List<PendingOrderDraft>? _pendingOrders;
    private int _pendingCount;

    public int PendingCount
    {
        get => _pendingCount;
        private set => SetProperty(ref _pendingCount, value);
    }

    private string QueueFilePath => Path.Combine(FileSystem.AppDataDirectory, "pedidos-pendientes.json");

    public PendingOrderService(OrderWorkflowService orderWorkflow)
    {
        _orderWorkflow = orderWorkflow;
    }

    public async Task<int> EnqueueAsync(PedidoRequest request, string estadoDestino)
    {
        await EnsureLoadedAsync();

        _pendingOrders!.Add(new PendingOrderDraft
        {
            LocalId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            EstadoDestino = estadoDestino,
            Request = CloneRequest(request)
        });

        await PersistAsync();
        UpdatePendingCount();
        return PendingCount;
    }

    public async Task<int> TrySyncPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync();

        if (_pendingOrders!.Count == 0)
            return 0;

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var synced = 0;
            var snapshot = _pendingOrders.ToList();
            foreach (var draft in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _orderWorkflow.SubmitPedidoAsync(draft.Request, draft.EstadoDestino);
                    _pendingOrders.RemoveAll(item => item.LocalId == draft.LocalId);
                    synced++;
                }
                catch (Exception ex) when (IsRecoverable(ex))
                {
                    break;
                }
            }

            await PersistAsync();
            UpdatePendingCount();
            return synced;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public static bool IsRecoverable(Exception ex)
    {
        return ex is TaskCanceledException
            || ex is IOException
            || ex is HttpRequestException httpEx && httpEx.StatusCode is null;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_pendingOrders is not null)
            return;

        if (!File.Exists(QueueFilePath))
        {
            _pendingOrders = [];
            UpdatePendingCount();
            return;
        }

        await using var stream = File.OpenRead(QueueFilePath);
        _pendingOrders = await JsonSerializer.DeserializeAsync<List<PendingOrderDraft>>(stream, JsonOptions) ?? [];
        UpdatePendingCount();
    }

    private async Task PersistAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(QueueFilePath)!);
        await using var stream = File.Create(QueueFilePath);
        await JsonSerializer.SerializeAsync(stream, _pendingOrders, JsonOptions);
    }

    private void UpdatePendingCount()
        => PendingCount = _pendingOrders?.Count ?? 0;

    private static PedidoRequest CloneRequest(PedidoRequest request)
    {
        return new PedidoRequest
        {
            Fecha = request.Fecha,
            IdCliente = request.IdCliente,
            IdMesa = request.IdMesa,
            IdUsuario = request.IdUsuario,
            Observaciones = request.Observaciones,
            Detalles = request.Detalles
                .Select(item => new PedidoDetalleRequest
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                })
                .ToList()
        };
    }
}