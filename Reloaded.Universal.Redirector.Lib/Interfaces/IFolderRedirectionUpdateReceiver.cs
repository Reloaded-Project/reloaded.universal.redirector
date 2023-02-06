using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;

namespace Reloaded.Universal.Redirector.Lib.Interfaces;

/// <summary>
/// For interfaces that receive notification on updating folder updates from <see cref="FolderRedirection"/>.
/// </summary>
public interface IFolderRedirectionUpdateReceiver
{
    /// <summary>
    /// Called for any other update not covered by other events.
    /// If this is raised, full rebuild is needed.
    /// </summary>
    public void OnOtherUpdate(FolderRedirection sender);

    /// <summary>
    /// Called whenever a file is added to the folder redirection.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="filePath">The path of the file.</param>
    public void OnFileAddition(FolderRedirection sender, string filePath);
}