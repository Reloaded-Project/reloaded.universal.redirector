using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    /// <summary>
    /// Represents a singular unicode string.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        private IntPtr buffer;

        /// <summary/>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        public unsafe UNICODE_STRING(char* pointer, int length) => Create(ref this, pointer, length);

        /// <summary/>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        /// <param name="attributes">The attributes to write the string to.</param>
        public unsafe UNICODE_STRING(char* pointer, int length, OBJECT_ATTRIBUTES* attributes) =>
            Create(ref this, pointer, length, attributes);

        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Create(ref UNICODE_STRING item, char* pointer, int length)
        {
            item.Length = (ushort)(length * 2);
            item.MaximumLength = (ushort)(item.Length + 2);
            item.buffer = (IntPtr)pointer;
        }

        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        /// <param name="attributes">The attributes to write the string to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Create(ref UNICODE_STRING item, char* pointer, int length,
            OBJECT_ATTRIBUTES* attributes)
        {
            Create(ref item, pointer, length);
            attributes->ObjectName = (UNICODE_STRING*)Unsafe.AsPointer(ref item);
            attributes->RootDirectory = IntPtr.Zero;
        }

        /// <summary>
        /// Returns a string with the contents
        /// </summary>
        /// <returns></returns>
        public unsafe ReadOnlySpan<char> ToSpan()
        {
            if (buffer != IntPtr.Zero)
                return new ReadOnlySpan<char>((char*)buffer, Length / sizeof(char));

            return default;
        }
    }
}