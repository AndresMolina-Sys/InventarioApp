using System.Threading.Channels;

namespace InventarioAppDesktop.Services;

// Cola de trabajos en segundo plano (Channel + workers).
// Los trabajos son best-effort: si la cola está llena se descartan
// (DropWrite) y cualquier excepción se traga para no tumbar la app.
// Uso actual: recalentar la caché tras operaciones de escritura.
public sealed class BackgroundJobQueue : IDisposable
{
    public static readonly BackgroundJobQueue Default = new(workerCount: 2, capacity: 256);

    private readonly Channel<Func<CancellationToken, Task>> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task[] _workers;
    private long _trabajosFallidos;

    // Observabilidad mínima: contador de trabajos fallidos (sin infraestructura de logs).
    public long TrabajosFallidos => Interlocked.Read(ref _trabajosFallidos);

    public void RegistrarFallo() => Interlocked.Increment(ref _trabajosFallidos);

    public BackgroundJobQueue(int workerCount = 2, int capacity = 256)
    {
        _channel = Channel.CreateBounded<Func<CancellationToken, Task>>(
            new BoundedChannelOptions(Math.Max(1, capacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = false,
                SingleWriter = false
            });

        _workers = Enumerable.Range(0, Math.Max(1, workerCount))
            .Select(_ => Task.Run(() => WorkerLoopAsync(_cts.Token)))
            .ToArray();
    }

    public bool TryEnqueue(Func<CancellationToken, Task> job)
        => job != null && _channel.Writer.TryWrite(job);

    private async Task WorkerLoopAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                await job(ct);
            }
            catch
            {
                // Best-effort: un trabajo fallido no debe detener la cola.
                RegistrarFallo();
            }
        }
    }

    public void Shutdown()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();
    }

    public void Dispose() => Shutdown();
}
