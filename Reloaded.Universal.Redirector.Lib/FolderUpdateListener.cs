namespace Reloaded.Universal.Redirector.Lib;

/// <summary>
/// Class that listens for updates of all folder redirections.
/// </summary>
public class FolderUpdateListener : IDisposable
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
    
    /// <summary/>
    /// <param name="baseFolder">The base folder whose events we are listening to.</param>
    public FolderUpdateListener(string baseFolder)
    {
        BaseFolder = baseFolder;
        
        // Note: Technically FileSystemWatcher is not foolproof; but 
        //       with large buffer is reliable enough for our needs.
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

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }
}