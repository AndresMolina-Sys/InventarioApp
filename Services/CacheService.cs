using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace InventarioAppDesktop.Services;

// Caché en memoria (Cache-Aside / Read-Through) con invalidación por keyspace.
// Cada keyspace tiene una versión; invalidar = aumentar la versión, de modo que
// las entradas antiguas dejan de leerse y expiran por TTL / presión de tamaño.
// No se borra por clave individual: es O(1), libre de carreras y suficiente para
// las cargas de trabajo de esta aplicación (un proceso por PC).
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string keyspace, string key, Func<Task<T>> factory, TimeSpan ttl);
    void Invalidate(string keyspace);
}

public sealed class MemoryCacheService : ICacheService
{
    private readonly MemoryCache _cache;
    private readonly ConcurrentDictionary<string, long> _versiones = new();
    private long _aciertos;
    private long _fallos;

    public const int EntradasMaximas = 4096;

    public MemoryCacheService(MemoryCacheOptions? options = null)
        => _cache = new MemoryCache(options ?? new MemoryCacheOptions { SizeLimit = EntradasMaximas });

    // Instancia compartida por toda la aplicación (los ViewModels crean su propio
    // InventarioService, pero todos comparten esta caché vía la instancia por defecto).
    public static readonly ICacheService Default = new MemoryCacheService();

    // Estadísticas de uso (útiles para pruebas de carga y monitoreo).
    public long Aciertos => Interlocked.Read(ref _aciertos);
    public long Fallos => Interlocked.Read(ref _fallos);
    public long TotalLecturas => Aciertos + Fallos;

    public Task<T> GetOrCreateAsync<T>(string keyspace, string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        var version = _versiones.GetOrAdd(keyspace, _ => 1L);
        var cacheKey = $"{keyspace}:{version}:{key}";

        // La caché guarda el valor (T), no la tarea.
        if (_cache.TryGetValue(cacheKey, out object? valor) && valor is T yaCalculado)
        {
            Interlocked.Increment(ref _aciertos);
            return Task.FromResult(yaCalculado);
        }

        Interlocked.Increment(ref _fallos);
        return _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            entry.Size = 1;
            return await factory();
        })!;
    }

    public void Invalidate(string keyspace)
        => _versiones.AddOrUpdate(keyspace, 1L, (_, actual) => actual + 1);
}
