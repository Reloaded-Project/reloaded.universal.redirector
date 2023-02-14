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
    /// <param name="relativePath">
    ///     The relative path of the file (to source folder).
    ///     This is assumed to be upper case and sanitised; and start with a slash
    ///     such that the string can be concatenated to <see cref="FolderRedirection.SourceFolder"/>.
    /// </param>
    public void OnFileAddition(FolderRedirection sender, ReadOnlySpan<char> relativePath);
}