using System;
using System.IO;
using Reloaded.Hooks.Definitions;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace Reloaded.Universal.Redirector
{
    /// <summary>
    /// Intercepts I/O access on the Win32
    /// </summary>
    public class FileAccessServer
    {
        private Redirector _redirector;
        private RedirectorController _redirectorController;
        private IHook<Native.NtCreateFile> _ntCreateFileHook;
        private object _lock = new object();

        public FileAccessServer(IReloadedHooks hooks, Redirector redirector, RedirectorController redirectorController)
        {
            _redirector = redirector;
            _redirectorController = new RedirectorController(redirector);

            // Get Hooks
            var ntdllHandle = Native.LoadLibraryW("ntdll");
            var ntCreateFilePointer = Native.GetProcAddress(ntdllHandle, "NtCreateFile");

            // Kick off the server
            if (ntCreateFilePointer != IntPtr.Zero)
                _ntCreateFileHook = hooks.CreateHook<Native.NtCreateFile>(NtCreateFileHookFn, (long)ntCreateFilePointer).Activate();
        }

        /* Hooks */

        private unsafe int NtCreateFileHookFn(out IntPtr fileHandle, FileAccess access, ref Native.OBJECT_ATTRIBUTES objectAttributes, ref Native.IO_STATUS_BLOCK ioStatus, ref long allocSize, uint fileattributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
        {
            // Get name of file to be loaded.
            lock (_lock)
            {
                string oldFilePath = objectAttributes.ObjectName->ToString();
                var oldObjectName  = objectAttributes.ObjectName;

                if (TryGetNewPath(oldFilePath, out string newFilePath))
                {
                    var newObjectName = new Native.UNICODE_STRING("\\??\\" + newFilePath);
                    objectAttributes.ObjectName = &newObjectName;
                    objectAttributes.RootDirectory = IntPtr.Zero;
                }

                // Call function with new file path.
                var returnValue = _ntCreateFileHook.OriginalFunction(out fileHandle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                // Dispose if old object name was modified.
                if (oldObjectName != objectAttributes.ObjectName)
                    objectAttributes.ObjectName->Dispose();

                // Restore old object name just in case (prevent potential memory leak).
                objectAttributes.ObjectName = oldObjectName;
                return returnValue;
            }
        }

        private bool TryGetNewPath(string oldFilePath, out string newFilePath)
        {
            oldFilePath = oldFilePath.Replace("\\??\\", "");
            if (!String.IsNullOrEmpty(oldFilePath))
            {
                oldFilePath = Path.GetFullPath(oldFilePath);

                // Get redirected path.
                ExecuteWithHookDisabled(() => _redirectorController.Loading?.Invoke(oldFilePath));

                if (_redirector.TryRedirect(oldFilePath, out newFilePath))
                {
                    string newPath = newFilePath;
                    ExecuteWithHookDisabled(() => _redirectorController.Redirecting?.Invoke(oldFilePath, newPath));
                    return true;
                }
            }

            newFilePath = oldFilePath;
            return false;
        }

        /* Helpers */
        private void ExecuteWithHookDisabled(Action action)
        {
            _ntCreateFileHook.Disable();
            action();
            _ntCreateFileHook.Enable();
        }

        /* Mod loader interface. */
        public void Enable()  => _ntCreateFileHook.Enable();
        public void Disable() => _ntCreateFileHook.Disable();
    }
}
