using System.Reflection;
using BenchmarkDotNet.Attributes;
using Reloaded.Hooks;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Benchmarks.Benchmarks;

public class HandleOpenBenchmark : IBenchmark
{
    private RedirectorApi _api = null!;
    private string[] _files;

    public HandleOpenBenchmark()
    {
        _files = Directory.GetFiles(@"D:\Games\GOG&DRMFREE", "*", SearchOption.AllDirectories);
    }

    [GlobalSetup(Target = nameof(OpenAllHandles_WithVfs))]
    public void VfsSetup()
    {
        _api = new RedirectorApi(new Lib.Redirector(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!));
        FileAccessServer.Initialize(ReloadedHooks.Instance, _api, AppContext.BaseDirectory);
        _api.Enable();
    }

    [GlobalCleanup(Target = nameof(OpenAllHandles_WithVfs))] 
    public void VfsCleanup() => _api.Disable();

    [Benchmark]
    public void OpenAllHandles_WithVfs() => OpenAllHandles_Common();
    
    [Benchmark(Baseline = true)]
    public void OpenAllHandles_WithoutVfs() => OpenAllHandles_Common();
    
    private void OpenAllHandles_Common()
    {
        foreach (var file in _files)
        {
            var hnd = WinApiHelpers.NtCreateFileOpen(Strings.PrefixLocalDeviceStr + file);
            Native.NtClose(hnd);
        }
    }
}