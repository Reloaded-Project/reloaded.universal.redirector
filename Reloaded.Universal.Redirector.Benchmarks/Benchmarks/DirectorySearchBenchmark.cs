using BenchmarkDotNet.Attributes;
using FileEmulationFramework.Lib.IO;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

/// <summary>
/// Lets us know how long it takes to search a directory.
/// </summary>
[MemoryDiagnoser]
public class DirectorySearchBenchmark : IBenchmark
{
    /// <summary>
    /// Path to the search folder.
    /// </summary>
    public static string SearchPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName;

    /// <summary>
    /// Legacy implementation from older versions of Redirector.
    /// </summary>
    [Benchmark]
    public List<DirectoryFilesGroup> System_X86()
    {
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(SearchPath, out var result);
        return result;
    }
}