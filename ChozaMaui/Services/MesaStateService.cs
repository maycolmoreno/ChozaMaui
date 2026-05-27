using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class MesaStateService
{
    private static readonly TimeSpan MesasCacheTtl = TimeSpan.FromSeconds(10);
    private const string MesasCacheKey = "mesas:all";

    private readonly MesaApiService _mesasApi;
    private readonly SessionCacheService _cache;

    public MesaStateService(MesaApiService mesasApi, SessionCacheService cache)
    {
        _mesasApi = mesasApi;
        _cache = cache;
    }

    public Task<IReadOnlyList<MesaResponse>> ObtenerMesasAsync()
        => _cache.GetOrCreateAsync(
            MesasCacheKey,
            MesasCacheTtl,
            ObtenerMesasDesdeApiAsync);

    public async Task<MesaResponse> ActualizarEstadoMesaAsync(MesaResponse mesa, bool nuevoEstado)
    {
        var actualizada = await _mesasApi.ActualizarEstadoMesaAsync(mesa, nuevoEstado);
        await InvalidarAsync();
        return actualizada;
    }

    public Task InvalidarAsync() => _cache.RemoveAsync(MesasCacheKey);

    private async Task<IReadOnlyList<MesaResponse>> ObtenerMesasDesdeApiAsync()
        => await _mesasApi.ObtenerMesasAsync();
}