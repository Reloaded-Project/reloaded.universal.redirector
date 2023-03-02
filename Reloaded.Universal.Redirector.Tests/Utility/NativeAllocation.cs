using System.Runtime.InteropServices;

namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Utility used to temporarily allocate native memory.
/// </summary>
public unsafe struct NativeAllocation<T> : IDisposable where T : unmanaged
{
    /// <summary>
    /// The allocated item
    /// </summary>
    public T* Value { get; private set; }

    public NativeAllocation() => Value = (T*)NativeMemory.Alloc((nuint)sizeof(T));

    /// <inheritdoc />
    public void Dispose() => NativeMemory.Free(Value);
}