using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Reloaded.Hooks;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;

namespace Reloaded.Universal.Redirector
{
    public class Program : IMod, IExports
    {
        public static IModLoader ModLoader { get; set; }
        public Type[] GetTypes() => _exports;

        private static Type[] _exports = new[] { typeof(IRedirectorController) };
        private IHook<Native.NtCreateFile> _ntCreateFileHook;
        private RedirectorController _redirectorController;
        private Redirector _redirector;
        private object _lock = new object();

        public void Start(IModLoaderV1 loader)
        {
            #if DEBUG
            Debugger.Launch();
            #endif
            ModLoader = (IModLoader)loader;

            /* Your mod code starts here. */
            var ntdllHandle         = Native.LoadLibraryW("ntdll");
            var ntCreateFilePointer = Native.GetProcAddress(ntdllHandle, "NtCreateFile");
            
            /* Kick off the redirector! */
            if (ntCreateFilePointer != IntPtr.Zero)
            {
                var modConfigs = ModLoader.GetActiveMods().Select(x => x.Generic);
                _redirector = new Redirector(modConfigs);
                ModLoader.ModLoading += ModLoading;
                ModLoader.ModUnloading += ModUnloading;

                if (IntPtr.Size == 4)
                    _ntCreateFileHook = new Reloaded.Hooks.X86.Hook<Native.NtCreateFile>(NtCreateFileHookFn, (long)ntCreateFilePointer).Activate();

                else if (IntPtr.Size == 8)
                    _ntCreateFileHook = new Reloaded.Hooks.X64.Hook<Native.NtCreateFile>(NtCreateFileHookFn, (long)ntCreateFilePointer).Activate();

                _redirectorController = new RedirectorController(_redirector);
                ModLoader.AddOrReplaceController<IRedirectorController>(this, _redirectorController);
            }
        }

        private void ModLoading(IModV1 mod, IModConfigV1 config)
        {
            _redirector.Add(config);
        }

        private void ModUnloading(IModV1 mod, IModConfigV1 config)
        {
            _redirector.Remove(config);
        }

        private void ExecuteWithHookDisabled(Action action)
        {
            _ntCreateFileHook.Disable();
            action();
            _ntCreateFileHook.Enable();
        }

        private int NtCreateFileHookFn(out IntPtr filehandle, FileAccess access, ref Native.OBJECT_ATTRIBUTES objectAttributes, ref Native.IO_STATUS_BLOCK ioStatus, ref long allocSize, uint fileattributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
        {
            // Get name of file to be loaded.
            lock (_lock)
            {
                string oldFilePath = objectAttributes.ObjectName.ToString();
                if (oldFilePath.StartsWith("\\??\\", StringComparison.InvariantCultureIgnoreCase))
                    oldFilePath = oldFilePath.Replace("\\??\\", "");

                oldFilePath = Path.GetFullPath(oldFilePath);

                // Get redirected path.
                ExecuteWithHookDisabled(() => _redirectorController.Loading?.Invoke(oldFilePath));

                if (_redirector.TryRedirect(oldFilePath, out string newFilePath))
                {
                    ExecuteWithHookDisabled(() => _redirectorController.Redirecting?.Invoke(oldFilePath, newFilePath));
                    objectAttributes.ObjectName = new Native.UNICODE_STRING($"\\??\\{newFilePath}");
                    objectAttributes.RootDirectory = IntPtr.Zero;
                }

                return _ntCreateFileHook.OriginalFunction(out filehandle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);
            }
        }

        /* Mod loader interface. */
        public void Suspend()   => _ntCreateFileHook.Disable();
        public void Resume()    => _ntCreateFileHook.Enable();
        public void Unload()    => Suspend();

        public bool CanUnload()  => true;
        public bool CanSuspend() => true;
        public Action Disposing { get; }
    }
}
