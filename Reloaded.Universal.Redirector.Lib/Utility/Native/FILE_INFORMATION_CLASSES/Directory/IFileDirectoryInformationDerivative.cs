// ReSharper disable once CheckNamespace
// ReSharper disable CheckNamespace
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
    /// Returns the file attributes from this structure.
    /// </summary>
    public FileAttributes GetFileAttributes();
    
    /// <summary>
    /// Retrieves the file name as a string.
    /// </summary>
    /// <param name="thisPtr">Pointer to 'this' object.</param>
    public string GetFileName(void* thisPtr);

    /// <summary>
    /// Tries to populate the item given a handle to a native file to get data from.
    /// </summary>
    /// <param name="thisPtr">Pointer to 'this' item.</param>
    /// <param name="handle">Handle to the file.</param>
    /// <returns>
    ///     True if the structure can be populated, false if there isn't enough space to do so.
    /// </returns>
    public bool TryPopulate(void* thisPtr, nint handle);
}