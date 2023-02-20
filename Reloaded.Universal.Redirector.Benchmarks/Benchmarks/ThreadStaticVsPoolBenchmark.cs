using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

public class ThreadStaticVsPoolBenchmark : IBenchmark
{
    [ThreadStatic]
    // ReSharper disable once ThreadStaticFieldHasInitializer
    private static byte[] _fieldStatic = new byte[65536];

    [Benchmark]
    public byte[] RentReturn()
    {
        var buf = ArrayPool<byte>.Shared.Rent(65536);
        ArrayPool<byte>.Shared.Return(buf);
        return buf;
    }

    [Benchmark]
    public byte[] GetStatic()
    {
        return _fieldStatic;
    }
}