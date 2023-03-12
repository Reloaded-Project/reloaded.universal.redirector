using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Reloaded.Universal.Redirector.Lib.Extensions;

/// <summary>
/// Extension methods tied to spans.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceFast<T>(this Span<T> data, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
    }
    
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SliceFast<T>(this ReadOnlySpan<T> data, int start)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), data.Length - start);
    }
}