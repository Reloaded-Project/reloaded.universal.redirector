using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Reloaded.Hooks;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;

/// <summary>
/// Contains the common code for setting up hook tests.
/// </summary>
public partial class BaseHookTest : IDisposable
{
    protected readonly RedirectorApi Api;
    protected readonly TemporaryClonedFolder _clonedFolder;
    
    public BaseHookTest()
    {
        _clonedFolder = new TemporaryClonedFolder(Paths.AssetsFolder);
        Api = new RedirectorApi(new Lib.Redirector(_clonedFolder.FolderPath));
        FileAccessServer.Initialize(ReloadedHooks.Instance, Api, AppContext.BaseDirectory);
    }

    public void Dispose() => _clonedFolder.Dispose();

    protected string GetBasePath() => Paths.GetBase(_clonedFolder.FolderPath);
    protected string GetOverlay1Path() => Paths.GetOverlay1(_clonedFolder.FolderPath);
    protected string GetOverlay2Path() => Paths.GetOverlay2(_clonedFolder.FolderPath);
    protected string GetOverride1Path() => Paths.GetOverride1(_clonedFolder.FolderPath);
    protected string GetOverride2Path() => Paths.GetOverride2(_clonedFolder.FolderPath);

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