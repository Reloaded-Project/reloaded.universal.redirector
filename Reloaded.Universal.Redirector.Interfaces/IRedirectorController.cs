using System;

namespace Reloaded.Universal.Redirector.Interfaces;

public interface IRedirectorControllerV5
{
    /// <summary>
    /// Retrieves the value of the given redirector setting flag.
    /// </summary>
    /// <returns>Current value of the flag.</returns>
    bool GetRedirectorSetting(RedirectorSettings setting);

    /// <summary>
    /// Sets or unsets the given redirector setting flag.
    /// </summary>
    /// <returns>Previous setting.</returns>
    bool SetRedirectorSetting(bool enable, RedirectorSettings setting);
}

/// <summary>
/// Describes various settings that can be enabled for the redirector.
/// </summary>
[Flags]
public enum RedirectorSettings
{
    /// <summary>
    /// Default value.
    /// </summary>
    None = 0,
 
    /// <summary>
    /// Prints when a file redirect is performed.
    /// </summary>
    PrintRedirect = 1 << 0,
    
    /// <summary>
    /// Prints file open operations.
    /// </summary>
    PrintOpen = 1 << 1,

    /// <summary>
    /// Skips printing non-files to the console.
    /// </summary>
    DontPrintNonFiles = 1 << 2,
}

public interface IRedirectorControllerV4
{
    /// <summary>
    /// Temporarily disables the redirector.
    /// </summary>
    void Disable();

    /// <summary>
    /// Re-enables the redirector after a temporary disable.
    /// </summary>
    void Enable();
}

public interface IRedirectorControllerV3
{
    /// <summary>
    /// Adds a folder for file redirection, specifying what folder inside the game directory the folder should map to.
    /// </summary>
    /// <param name="folderPath">The full path of the folder to redirect files to.</param>
    /// <param name="sourceFolder">Folder path relative to game's directory to redirect files from. Path should start with back or forward slash.</param>
    /// <returns>True if the operation succeeds, else false. Operation fails if a folder is already redirected.</returns>
    void AddRedirectFolder(string folderPath, string sourceFolder);

    /// <summary>
    /// Removes a folder to be redirected.
    /// The files inside the folder are mapped relative to the executable directory of the modified application's executable.
    /// </summary>
    /// <returns>True if the operation succeeds, else false.</returns>
    void RemoveRedirectFolder(string folderPath, string sourceFolder);
}

public interface IRedirectorControllerV2
{
    /// <summary>
    /// Adds a folder for file redirection.
    /// The files inside the folder are mapped relative to the executable directory of the modified application's executable.
    /// </summary>
    /// <param name="folderPath">The full path of the folder to redirect files to.</param>
    /// <returns>True if the operation succeeds, else false. Operation fails if a folder is already redirected.</returns>
    void AddRedirectFolder(string folderPath);

    /// <summary>
    /// Removes a folder to be redirected.
    /// The files inside the folder are mapped relative to the executable directory of the modified application's executable.
    /// </summary>
    /// <returns>True if the operation succeeds, else false.</returns>
    void RemoveRedirectFolder(string folderPath);
}

public interface IRedirectorController : IRedirectorControllerV2, IRedirectorControllerV3, IRedirectorControllerV4, IRedirectorControllerV5
{
    // The below APIs are removed because one day this will be rewritten in native code due to GC Transition shenanigans.
    // At that point, these APIs will not be fully supported.
    [Obsolete("This API was removed in Virtual FileSystem Rewrite. If you need this, please let me know.")]
    Redirecting? Redirecting { get; set; }
    
    [Obsolete("This API was removed in Virtual FileSystem Rewrite. If you need this, please let me know.")]
    Loading? Loading { get; set; }

    /// <summary>
    /// Adds a file to be redirected.
    /// </summary>
    /// <param name="oldFilePath">The absolute path of the file to be replaced. Tip: Use Path.GetFullPath()</param>
    /// <param name="newFilePath">The absolute path to the new file. Tip: Use Path.GetFullPath()</param>
    void AddRedirect(string oldFilePath, string newFilePath);

    /// <summary>
    /// Removes a file from being redirected.
    /// </summary>
    /// <param name="oldFilePath">The absolute path of the file to no longer be replaced. Tip: Use Path.GetFullPath()</param>
    void RemoveRedirect(string oldFilePath);
}

/// <summary>
/// Called when a file is about to be redirected.
/// </summary>
/// <param name="oldPath">The path that was originally going to be opened.</param>
/// <param name="newPath">The new path of the file.</param>
[Obsolete("This API was removed in Virtual FileSystem Rewrite. If you need this, please let me know.")]
public delegate void Redirecting(string oldPath, string newPath);

/// <summary>
/// Called when a file with a specific file is going to be loaded.
/// Note: This is before redirection takes place, see <see cref="Redirecting"/> if you want to know redirected paths.
/// </summary>
/// <param name="path">The path to be loaded.</param>
[Obsolete("This API was removed in Virtual FileSystem Rewrite. If you need this, please let me know.")]
public delegate void Loading(string path);