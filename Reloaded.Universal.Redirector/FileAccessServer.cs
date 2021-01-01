using System;
using System.IO;
using Reloaded.Hooks.Definitions;
using Reloaded.Universal.Redirector.Utility;
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
        private const string _prefix = "\\??\\";

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
                Native.UNICODE_STRING newObjectName;

                if (TryGetNewPath(oldFilePath, out string newFilePath))
                {
                    newObjectName = new Native.UNICODE_STRING(_prefix + newFilePath);
                    objectAttributes.ObjectName = &newObjectName;
                    objectAttributes.RootDirectory = IntPtr.Zero;
                }

                // Call function with new file path.
                var returnValue = _ntCreateFileHook.OriginalFunction(out fileHandle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                // Dispose if old object name was modified and restore original.
                if (oldObjectName != objectAttributes.ObjectName)
                {
                    objectAttributes.ObjectName->Dispose();
                    objectAttributes.ObjectName = oldObjectName;
                }

                return returnValue;
            }
        }

        private bool TryGetNewPath(string oldFilePath, out string newFilePath)
        {
            oldFilePath = oldFilePath.TrimStart(_prefix);
            if (!String.IsNullOrEmpty(oldFilePath))
            {
                oldFilePath = Path.GetFullPath(oldFilePath);

                // Get redirected path.
                if (_redirectorController.Loading != null)
                {
                    _ntCreateFileHook.Disable();
                    _redirectorController.Loading.Invoke(oldFilePath);
                    _ntCreateFileHook.Enable();
                }

                if (_redirector.TryRedirect(oldFilePath, out newFilePath))
                {
                    string newPath = newFilePath;
                    if (_redirectorController.Redirecting != null)
                    {
                        _ntCreateFileHook.Disable();
                        _redirectorController.Redirecting.Invoke(oldFilePath, newPath);
                        _ntCreateFileHook.Enable();
                    }
                    
                    return true;
                }
            }

            newFilePath = oldFilePath;
            return false;
        }

        /* Mod loader interface. */
        public void Enable()  => _ntCreateFileHook.Enable();
        public void Disable() => _ntCreateFileHook.Disable();
    }
}
