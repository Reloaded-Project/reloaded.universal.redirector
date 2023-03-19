using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Reloaded.Hooks.Definitions.Helpers;
using Reloaded.Universal.Redirector.Interfaces;
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
    
    private void PrintFileLoadIfNeeded(ReadOnlySpan<char> path)
    {
        if (!_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintOpen) || _logger == null) 
            return;
        
        if (_redirectorApi.GetRedirectorSetting(RedirectorSettings.DontPrintNonFiles))
        {
            if (path.Contains("usb#vid", StringComparison.Ordinal) || path.Contains("hid#vid", StringComparison.Ordinal))
                return;
        }

        _logger.Info("[R2.Redirector] File Open {0}", path.ToString());
    }

    private void PrintFileRedirectIfNeeded(ReadOnlySpan<char> path, string newFilePath)
    {
        if (_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintRedirect) && _logger != null)
            _logger.Info("[R2.Redirector] File Redirect {0}\n-> {1}", path.ToString(), newFilePath);
    }
    
    /// <summary>
    /// Forces the JIT to compile a given function.
    /// </summary>
    /// <param name="type">Type inside which the function is contained.</param>
    /// <param name="name">Name of the function.</param>
    /// <returns>Pointer to the function.</returns>
    private unsafe void* JitFunction(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.Methods)]
#endif
        Type type, string name)
    {
        _logger?.Debug("Jitting Function: {0}", name);
        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!.MethodHandle;
        RuntimeHelpers.PrepareMethod(method);
        return (void*) method.GetFunctionPointer();
    }
    
    [Conditional("DEBUG")]  
    private void LogDebugOnly<T>(string message) => _logger?.Debug(message);

    [Conditional("DEBUG")]  
    private void LogDebugOnly<T>(string format, T itemOne) => _logger?.Debug(format, itemOne);
    
    [Conditional("DEBUG")]  
    private void LogDebugOnly<T, T2>(string format, T itemOne, T2 itemTwo) => _logger?.Debug(format, itemOne, itemTwo);
    
    [Conditional("DEBUG")]  
    private void LogDebugOnly<T, T2, T3>(string format, T itemOne, T2 itemTwo, T3 itemThree) => _logger?.Debug(format, itemOne, itemTwo, itemThree);
}