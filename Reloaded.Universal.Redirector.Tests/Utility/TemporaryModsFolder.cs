namespace Reloaded.Universal.Redirector.Tests.Utility;

public class TemporaryModsFolder : TemporaryFolderAllocation
{
    public TemporaryModsFolder()
    {
        var baseFolder = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.Base)!);
        var baseWithSubfolders = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.BaseWithSubfolders)!);
        var overlay1 = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.Overlay1)!);
        var overlay2 = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.Overlay2)!);
        var override1 = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.Override1)!);
        var override2 = Path.Combine(FolderPath, Path.GetDirectoryName(Paths.Override2)!);
        IO.CopyFilesRecursively(Paths.Base, baseFolder);
        IO.CopyFilesRecursively(Paths.BaseWithSubfolders, baseWithSubfolders);
        IO.CopyFilesRecursively(Paths.Overlay1, overlay1);
        IO.CopyFilesRecursively(Paths.Overlay2, overlay2);
        IO.CopyFilesRecursively(Paths.Override1, override1);
        IO.CopyFilesRecursively(Paths.Override2, override2);
    }
}