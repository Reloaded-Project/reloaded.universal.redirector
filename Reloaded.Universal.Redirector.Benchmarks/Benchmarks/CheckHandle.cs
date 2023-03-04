using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

public partial class CheckHandle : IBenchmark
{
    public IntPtr Handle;
    public FileStream FileStream = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var filePath = Directory.EnumerateFiles(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Desktop))!.FullName).First();
        FileStream = new FileStream(filePath, FileMode.Open);
        Handle = FileStream.SafeFileHandle.DangerousGetHandle();
    }

    [GlobalCleanup]
    public void Cleanup() => FileStream.Dispose();

    [Benchmark]
    public uint GetFileType() => GetFileType(Handle);

    [LibraryImport("kernel32.dll", SetLastError = false)]
    public static partial uint GetFileType(IntPtr hFile);
}