using System.Diagnostics.CodeAnalysis;
using Reloaded.Universal.Redirector.Lib.Backports.System.Globalization;
using Reloaded.Universal.Redirector.Lib.Interfaces;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Lib;

/// <summary>
/// Class that listens for updates of all folder redirections.
/// </summary>
public class FolderUpdateListener<TReceiver> : IDisposable where TReceiver : IFolderRedirectionUpdateReceiver
{
    // TODO: Folder updates for items outside of common 'Mods' folder.
    
    /// <summary>
    /// The base folder which should include all mods inside.
    /// </summary>
    public string BaseFolder { get; private set; }
    
    /// <summary>
    /// The file system watcher listening for events under the base folder.
    /// </summary>
    public FileSystemWatcher Watcher { get; private set; }

    /// <summary>
    /// Maps source from which files are to be redirected to a folder which handles the redirection.
    /// </summary>
    public RedirectionTree<FolderRedirection> SourceToFolder = RedirectionTree<FolderRedirection>.Create();
    
    /// <summary>
    /// The item that receives file add/remove notifications.
    /// </summary>
    public TReceiver Receiver { get; private set; }
    
    /// <summary/>
    /// <param name="baseFolder">The base folder whose events we are listening to.</param>
    /// <param name="receiver">The notification receiver.</param>
    public FolderUpdateListener(string baseFolder, TReceiver receiver)
    {
        Receiver = receiver;
        BaseFolder = baseFolder.NormalizePath();
        
        /*
            Technically FileSystemWatcher is not foolproof; but  
            with large buffer is reliable enough for our needs.  
        */ 
        Watcher = new FileSystemWatcher(BaseFolder);
        Watcher.IncludeSubdirectories = true;
        Watcher.InternalBufferSize = ushort.MaxValue;
        Watcher.Created += WatcherOnCreated;
        Watcher.Deleted += WatcherOnDeleted;
        Watcher.Renamed += WatcherOnRenamed;
        Watcher.EnableRaisingEvents = true;
    }
    
    /// <inheritdoc />
    ~FolderUpdateListener()
    {
        Dispose();        
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Watcher.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Registers the given redirection to receive update vents.
    /// </summary>
    /// <param name="redirection">The redirection to register.</param>
    public void Register(FolderRedirection redirection)
    {
        SourceToFolder.AddPath(redirection.TargetFolder, redirection);
    }

    /// <summary>
    /// Unregisters the given redirection from update events.
    /// </summary>
    /// <param name="redirection">The redirection to unregister.</param>
    public void Unregister(FolderRedirection redirection)
    {
        if (SourceToFolder.TryGetFolder(redirection.TargetFolder, out var result))
            result.Reset(); ;
    }

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        if (TryResolveFolderRedirection(e.FullPath, out var result))
            Receiver.OnOtherUpdate(result);
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        if (TryResolveFolderRedirection(e.FullPath, out var result))
            Receiver.OnOtherUpdate(result);
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        var filePath = e.FullPath;
        var filePathUpper = filePath.Length <= 512
            ? stackalloc char[filePath.Length]
            : GC.AllocateUninitializedArray<char>(filePath.Length); // super cold path, basically never hit
        
        TextInfo.ChangeCase<TextInfo.ToUpperConversion>(filePath, filePathUpper);
        if (TryResolveFolderRedirectionUpper(filePathUpper, out var result))
            Receiver.OnFileAddition(result, filePathUpper[result.TargetFolder.Length..]);
    }

    /// <summary>
    /// Tries to resolve a folder redirection based on a path.
    /// </summary>
    /// <param name="filePath">Path of the file modified/affected/changed.</param>
    /// <param name="result">The found redirection.</param>
    /// <returns>True if found; else false.</returns>
    private bool TryResolveFolderRedirection(ReadOnlySpan<char> filePath, [MaybeNullWhen(false)] out FolderRedirection result)
    {
        var filePathUpper = filePath.Length <= 512
            ? stackalloc char[filePath.Length]
            : GC.AllocateUninitializedArray<char>(filePath.Length); // super cold path, basically never hit
        
        TextInfo.ChangeCase<TextInfo.ToUpperConversion>(filePath, filePathUpper);
        return TryResolveFolderRedirectionUpper(filePathUpper, out result);
    }
    
    /// <summary>
    /// Tries to resolve a folder redirection based on a path.
    /// </summary>
    /// <param name="filePathUpper">Path of the file modified/affected/changed. Upper case.</param>
    /// <param name="result">The found redirection.</param>
    /// <returns>True if found; else false.</returns>
    private bool TryResolveFolderRedirectionUpper(ReadOnlySpan<char> filePathUpper, [MaybeNullWhen(false)] out FolderRedirection result)
    {
        var resolved = SourceToFolder.ResolvePartialPath(filePathUpper);
        if (resolved.HasValue)
        {
            var items = resolved.GetValueOrDefault().Items;
            if (items.Count >= 1)
            {
                result = items.GetFirstItem(out _);
                return true;
            }
        }

        result = default;
        return false;
    }
}