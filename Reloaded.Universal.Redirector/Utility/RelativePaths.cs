using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reloaded.Universal.Redirector.Utility;

public static class RelativePaths
{
    /// <summary>
    /// Retrieves all relative file paths to a directory.
    /// </summary>
    /// <param name="directory">Absolute path to directory to get file paths from. </param>
    public static List<string> GetRelativeFilePaths(string directory)
    {
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Select(x => x.TrimStart(directory)).ToList();
    }
}