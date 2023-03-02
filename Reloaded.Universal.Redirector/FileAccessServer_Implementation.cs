using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;

namespace Reloaded.Universal.Redirector;

// Contains the parts of FileAccessServer responsible for the common logic.
public partial class FileAccessServer
{
    /// <summary>
    /// Attempts to resolve the given path from source to target.
    /// </summary>
    /// <param name="originalPath">The original path to resolve.</param>
    /// <param name="result">The resulting path, including prefix <see cref="Strings.PrefixLocalDeviceStr"/>.</param>
    /// <returns>True if the path is resolved, else false.</returns>
    public bool TryResolvePath(ReadOnlySpan<char> originalPath, out string result)
    {
        var man = GetManager();
        bool success = man.TryGetFile(originalPath, out var redir);
        result = redir.GetFullPathWithDevicePrefix();
        return success;
    }

    /// <summary>
    /// Attempts to resolve the given path from source to target.
    /// </summary>
    /// <param name="objectAttributes">Attributes containing the original path to resolve.</param>
    /// <param name="result">The resulting path.</param>
    /// <returns>True if the path is resolved, else false.</returns>
    public unsafe bool TryResolvePath(Native.OBJECT_ATTRIBUTES* objectAttributes, out string result)
    {
        return TryResolvePath(ExtractPathFromObjectAttributes(objectAttributes), out result);
    }

    /// <summary>
    /// Extracts a full file path from the given object attributes.
    /// </summary>
    public unsafe ReadOnlySpan<char> ExtractPathFromObjectAttributes(Native.OBJECT_ATTRIBUTES* objectAttributes)
    {
        if (!objectAttributes->TryGetRootDirectory(out string result))
            return objectAttributes->ObjectName->ToSpan().TrimWindowsPrefixes().ToString().NormalizePath();

        var joined = Path.Join(result.AsSpan(), objectAttributes->ObjectName->ToSpan());
        return joined.AsSpan().TrimWindowsPrefixes().ToString().NormalizePath();
    }
}