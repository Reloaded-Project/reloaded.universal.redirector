namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Temporarily creates a folder with X dummy files for testing purposes; then disposes it.
/// </summary>
public class TemporaryJunkFolder : TemporaryFolderAllocation
{
    /// <summary>
    /// List of file names stored in this folder.
    /// </summary>
    public string[] FileNames { get; set; }

    /// <summary/>
    public TemporaryJunkFolder(int numFiles, Func<string>? createFileName = null)
    {
        FileNames = new string[numFiles];
        createFileName ??= () => Path.GetRandomFileName() + Path.GetRandomFileName() + Path.GetRandomFileName();
        
        for (int x = 0; x < numFiles; x++)
        {
            string tempPath;
            do
            {
                tempPath = Path.Combine(FolderPath, createFileName());
            }
            while (File.Exists(tempPath));

            FileNames[x] = Path.GetFileName(tempPath);
            using var file = File.Create(tempPath, 4096);
            for (int y = 0; y < x; y++)
                file.WriteByte((byte)y);
        }
        
        Array.Sort(FileNames);
    }
}