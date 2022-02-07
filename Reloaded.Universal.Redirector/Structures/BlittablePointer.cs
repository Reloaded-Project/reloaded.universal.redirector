namespace Reloaded.Universal.Redirector.Structures;

/// <summary>
/// Blittable single level pointer type that you can use with generic types.
/// </summary>
public unsafe struct BlittablePointer<T> where T : unmanaged
{
    /// <summary>
    /// The pointer to the value.
    /// </summary>
    public T* Pointer { get; set; }

    /// <summary>
    /// Creates a blittable pointer
    /// </summary>
    public BlittablePointer(T* pointer) => Pointer = pointer;

    /// <summary/>
    public static implicit operator BlittablePointer<T>(T* operand) => new BlittablePointer<T>(operand);

    /// <summary/>
    public static implicit operator T*(BlittablePointer<T> operand) => operand.Pointer;
}