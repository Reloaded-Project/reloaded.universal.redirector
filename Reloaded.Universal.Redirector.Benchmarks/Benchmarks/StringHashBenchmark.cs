using System.Numerics;
using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Benchmarks.Utilities;
using Reloaded.Universal.Redirector.Lib.Extensions;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MinColumn, MaxColumn, MedianColumn, DisassemblyDiagnoser(printInstructionAddresses: true)]
public class StringHashBenchmark : IBenchmark
{
    private const int ItemCount = 10000;
    
    [Params(12, 64, 96, 128, 256, 1024)]
    public int CharacterCount { get; set; }
    
    public string[] Input { get; set; } = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        Input = new string[ItemCount];
        
        for (int x = 0; x < ItemCount; x++)
            Input[x] = Helpers.RandomString(CharacterCount);
    }

    [Benchmark]
    public nuint Custom()
    {
        nuint result = 0;
        var maxLen = Input.Length / 4;
        // unroll
        for (int x = 0; x < maxLen; x += 4)
        {
            result = Strings.GetNonRandomizedHashCode(Input.DangerousGetReferenceAt(x));
            result = Strings.GetNonRandomizedHashCode(Input.DangerousGetReferenceAt(x + 1));
            result = Strings.GetNonRandomizedHashCode(Input.DangerousGetReferenceAt(x + 2));
            result = Strings.GetNonRandomizedHashCode(Input.DangerousGetReferenceAt(x + 3));
        }
        
        return result;
    }
    
    [Benchmark]
    public int Runtime_NonRandom_702()
    {
        var result = 0;
        var maxLen = Input.Length / 4;
        // unroll
        for (int x = 0; x < maxLen; x += 4)
        {
            result = Runtime_70_Impl(Input.DangerousGetReferenceAt(x));
            result = Runtime_70_Impl(Input.DangerousGetReferenceAt(x + 1));
            result = Runtime_70_Impl(Input.DangerousGetReferenceAt(x + 2));
            result = Runtime_70_Impl(Input.DangerousGetReferenceAt(x + 3));
        }
        
        return result;
    }
    
    [Benchmark]
    public int Runtime_Current()
    {
        var result = 0;
        var maxLen = Input.Length / 4;
        // unroll
        for (int x = 0; x < maxLen; x += 4)
        {
            result = Input.DangerousGetReferenceAt(x).GetHashCode();
            result = Input.DangerousGetReferenceAt(x + 1).GetHashCode();
            result = Input.DangerousGetReferenceAt(x + 2).GetHashCode();
            result = Input.DangerousGetReferenceAt(x + 3).GetHashCode();
        }
        
        return result;
    }
    
    public unsafe int Runtime_70_Impl(ReadOnlySpan<char> text)
    {
        fixed (char* src = &text.GetPinnableReference())
        {
            // Asserts here for alignment etc. are no longer valid as we are operating on a slice, so memory alignment is not guaranteed.
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            uint* ptr = (uint*)src;
            int length = text.Length;

            while (length > 2)
            {
                length -= 4;
                // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                ptr += 2;
            }

            if (length > 0)
            {
                // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
            }

            return (int)(hash1 + (hash2 * 1566083941));
        }
    }
}