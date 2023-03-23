using BenchmarkDotNet.Attributes;
using FileEmulationFramework.Lib.IO;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using WindowsDirectorySearcher = Reloaded.Universal.Redirector.Lib.Utility.WindowsDirectorySearcher;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, DisassemblyDiagnoser(int.MaxValue, printSource: true, printInstructionAddresses: true, exportCombinedDisassemblyReport: true)]
public class LookupCreateBenchmark : IBenchmark
{
    public RedirectionTree<RedirectionTreeTarget> Tree { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var searchPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName;
        
        // Note: Do not multithread, we need reproducible order between runs. 
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(searchPath, out var groupsLst);
        var groups = groupsLst.ToArray();
        
        Tree = RedirectionTree<RedirectionTreeTarget>.Create();
        foreach (var group in groups)
            Tree.AddFolderPaths(group.Directory.FullPath, group.Items, group.Directory.FullPath);
    }

    [Benchmark]
    public LookupTree<RedirectionTreeTarget> Build()
    {
        return new LookupTree<RedirectionTreeTarget>(Tree);
    }
}