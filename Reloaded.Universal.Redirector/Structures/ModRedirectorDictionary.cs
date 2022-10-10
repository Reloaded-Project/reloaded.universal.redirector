using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Utility;

namespace Reloaded.Universal.Redirector.Structures;

public class ModRedirectorDictionary : IDisposable
{
    public Dictionary<string, string> FileRedirects { get; set; } = new Dictionary<String,String>(StringComparer.OrdinalIgnoreCase);

    private static string _programFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;

    /// <summary>
    /// Target folder the redirection is pointing towards.
    /// </summary>
    public string RedirectFolder { get; private set; } = string.Empty;

    /// <summary>
    /// Path of the source folder to redirect from inside the application directory.
    /// </summary>
    public string SourceFolder { get; private set; } = string.Empty;

    private FileSystemWatcher _watcher = null!;

    /* Creation/Destruction */
    public ModRedirectorDictionary() { }

    /// <summary>
    /// Creates a mapping from a given folder's files to files in the target application directory.
    /// </summary>
    /// <param name="redirectFolder">Full path of the folder to redirect to.</param>
    /// <param name="sourceFolder">Path of the source folder to redirect from inside the application directory.</param>
    public ModRedirectorDictionary(string redirectFolder, string sourceFolder = "")
    {
        RedirectFolder = redirectFolder;
        SourceFolder = sourceFolder;
        SetupFileWatcher();
        SetupFileRedirects();
    }

    ~ModRedirectorDictionary()
    {
        Dispose();
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Attempts to acquire a redirection.
    /// </summary>
    /// <param name="path">The original path of the file.</param>
    /// <param name="newPath">The new path of the file.</param>
    /// <returns>True if it succeeded, else false.</returns>
    public bool GetRedirection(string path, out string newPath)
    {
        var fileRedirects = FileRedirects;
        if (fileRedirects.TryGetValue(path, out newPath!))
            return true;

        newPath = path;
        return false;
    }

    /* Setup the dictionary of file redirections. */
    private void SetupFileRedirects()
    {
        if (!Directory.Exists(RedirectFolder)) 
            return;

        var redirects   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allModFiles = RelativePaths.GetRelativeFilePaths(RedirectFolder);

        foreach (string modFile in allModFiles)
        {
            string applicationFileLocation = _programFolder + SourceFolder + modFile;
            string modFileLocation = RedirectFolder + modFile;
            applicationFileLocation = Path.GetFullPath(applicationFileLocation);
            modFileLocation         = Path.GetFullPath(modFileLocation);

            redirects[applicationFileLocation] = modFileLocation;
        }

        FileRedirects = redirects;
    }

    /* Sets up the FileSystem watcher that will update redirect paths on file add/modify/delete. */
    private void SetupFileWatcher()
    {
        if (!Directory.Exists(RedirectFolder)) 
            return;

        _watcher = new FileSystemWatcher(RedirectFolder);
        _watcher.EnableRaisingEvents   = true;
        _watcher.IncludeSubdirectories = true;
        _watcher.Created += (sender, args) => { SetupFileRedirects(); };
        _watcher.Deleted += (sender, args) => { SetupFileRedirects(); };
        _watcher.Renamed += (sender, args) => { SetupFileRedirects(); };
    }
}