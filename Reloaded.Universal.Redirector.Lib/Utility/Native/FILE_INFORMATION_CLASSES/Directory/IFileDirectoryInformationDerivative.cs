// ReSharper disable once CheckNamespace
// ReSharper disable CheckNamespace

using System.Runtime.CompilerServices;

#pragma warning disable CS1591
namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

/// <summary>
/// Constraint used for derivatives of <see cref="Native.FILE_DIRECTORY_INFORMATION"/>.
/// </summary>
public unsafe interface IFileDirectoryInformationDerivative
{
    /// <summary>
    /// Returns the offset of next entry from this structure.
    /// </summary>
    public int GetNextEntryOffset();
    
    /// <summary>
    /// Sets the offset of next entry to this structure.
    /// </summary>
    public void SetNextEntryOffset(int offset);
    
    /// <summary>
    /// Returns the file attributes from this structure.
    /// </summary>
    public FileAttributes GetFileAttributes();
    
    /// <summary>
    /// Retrieves the file name as a string.
    /// </summary>
    /// <param name="thisPtr">Pointer to 'this' object.</param>
    public ReadOnlySpan<char> GetFileName(void* thisPtr);

    /// <summary>
    /// Tries to populate the item given a handle to a native file to get data from.
    /// </summary>
    /// <param name="thisPtr">Pointer to 'this' item.</param>
    /// <param name="availableSize">Available size in <paramref name="thisPtr"/>.</param>
    /// <param name="handle">Handle to the file.</param>
    /// <returns>
    ///     True if the structure can be populated, false if there isn't enough space to do so.
    /// </returns>
    public static abstract bool TryPopulate(void* thisPtr, int availableSize, nint handle);
}

/// <summary>
/// Extensions for <see cref="IFileDirectoryInformationDerivative"/>.
/// </summary>
public static class FileDirectoryInformationDerivativeExtensions 
{
    /// <summary>
    /// Checks whether there is sufficient size available to create an instance of <see cref="T"/> with a string of the given length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool SufficientSize<T>(int availableSpace, int stringLength) where T : unmanaged, IFileDirectoryInformationDerivative
    {
        return availableSpace > stringLength * sizeof(char) + sizeof(T);
    }

    /// <summary>
    /// Checks whether there is sufficient size available to create an instance of <see cref="T"/> with a string of the given length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyString(ReadOnlySpan<char> source, char* destination, uint numBytes)
    {
        fixed (char* sourcePtr = source)
        {
            Unsafe.CopyBlock(destination, sourcePtr, numBytes);
        }
    }
    
    /// <summary>
    /// Moves the pointer to the next item.
    /// Returns false if there is no next item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool GoToNext<T>(ref T* item) where T : unmanaged, IFileDirectoryInformationDerivative
    {
        var ofs = item->GetNextEntryOffset();
        if (ofs == 0)
            return false;

        item = (T*)((byte*)item + ofs);
        return true;
    }
}