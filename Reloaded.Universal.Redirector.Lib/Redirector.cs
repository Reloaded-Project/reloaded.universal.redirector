using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Lib.Utility;

#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib;

/// <summary>
/// The class that ties all functionality together.
/// Contains legacy as well as current API.
/// </summary>
public struct Redirector : IDisposable
{
    /// <summary>
    /// The manager responsible for handling the redirection tree.
    /// </summary>
    public RedirectionTreeManager Manager { get; private set; }
    
    /// <summary>
    /// Class that listens to individual file change events.
    /// </summary>
    public FolderUpdateListener<RedirectionTreeManager> Listener { get; private set; }

    /// <summary>
    /// Creates a new instance of the redirector.
    /// </summary>
    /// <param name="baseFolder">Folder under which all mods are stored under.</param>
    public Redirector(string baseFolder)
    {
        Manager = new RedirectionTreeManager();
        Listener = new FolderUpdateListener<RedirectionTreeManager>(baseFolder, Manager);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Listener.Dispose();
    }
    
    /* Business Logic */

    #region Legacy API
    // Flawed API.
    public void AddCustomRedirect(string oldPath, string newPath)
    {
        Manager.AddFileRedirection(new FileRedirection(oldPath, newPath));
    }

    public void RemoveCustomRedirect(string oldPath)
    {
        oldPath = oldPath.NormalizePath();
        for (var x = Manager.FileRedirections.Count - 1; x >= 0; x--)
        {
            var redir = Manager.FileRedirections[x];
            if (redir.OldPath != oldPath) 
                continue;
            
            Manager.RemoveFileRedirection(redir);
            return;
        }
    }

    public void Add(string targetFolder) => Add(targetFolder, Environment.CurrentDirectory);

    public void Add(string targetFolder, string sourceFolder)
    {
        targetFolder = targetFolder.NormalizePath();
        sourceFolder = sourceFolder.NormalizePath();
        
        var folderRedirect = new FolderRedirection(sourceFolder, targetFolder);
        Manager.AddFolderRedirection(folderRedirect);
        Listener.Register(folderRedirect);
    }
    
    public void Remove(string targetFolder, string sourceFolder)
    {
        targetFolder = targetFolder.NormalizePath();
        sourceFolder = sourceFolder.NormalizePath();
        
        for (var x = Manager.FolderRedirections.Count - 1; x >= 0; x--)
        {
            var folderRedir = Manager.FolderRedirections[x];
            if (!folderRedir.SourceFolder.Equals(sourceFolder, StringComparison.OrdinalIgnoreCase) ||
                !folderRedir.TargetFolder.Equals(targetFolder, StringComparison.OrdinalIgnoreCase)) 
                continue;
            
            Manager.RemoveFolderRedirection(folderRedir);
            Listener.Unregister(folderRedir);
            return;
        }
    }

    public void Remove(string targetFolder)
    {
        targetFolder = targetFolder.NormalizePath();
        
        for (var x = Manager.FolderRedirections.Count - 1; x >= 0; x--)
        {
            var folderRedir = Manager.FolderRedirections[x];
            if (!folderRedir.TargetFolder.Equals(targetFolder, StringComparison.OrdinalIgnoreCase)) 
                continue;
            
            Manager.RemoveFolderRedirection(folderRedir);
            Listener.Unregister(folderRedir);
            return;
        }
    }
    #endregion
}