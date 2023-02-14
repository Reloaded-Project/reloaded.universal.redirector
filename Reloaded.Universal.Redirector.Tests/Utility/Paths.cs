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
    public static readonly string Base = Path.Combine(AssetsFolder, "Base");
    
    /// <summary>
    /// Base folder (wish subfolders) for testing redirections.
    /// </summary>
    public static readonly string BaseWithSubfolders = Path.Combine(AssetsFolder, "Base with Subfolder");
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static readonly string Overlay1 = Path.Combine(AssetsFolder, "Overlay1");
    
    /// <summary>
    /// An overlay folder that combines with the base folder.
    /// </summary>
    public static readonly string Overlay2 = Path.Combine(AssetsFolder, "Overlay2");
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static readonly string Override1 = Path.Combine(AssetsFolder, "Override1");
    
    /// <summary>
    /// An overlay folder that overrides a file stored in the base folder.
    /// </summary>
    public static readonly string Override2 = Path.Combine(AssetsFolder, "Override2");
}