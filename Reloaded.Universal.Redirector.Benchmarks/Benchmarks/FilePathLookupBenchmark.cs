using BenchmarkDotNet.Attributes;
using FileEmulationFramework.Lib.IO;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, DisassemblyDiagnoser(int.MaxValue, printInstructionAddresses: true, printSource: true)]
public class FilePathLookupBenchmark : IBenchmark
{
    public string LookupDirectory { get; set; }
    public string LookupFile { get; set; }
    public string LookupDirectoryLongest { get; set; }
    public string LookupFileLongest { get; set; }
    public string LookupDirectoryLongestLower { get; set; }
    public string LookupFileLongestLower { get; set; }
    public string LookupDirectoryLower { get; set; }
    public string LookupFileLower { get; set; }
    public LookupTree Tree { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        var searchPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName;
        
        // Note: Do not multithread, we need reproducible order between runs. 
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(searchPath, out var groupsLst, false);
        var groups = groupsLst.ToArray();
        
        var tree = RedirectionTree.Create();
        foreach (var group in groups)
        {
            var directoryLower = group.Directory.FullPath.ToUpperInvariant();
            tree.AddFolderPaths(directoryLower, group.Files.Select(x => x.ToUpperInvariant()).ToArray(), directoryLower);
        }

        Tree = new LookupTree(tree);
        var lookupDirShortest = groupsLst.Where(x => x.Files.Length > 0).MinBy(x => x.Directory.FullPath.Length);
        LookupDirectory = lookupDirShortest.Directory.FullPath;
        LookupFile = Path.Combine(LookupDirectory, lookupDirShortest.Files[0]);
        LookupDirectoryLower = LookupDirectory.ToUpperInvariant();
        LookupFileLower = LookupFile.ToUpperInvariant();

        var lookupDirLongest = groupsLst.Where(x => x.Files.Length > 0).MaxBy(x => x.Directory.FullPath.Length);
        LookupDirectoryLongest = lookupDirLongest!.Directory.FullPath;
        LookupFileLongest = Path.Combine(LookupDirectoryLongest, lookupDirLongest.Files.MaxBy(x => x.Length)!);
        LookupDirectoryLongestLower = LookupDirectoryLongest.ToUpperInvariant();
        LookupFileLongestLower = LookupFileLongest.ToUpperInvariant();
        Console.WriteLine($"Lookup Dir: {LookupDirectory}, Lookup File: {Path.GetFileName(LookupFile)}");
        Console.WriteLine($"Longest Lookup Dir: {LookupDirectoryLongest}, Lookup File: {Path.GetFileName(LookupFileLongest)}");
    }

    [Benchmark]
    public bool LookupFilePath() => Tree.TryGetFile(LookupFile, out _);

    [Benchmark]
    public bool LookupFolderPath() => Tree.TryGetFolder(LookupDirectory, out _);
    
    [Benchmark]
    public bool LookupFilePathLower() => Tree.TryGetFileUpper(LookupFileLower, out _);

    [Benchmark]
    public bool LookupFolderPathLower() => Tree.TryGetFolderUpper(LookupDirectoryLower, out _);
    
    [Benchmark]
    public bool LookupFilePathLongest() => Tree.TryGetFile(LookupFileLongest, out _);

    [Benchmark]
    public bool LookupFolderPathLongest() => Tree.TryGetFolder(LookupDirectoryLongest, out _);
    
    [Benchmark]
    public bool LookupFilePathLongestLower() => Tree.TryGetFileUpper(LookupFileLongestLower, out _);

    [Benchmark]
    public bool LookupFolderPathLongestLower() => Tree.TryGetFolderUpper(LookupDirectoryLongestLower, out _);
}