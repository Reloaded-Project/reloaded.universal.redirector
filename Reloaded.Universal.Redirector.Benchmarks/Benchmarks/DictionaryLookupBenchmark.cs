using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Benchmarks.Utilities;
using Reloaded.Universal.Redirector.Lib.Extensions;
using Reloaded.Universal.Redirector.Lib.Structures;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, MinColumn, MaxColumn, MedianColumn, DisassemblyDiagnoser(printInstructionAddresses: true)]
public class DictionaryLookupBenchmark : IBenchmark
{
    private const int ItemCount = 1000;
    
    [Params(12, 64, 128, 256, 1024)]
    public int CharacterCount { get; set; }
    
    public Dictionary<string, int> NetDictFile = null!;
    public SpanOfCharDict<int> SpanDictFile = null!;
    public string[] FileNames = null!;

    
    [GlobalSetup]
    public void Setup()
    {
        NetDictFile  = new Dictionary<string, int>(ItemCount);
        SpanDictFile = new SpanOfCharDict<int>(ItemCount);
        FileNames = new string[ItemCount];
        
        for (int x = 0; x < ItemCount; x++)
        {
            var str = Helpers.RandomString(CharacterCount);
            
            // Don't allow check by reference; at runtime, we wouldn't have this luxury because we will deal with slices.
            FileNames[x] = str;
            
            NetDictFile[str] = x;
            SpanDictFile.AddOrReplace(str, x);
        }
    }
    
    [Benchmark]
    public int Get_FileName_WithSpanDict()
    {
        var result = 0;
        var maxLen = FileNames.Length / 4;
        // unroll
        for (int x = 0; x < maxLen; x += 4)
        {
            result = SpanDictFile.GetValueRef(FileNames.DangerousGetReferenceAt(x));
            result = SpanDictFile.GetValueRef(FileNames.DangerousGetReferenceAt(x + 1));
            result = SpanDictFile.GetValueRef(FileNames.DangerousGetReferenceAt(x + 2));
            result = SpanDictFile.GetValueRef(FileNames.DangerousGetReferenceAt(x + 3));
        }
        
        return result;
    }
    
    [Benchmark(Baseline = true)]
    public int Get_FileName_WithDictionary()
    {
        var result = 0;
        var maxLen = FileNames.Length / 4;
        // unroll
        for (int x = 0; x < maxLen; x += 4)
        {
            result = NetDictFile[FileNames.DangerousGetReferenceAt(x)];
            result = NetDictFile[FileNames.DangerousGetReferenceAt(x + 1)];
            result = NetDictFile[FileNames.DangerousGetReferenceAt(x + 2)];
            result = NetDictFile[FileNames.DangerousGetReferenceAt(x + 3)];
        }

        return result;
    }
}