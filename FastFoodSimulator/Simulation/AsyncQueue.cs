namespace FastFoodSimulator.Simulation;

// FIFO-очередь с асинхронным ожиданием и отменой.
internal sealed class AsyncQueue<T>
{
    private readonly Queue<T> _items = new();
    private readonly SemaphoreSlim _available = new(0);
    private readonly object _sync = new();

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _items.Count;
            }
        }
    }

    public void Enqueue(T item)
    {
        lock (_sync)
        {
            _items.Enqueue(item);
        }

        _available.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        await _available.WaitAsync(cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            return _items.Dequeue();
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_sync)
        {
            return _items.ToArray();
        }
    }
}
