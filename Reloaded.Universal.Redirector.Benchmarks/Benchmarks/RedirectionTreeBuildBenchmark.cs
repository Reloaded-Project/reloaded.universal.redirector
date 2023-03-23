using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class RedirectionTreeBuildBenchmark : IBenchmark
{
    public string[] Files = null!;
    public DirectoryFilesGroup[] Groups = null!;

    [GlobalSetup]
    public void Setup()
    {
        var searchPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName;
        
        // Note: Do not multithread, we need reproducible order between runs. 
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(searchPath, out var groups);

        var allFiles = new List<string>();
        foreach (var group in groups)
        foreach (var file in group.Items)
            allFiles.Add(Path.Combine(group.Directory.FullPath, file.FileName));

        Groups = groups.ToArray();
        Files = allFiles.ToArray();
        Console.WriteLine($"[{nameof(RedirectionTreeBuildBenchmark)}] File Count: {Files.Length}, Group Count: {groups.Count}");
    }

    // Note: For benchmarks we just map directories to self. This does not affect our runtime.
    
    [Benchmark(Baseline = true)]
    public RedirectionTree<RedirectionTreeTarget> BuildTree_WithAddFile()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        foreach (var file in Files)
            tree.AddPath(file, file);

        return tree;
    }
    
    [Benchmark]
    public RedirectionTree<RedirectionTreeTarget> BuildTree_WithAddFiles()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        foreach (var group in Groups)
            tree.AddFolderPaths(group.Directory.FullPath, group.Items, group.Directory.FullPath);

        return tree;
    }
    
    [Benchmark]
    public RedirectionTree<RedirectionTreeTarget> BuildTree_WithAddFiles_WithSorting()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        Array.Sort(Groups, (a, b) => b.Directory.FullPath.Length.CompareTo(a.Directory.FullPath.Length));
        foreach (var group in Groups)
            tree.AddFolderPaths(group.Directory.FullPath, group.Items, group.Directory.FullPath);

        return tree;
    }
}