using BenchmarkDotNet.Attributes;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Structures;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

[MemoryDiagnoser, DisassemblyDiagnoser(int.MaxValue, printInstructionAddresses: true, printSource: true)]
public unsafe class RecursionLockCandidatesBenchmark : IBenchmark
{
    private bool _field;
    
    [ThreadStatic]
    private static bool _fieldStatic;
    private static object _lock = new object();
    private int _fieldInterlocked;
    private SemaphoreRecursionLock _semaphore = new();
    private delegate*unmanaged[Stdcall, SuppressGCTransition]<int> _getCurrentThreadId;

    public RecursionLockCandidatesBenchmark()
    {
        var kernel32 = Native.LoadLibraryW("kernelbase.dll");
        if (kernel32 == 0) // old OS
            kernel32 = Native.LoadLibraryW("kernel32.dll");
        
        _getCurrentThreadId = (delegate*unmanaged[Stdcall, SuppressGCTransition]<int>)Native.GetProcAddress(kernel32, "GetCurrentThreadId");
    }

    [Benchmark]
    public bool SemaphoreLock()
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_semaphore.IsThisThread(threadId))
            return true;

        _semaphore.Lock(threadId);
        _semaphore.Unlock();
        return _field;
    }
    
    [Benchmark]
    public unsafe bool SemaphoreLockNativeThreadId()
    {
        var threadId = _getCurrentThreadId();
        if (_semaphore.IsThisThread(threadId))
            return true;

        _semaphore.Lock(threadId);
        _semaphore.Unlock();
        return _field;
    }
    
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
        _fieldInterlocked = 0;
        return _field;
    }
}