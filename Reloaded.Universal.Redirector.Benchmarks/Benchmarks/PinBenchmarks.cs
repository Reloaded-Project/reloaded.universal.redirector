using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Lib.Extensions;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark to investigate overhead of pinning an object.
/// Used to influence dictionary design.
/// </summary>
public unsafe class PinBenchmarks : IBenchmark
{
    // Note: I expect difference to be basically 100% negligible, I am just curious.
    
    [Params(8, 64)] // 8 is expected value, 64 is our of curiosity.
    public int ArrayLength; // We will store current index elsewhere, so here to simulate that.
    public int[] Numbers = new int[64];
    
    [GlobalSetup]
    public void Setup()
    {
        for (int x = 0; x < Numbers.Length; x++) 
            Numbers[x] = x;
    }

    [Benchmark]
    public int WithPin()
    {
        var result = 0;
        fixed (int* ptr = &Numbers.DangerousGetReferenceAt(0))
        {
            for (int x = 0; x < ArrayLength; x++)
                result += ptr[x];
        }

        return result;
    }
    
    [Benchmark]
    public int WithPin_Safe()
    {
        var result = 0;
        fixed (int* ptr = &Numbers[0])
        {
            for (int x = 0; x < ArrayLength; x++)
                result += ptr[x];
        }

        return result;
    }

    [Benchmark]
    public int WithoutPin_NoBounds()
    {
        var result = 0;
        
        for (int x = 0; x < ArrayLength; x++)
            result += Numbers.DangerousGetReferenceAt(0);

        return result;
    }
    
    [Benchmark]
    public int Baseline_NoPin_Bounds()
    {
        var result = 0;
        
        // Codegen might clone this loop, resulting in no difference.
        for (int x = 0; x < ArrayLength; x++)
            result += Numbers[x];

        return result;
    }
}