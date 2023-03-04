using Reloaded.Hooks;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;

/// <summary>
/// Contains the common code for setting up hook tests.
/// </summary>
public class BaseHookTest : IDisposable
{
    protected readonly RedirectorApi Api;
    protected readonly TemporaryClonedFolder ClonedFolder;
    
    public BaseHookTest()
    {
        ClonedFolder = new TemporaryClonedFolder(Paths.AssetsFolder);
        Api = new RedirectorApi(new Lib.Redirector(ClonedFolder.FolderPath));
        FileAccessServer.Initialize(ReloadedHooks.Instance, Api, AppContext.BaseDirectory);
    }

    public void Dispose() => ClonedFolder.Dispose();

    protected string GetBasePath() => Paths.GetBase(ClonedFolder.FolderPath);
    protected string GetOverlay1Path() => Paths.GetOverlay1(ClonedFolder.FolderPath);
    protected string GetOverlay2Path() => Paths.GetOverlay2(ClonedFolder.FolderPath);
    protected string GetOverride1Path() => Paths.GetOverride1(ClonedFolder.FolderPath);
    protected string GetOverride2Path() => Paths.GetOverride2(ClonedFolder.FolderPath);

    protected string GetBaseFile(string fileName) => Path.Combine(GetBasePath(), fileName);
    protected string GetOverlay1File(string fileName) => Path.Combine(GetOverlay1Path(), fileName);
    protected string GetOverlay2File(string fileName) => Path.Combine(GetOverlay2Path(), fileName);
    protected string GetOverride1File(string fileName) => Path.Combine(GetOverride1Path(), fileName);
    protected string GetOverride2File(string fileName) => Path.Combine(GetOverride2Path(), fileName);

    protected string GetBaseFilePrefixed(string fileName) => Strings.PrefixLocalDeviceStr + GetBaseFile(fileName);
    protected string GetOverlay1FilePrefixed(string fileName) => Strings.PrefixLocalDeviceStr + GetOverlay1File(fileName);
    protected string GetOverlay2FilePrefixed(string fileName) => Strings.PrefixLocalDeviceStr + GetOverlay2File(fileName);
    protected string GetOverride1FilePrefixed(string fileName) => Strings.PrefixLocalDeviceStr + GetOverride1File(fileName);
    protected string GetOverride2FilePrefixed(string fileName) => Strings.PrefixLocalDeviceStr + GetOverride2File(fileName);
}