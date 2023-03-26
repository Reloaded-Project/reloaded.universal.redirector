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
    /// <param name="isDirectory">True if resulting item is a directory, else false.</param>
    /// <returns>True if the path is resolved, else false if not found or is a directory.</returns>
    public bool TryResolveFilePath(ReadOnlySpan<char> originalPath, out string result, out bool isDirectory)
    {
        var man = GetManager();
        bool success = man.TryGetFile(originalPath, out var redir);
        if (!success)
        {
            isDirectory = default;
            result = string.Empty;
            return false;
        }
        
        isDirectory = redir.IsDirectory;
        result = redir.GetFullPathWithDevicePrefix();
        return success;
    }

    /// <summary>
    /// Attempts to resolve the given path from source to target.
    /// </summary>
    /// <param name="originalPath">The original path to resolve.</param>
    /// <param name="result">The resulting path, including prefix <see cref="Strings.PrefixLocalDeviceStr"/>.</param>
    /// <param name="isDirectory">True if resulting item is a directory, else false.</param>
    /// <returns>True if the path is resolved, else false if not found or is a directory.</returns>
    public unsafe bool TryResolveFilePath(Native.OBJECT_ATTRIBUTES* objectAttributes, out string result, out bool isDirectory)
    {
        return TryResolveFilePath(ExtractPathFromObjectAttributes(objectAttributes), out result, out isDirectory);
    }

    /// <summary>
    /// Extracts a full file path from the given object attributes.
    /// </summary>
    public unsafe ReadOnlySpan<char> ExtractPathFromObjectAttributes(Native.OBJECT_ATTRIBUTES* objectAttributes)
    {
        if (!objectAttributes->TryGetRootDirectory(out string result))
        {
            var str = objectAttributes->ObjectName->ToSpan().TrimWindowsPrefixes();
            if (str.Length == 0)
                return default;
            
            return str.ToString().NormalizePath();
        }

        var joined = Path.Join(result.AsSpan(), objectAttributes->ObjectName->ToSpan()).AsSpan();
        if (joined.Length == 0)
            return default;
        joined = joined.TrimWindowsPrefixes();
        return joined.ToString().NormalizePath();
    }
    
    private void PrintFileLoadIfNeeded(ReadOnlySpan<char> path)
    {
        if (!_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintOpen) || _logger == null) 
            return;
        
        if (_redirectorApi.GetRedirectorSetting(RedirectorSettings.DontPrintNonFiles))
        {
            if (path.Contains("usb#vid", StringComparison.OrdinalIgnoreCase) || path.Contains("hid#vid", StringComparison.OrdinalIgnoreCase))
                return;
        }

        _logger.Info("[R2.Redirector] File Open {0}", path.ToString());
    }
    
    private void PrintGetAttributeIfNeeded(ReadOnlySpan<char> path)
    {
        if (!_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintGetAttributes) || _logger == null) 
            return;
        
        _logger.Info("[R2.Redirector] File Get Attributes {0}", path.ToString());
    }
    
    private void PrintDirectoryGetAttributeIfNeeded(ReadOnlySpan<char> path)
    {
        if (!_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintGetAttributes) || _logger == null) 
            return;
        
        _logger.Info("[R2.Redirector] Directory Get Attributes {0}", path.ToString());
    }

    private void PrintAttributeRedirectIfNeeded(ReadOnlySpan<char> path, string newFilePath)
    {
        if (_redirectorApi.GetRedirectorSetting(RedirectorSettings.PrintGetAttributes) && _logger != null)
            _logger.Info("[R2.Redirector] Attribute Redirect {0}\n-> {1}", path.ToString(), newFilePath);
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
    // ReSharper disable once UnusedMethodReturnValue.Local
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
    
    // ReSharper disable UnusedMember.Local
    [Conditional("DEBUG")]  
    private void LogDebugOnly(string message) => _logger?.Debug(message);

    [Conditional("DEBUG")]  
    private void LogDebugOnly<T>(string format, T itemOne) => _logger?.Debug(format, itemOne);
    
    [Conditional("DEBUG")]  
    private void LogDebugOnly<T, T2>(string format, T itemOne, T2 itemTwo) => _logger?.Debug(format, itemOne, itemTwo);
    
    [Conditional("DEBUG")]  
    private void LogDebugOnly<T, T2, T3>(string format, T itemOne, T2 itemTwo, T3 itemThree) => _logger?.Debug(format, itemOne, itemTwo, itemThree);
    
    [Conditional("DEBUG")]
    private void LogFatalError(string methodName, Exception ex)
    {
        _logger?.Fatal($"Exception thrown in {methodName}\n" +
                       $"Message: {ex.Message}\n" +
                       $"Stack: {ex.StackTrace}");
    }
    // ReSharper restore UnusedMember.Local
}