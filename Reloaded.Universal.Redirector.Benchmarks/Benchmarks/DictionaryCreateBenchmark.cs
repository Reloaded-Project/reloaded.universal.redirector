using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Lib.Structures;
#pragma warning disable CS8618

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DictionaryCreateBenchmark : IBenchmark
{
    private const int ItemCount = 10000;
    
    public Dictionary<string, int> NetDict = null!;
    public SpanOfCharDict<int> SpanDict;
    
    public Dictionary<string, int> ExistingNetDict = null!;
    public SpanOfCharDict<int> ExistingSpanDict;
    
    public string[] Strings = null!;

    [GlobalSetup]
    public void Setup()
    {
        Strings = new string[ItemCount];

        for (int x = 0; x < ItemCount; x++)
            Strings[x] = Path.GetRandomFileName();
        
        // The update part.
        ExistingSpanDict = new SpanOfCharDict<int>(ItemCount);
        ExistingNetDict = new Dictionary<string, int>(ItemCount);
        for (int x = 0; x < Strings.Length; x++)
        {
            ExistingNetDict[Strings[x]] = x;
            ExistingSpanDict.AddOrReplace(Strings[x], x);
        }
    }

    [Benchmark]
    public void Add_WithSpanDict()
    {
        SpanDict = new SpanOfCharDict<int>(ItemCount);
        for (int x = 0; x < Strings.Length; x++)
            SpanDict.AddOrReplace(Strings[x], x);
    }
    
    [Benchmark]
    public void Update_WithSpanDict()
    {
        for (int x = 0; x < Strings.Length; x++)
            ExistingSpanDict.AddOrReplace(Strings[x], x);
    }
    
    [Benchmark(Baseline = true)]
    public void Add_WithDictionary()
    {
        NetDict = new Dictionary<string, int>(ItemCount);
        for (int x = 0; x < Strings.Length; x++)
            NetDict[Strings[x]] = x;
    }
    
    [Benchmark]
    public void Update_WithDictionary()
    {
        for (int x = 0; x < Strings.Length; x++)
            ExistingNetDict[Strings[x]] = x;
    }
}