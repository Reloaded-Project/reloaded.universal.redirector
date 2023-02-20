using BenchmarkDotNet.Attributes;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, DisassemblyDiagnoser(int.MaxValue, printInstructionAddresses: true, printSource: true)]
public class RecursionLockCandidatesBenchmark : IBenchmark
{
    private bool _field;
    
    [ThreadStatic]
    private static bool _fieldStatic;
    private static object _lock = new object();
    private int _fieldInterlocked;

    [Benchmark]
    public bool NoLock()
    {
        if (_field)
            return true;

        _field = true;
        try
        {
            return _field;
        }
        finally
        {
            _field = false;
        }
    }

    [Benchmark]
    public bool ThreadStatic()
    {
        ref var field = ref _fieldStatic;
        if (field)
            return true;

        field = true;
        try
        {
            return field;
        }
        finally
        {
            field = false;
        }
    }
    
    [Benchmark]
    public bool Lock()
    {
        lock (_lock)
        {
            return NoLock();
        }
    }
    
    [Benchmark]
    public bool InterlockedLock()
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_fieldInterlocked == threadId)
            return true;
            
        while (Interlocked.CompareExchange(ref _fieldInterlocked, threadId, 0) != 0) { }
        try
        {
            var field = _field;
            _field = !field;
            return field;
        }
        finally
        {
            _fieldInterlocked = 0;
        }
    }
}