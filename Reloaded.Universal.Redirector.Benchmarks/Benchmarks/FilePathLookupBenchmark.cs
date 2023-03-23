using BenchmarkDotNet.Attributes;
using FileEmulationFramework.Lib.IO;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Utility;
using FileInformation = Reloaded.Universal.Redirector.Lib.Utility.FileInformation;
using WindowsDirectorySearcher = Reloaded.Universal.Redirector.Lib.Utility.WindowsDirectorySearcher;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, DisassemblyDiagnoser(int.MaxValue, printInstructionAddresses: true, printSource: true)]
public class FilePathLookupBenchmark : IBenchmark
{
    public string LookupDirectory { get; set; } = null!;
    public string LookupFile { get; set; } = null!;
    public string LookupDirectoryLongest { get; set; } = null!;
    public string LookupFileLongest { get; set; } = null!;
    public string LookupDirectoryLongestUpper { get; set; } = null!;
    public string LookupFileLongestUpper { get; set; } = null!;
    public string LookupDirectoryUpper { get; set; } = null!;
    public string LookupFileUpper { get; set; } = null!;
    public LookupTree<RedirectionTreeTarget> Tree { get; set; }
    public RedirectionTree<RedirectionTreeTarget> RedirectionTree { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        var searchPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName;
        
        // Note: Do not multithread, we need reproducible order between runs. 
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(searchPath, out var groupsLst);
        var groups = groupsLst.ToArray();
        
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        foreach (var group in groups)
        {
            var directoryUpper = group.Directory.FullPath.ToUpperInvariant();
            var items = group.Items.Select(x => new FileInformation(x.FileName.ToUpperInvariant(), x.IsDirectory));
            tree.AddFolderPaths(directoryUpper, items.ToArray(), directoryUpper);
        }

        RedirectionTree = tree;
        Tree = new LookupTree<RedirectionTreeTarget>(tree);
        var lookupDirShortest = groupsLst.Where(x => x.Items.Length > 0).MinBy(x => x.Directory.FullPath.Length);
        LookupDirectory = lookupDirShortest!.Directory.FullPath;
        LookupFile = Path.Combine(LookupDirectory, lookupDirShortest.Items[0].FileName);
        LookupDirectoryUpper = LookupDirectory.ToUpperInvariant();
        LookupFileUpper = LookupFile.ToUpperInvariant();

        var lookupDirLongest = groupsLst.Where(x => x.Items.Length > 0).MaxBy(x => x.Directory.FullPath.Length);
        LookupDirectoryLongest = lookupDirLongest!.Directory.FullPath;
        LookupFileLongest = Path.Combine(LookupDirectoryLongest, lookupDirLongest.Items.MaxBy(x => x.FileName.Length).FileName!);
        LookupDirectoryLongestUpper = LookupDirectoryLongest.ToUpperInvariant();
        LookupFileLongestUpper = LookupFileLongest.ToUpperInvariant();
        Console.WriteLine($"Lookup Dir: {LookupDirectory}, Lookup File: {Path.GetFileName(LookupFile)}");
        Console.WriteLine($"Longest Lookup Dir: {LookupDirectoryLongest}, Lookup File: {Path.GetFileName(LookupFileLongest)}");
    }

    [Benchmark]
    public bool LookupFilePath() => Tree.TryGetFile(LookupFile, out _, out _);

    [Benchmark]
    public bool LookupFolderPath() => Tree.TryGetFolder(LookupDirectory, out _);
    
    [Benchmark]
    public bool LookupFilePathUpper() => Tree.TryGetFileUpper(LookupFileUpper, out _, out _);

    [Benchmark]
    public bool LookupFolderPathUpper() => Tree.TryGetFolderUpper(LookupDirectoryUpper, out _);
    
    [Benchmark]
    public RedirectionTreeNode<RedirectionTreeTarget>? RedirLookupFilePathUpper() => RedirectionTree.ResolvePartialPath(LookupFileUpper);

    [Benchmark]
    public RedirectionTreeNode<RedirectionTreeTarget>? RedirLookupFolderPathUpper() => RedirectionTree.ResolvePartialPath(LookupDirectoryUpper);
    
    [Benchmark]
    public bool LookupFilePathLongest() => Tree.TryGetFile(LookupFileLongest, out _, out _);

    [Benchmark]
    public bool LookupFolderPathLongest() => Tree.TryGetFolder(LookupDirectoryLongest, out _);
    
    [Benchmark]
    public bool LookupFilePathLongestUpper() => Tree.TryGetFileUpper(LookupFileLongestUpper, out _, out _);

    [Benchmark]
    public bool LookupFolderPathLongestUpper() => Tree.TryGetFolderUpper(LookupDirectoryLongestUpper, out _);
    
    [Benchmark]
    public void RedirLookupFilePathLongestUpper() => RedirectionTree.ResolvePartialPath(LookupFileLongestUpper);

    [Benchmark]
    public void RedirLookupFolderPathLongestUpper() => RedirectionTree.ResolvePartialPath(LookupDirectoryLongestUpper);
}