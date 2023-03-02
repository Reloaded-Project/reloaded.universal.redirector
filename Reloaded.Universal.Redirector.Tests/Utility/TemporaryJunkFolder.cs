namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Temporarily creates a folder with X dummy files for testing purposes; then disposes it.
/// </summary>
public class TemporaryJunkFolder : TemporaryFolderAllocation
{
    /// <summary/>
    public TemporaryJunkFolder(int numFiles)
    {
        for (int x = 0; x < numFiles; x++)
        {
            string tempPath;
            do
            {
                tempPath = Path.Combine(FolderPath, Path.GetRandomFileName() + Path.GetRandomFileName() + Path.GetRandomFileName());
            } 
            while (File.Exists(tempPath));
            
            using var file = File.Create(tempPath, 4096);
            for (int y = 0; y < x; y++)
                file.WriteByte((byte)y);
        }
    }
}