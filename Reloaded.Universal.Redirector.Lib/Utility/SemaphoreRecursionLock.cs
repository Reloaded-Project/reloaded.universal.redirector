using System.Runtime.CompilerServices;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Represents a recursion lock implemented via an interlocked semaphore.
/// This is a lock that basically ensures only 1 thread can run code wrapped around lock and unlock methods.
/// </summary>
public struct SemaphoreRecursionLock
{
    private const int UnusedThreadId = -1;
    
    private int _currentThread = UnusedThreadId;
    private int _numCount = 0;

    /// <summary/>
    public SemaphoreRecursionLock() { }

    /// <summary>
    /// Returns true if locked, else false.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsThisThread(int threadId) => _currentThread == threadId;

    /// <summary>
    /// Locks the current thread, incrementing count by one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Lock(int threadId)
    {
        while (Interlocked.CompareExchange(ref _currentThread, threadId, UnusedThreadId) != UnusedThreadId) { }
        _numCount++;
    }

    /// <summary>
    /// Decrements count by 1 and unlocks the thread if 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unlock()
    {
        if (--_numCount == 0)
            _currentThread = UnusedThreadId;
    }
}