using System.Runtime.InteropServices;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NativeDllTest : BaseHookTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public unsafe void LoadNativeDll_Baseline_UsingLoadLibrary(bool useLdrLoadDll)
    {
        Api.Enable();
        const string dllName = "ReturnCurrentPath.dll";
        const string fuctionName = "GetCurrentDllDirectory";

        // Assert Baseline
        void AssertDllPath(string baseFolder)
        {
            var dllPath = Path.Combine(baseFolder, dllName);
            var lib = LoadDll(useLdrLoadDll, dllPath);
            var getFilePath = (delegate*unmanaged[Cdecl]<nint>)Native.GetProcAddress(lib, fuctionName);
            var filePath = Marshal.PtrToStringUni(getFilePath());
            Assert.Equal(Path.GetFullPath(dllPath), Path.GetFullPath(filePath!));
            FreeLibrary(lib);
        }
        
        // Baseline test.
        AssertDllPath(GetNativeDllPath());

        // Test with DLL redirected into basepath.
        Api.AddRedirectFolder(GetNativeDllPath(), GetBasePath());
        AssertDllPath(GetBasePath());
    }

    private static unsafe nint LoadDll(bool useLdrLoadDll, string filePath)
    {
        if (!useLdrLoadDll)
            return Native.LoadLibraryW(filePath);

        nint handle = 0;
        var newPath = Path.GetFullPath(filePath);
        fixed (char* filePathPtr = newPath)
        {
            var ustr = new Native.UNICODE_STRING(filePathPtr, newPath.Length); 
            LdrLoadDll(null!, 0, ref ustr, ref handle);
            return handle;
        }
    }
    
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int LdrLoadDll(string pathToFile, uint dwFlags, ref Native.UNICODE_STRING moduleFileName, ref IntPtr handle);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int FreeLibrary(IntPtr hModule);
}

/*
#include <windows.h>

static HMODULE module;

extern "C"
{
    __declspec(dllexport) LPCWSTR GetCurrentDllDirectory()
    {
        static WCHAR path[MAX_PATH] = { 0 };
        GetModuleFileName(module, path, MAX_PATH);
        return path;
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    module = hModule;
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
	
    return TRUE;
}
*/