using System.Buffers;
using System.Runtime.InteropServices;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Allows you to temporarily borrow an array from the <see cref="ArrayPool{T}"/>
/// for allocations larger than
/// </summary>
public struct ArrayRental<T> : IDisposable
{
    private const int MinPooledBytes = 1024;
    
    private T[] _data;
    private int _count;
    private bool _canDispose;

    /// <summary>
    /// Rents an array of bytes from the arraypool.
    /// </summary>
    /// <param name="count">Needed amount of items.</param>
    public unsafe ArrayRental(int count)
    {
#pragma warning disable CS8500
        var numBytes = sizeof(T) * count;
#pragma warning restore CS8500
        _count = count;
        _canDispose = numBytes > MinPooledBytes;
        _data = _canDispose 
            ? ArrayPool<T>.Shared.Rent(count) 
            : GC.AllocateUninitializedArray<T>(count);
    }

    /// <summary>
    /// Exposes the raw underlying array, which will likely
    /// be bigger than the number of elements.
    /// </summary>
    public T[] RawArray => _data;

    /// <summary>
    /// Returns the rented array as a span.
    /// </summary>
    public Span<T> Span => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_data), _count);

    /// <summary>
    /// Exposes the number of elements stored by this structure.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Returns the array to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_canDispose)
            ArrayPool<T>.Shared.Return(_data);
    }
}