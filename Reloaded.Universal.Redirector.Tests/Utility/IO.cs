namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Various methods related to IO workloads.
/// </summary>
public class IO
{
    /// <summary>
    /// Recursively copies files from one directory to another.
    /// </summary>
    public static void CopyFilesRecursively(string source, string target)
    {
        CopyFilesRecursively(new DirectoryInfo(source), new DirectoryInfo(target));
    }
    
    /// <summary>
    /// Recursively copies files from one directory to another.
    /// </summary>
    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) 
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }
}