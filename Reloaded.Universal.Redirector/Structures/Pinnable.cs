using System.Runtime.CompilerServices;

namespace Reloaded.Universal.Redirector.Structures;

/// <summary>
/// Allows you to pin a native unmanaged object in a static location in memory, to be
/// later accessible from native code.
/// </summary>
public unsafe class Pinnable<T> : IDisposable where T : unmanaged
{
    /// <summary>
    /// Pointer to the native value in question.
    /// If the class was instantiated using an array, this is the pointer to the first element of the array.
    /// </summary>
    public T* Pointer { get; private set; }

    private T[] _pohReference = null!;
    
    /* Constructor/Destructor */
    // Note: GCHandle.Alloc causes boxing(due to conversion to object), meaning our item is stored on the heap.
    // This means that for value types, we do not need to store them explicitly.

    /// <summary>
    /// Pins a value to the heap. 
    /// </summary>
    /// <param name="value">The value to be pinned on the heap.</param>
    public Pinnable(T value)
    {
        InitFromReference(ref value);
    }

    private void InitFromReference(ref T value)
    {
        _pohReference = GC.AllocateUninitializedArray<T>(1, true);
        Pointer = (T*)Unsafe.AsPointer(ref _pohReference[0]);
        *Pointer = value;
    }

    /// <summary>Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.</summary>
    ~Pinnable() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        _pohReference = null!;
        GC.SuppressFinalize(this);
    }
}