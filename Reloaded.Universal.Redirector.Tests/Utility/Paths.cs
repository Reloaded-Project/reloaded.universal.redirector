namespace Reloaded.Universal.Redirector.Tests.Utility;

public static class Paths
{
    /// <summary>
    /// The location where the current program is located.
    /// </summary>
    public static readonly string ProgramFolder = Path.GetDirectoryName(new Uri(AppContext.BaseDirectory).LocalPath)!;
    
    /// <summary>
    /// Folder containing the assets.
    /// </summary>
    public static readonly string AssetsFolder = Path.Combine(ProgramFolder, "Assets");

    /// <summary>
    /// Base folder for testing redirections.
    /// </summary>
    public static readonly string Base = GetBase(AssetsFolder);
    
    /// <summary>
    /// Base folder for testing redirections.
    /// </summary>
    public static string GetBase(string baseFolder) => Path.Combine(baseFolder, "Base");
    
    /// <summary>
    /// Base folder (wish subfolders) for testing redirections.
    /// </summary>
    public static string BaseWithSubfolders => GetBaseWithSubfolders(AssetsFolder);
    
    /// <summary>
    /// Base folder (wish subfolders) for testing redirections.
    /// </summary>
    public static string GetBaseWithSubfolders(string baseFolder) => Path.Combine(baseFolder, "Base with Subfolder");
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static readonly string Overlay1 = GetOverlay1(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static string GetOverlay1(string baseFolder) => Path.Combine(baseFolder, "Overlay1");
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static readonly string Overlay2 = GetOverlay2(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static string GetOverlay2(string baseFolder) => Path.Combine(baseFolder, "Overlay2");
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static readonly string Override1 = GetOverride1(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static string GetOverride1(string baseFolder) => Path.Combine(baseFolder, "Override1");
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static readonly string Override2 = GetOverride2(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static string GetOverride2(string baseFolder) => Path.Combine(baseFolder, "Override2");
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder with subfolders.
    /// </summary>
    public static string OverrideWithSubfolders => GetBaseWithSubfolders(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder with subfolders.
    /// </summary>
    public static string GetOverrideWithSubfolders(string baseFolder) => Path.Combine(baseFolder, "Override with Subfolder");
    
    /// <summary>
    /// An overlay folder that contains a native DLL which can be loaded.
    /// </summary>
    public static readonly string NativeDll = GetOverride1(AssetsFolder);
    
    /// <summary>
    /// An overlay folder that contains a native DLL which can be loaded.
    /// </summary>
    public static string GetNativeDll(string baseFolder) => Path.Combine(baseFolder, nint.Size == 4 ? "Dlls/x86" : "Dlls/x64");
}