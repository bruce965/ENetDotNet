using System.Collections.Concurrent;

namespace ENetDotNet.Internal;

class Pool<T> where T : class, new()
{
    readonly ConcurrentBag<T> _items = new();

    public static Pool<T> Shared { get; } = new();

    public T Rent()
        => _items.TryTake(out T? item) ? item : new();

    public void Return(T item)
        => _items.Add(item);
}
