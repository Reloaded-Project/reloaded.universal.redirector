using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Reloaded.Universal.Redirector
{
    internal class Native
    {
        /// <summary>
        /// Creates a new file or directory, or opens an existing file, device, directory, or volume.
        /// (The description here is a partial, lazy copy from MSDN)
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
        [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
        public delegate int NtCreateFile(out IntPtr handle, FileAccess access, ref OBJECT_ATTRIBUTES objectAttributes,
            ref IO_STATUS_BLOCK ioStatus, ref long allocSize, uint fileAttributes, FileShare share, uint createDisposition, uint createOptions,
            IntPtr eaBuffer, uint eaLength);

        /// <summary>
        /// A driver sets an IRP's I/O status block to indicate the final status of an I/O request, before calling IoCompleteRequest for the IRP.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct IO_STATUS_BLOCK
        {
            public UInt32 status;
            public IntPtr information;
        }

        /// <summary>
        /// The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects or object
        /// handles by routines that create objects and/or return handles to objects.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_ATTRIBUTES : IDisposable
        {
            /// <summary>
            /// Lengthm of this structure.
            /// </summary>
            public int Length;

            /// <summary>
            /// Optional handle to the root object directory for the path name specified by the ObjectName member.
            /// If RootDirectory is NULL, ObjectName must point to a fully qualified object name that includes the full path to the target object.
            /// If RootDirectory is non-NULL, ObjectName specifies an object name relative to the RootDirectory directory.
            /// The RootDirectory handle can refer to a file system directory or an object directory in the object manager namespace.
            /// </summary>
            public IntPtr RootDirectory;

            /// <summary>
            /// Pointer to a Unicode string that contains the name of the object for which a handle is to be opened.
            /// This must either be a fully qualified object name, or a relative path name to the directory specified by the RootDirectory member.
            /// </summary>
            private IntPtr objectName;

            /// <summary>
            /// Bitmask of flags that specify object handle attributes. This member can contain one or more of the flags in the following table (See MSDN)
            /// </summary>
            public uint Attributes;

            /// <summary>
            /// Specifies a security descriptor (SECURITY_DESCRIPTOR) for the object when the object is created.
            /// If this member is NULL, the object will receive default security settings.
            /// </summary>
            public IntPtr SecurityDescriptor;

            /// <summary>
            /// Optional quality of service to be applied to the object when it is created.
            /// Used to indicate the security impersonation level and context tracking mode (dynamic or static).
            /// Currently, the InitializeObjectAttributes macro sets this member to NULL.
            /// </summary>
            public IntPtr SecurityQualityOfService;

            /// <summary>
            /// Gets or sets the file path of the files loaded in or out.
            /// </summary>
            public unsafe UNICODE_STRING ObjectName
            {
                get => *(UNICODE_STRING*)objectName;

                set
                {
                    // Check if we need to delete old memory.
                    bool fDeleteOld = objectName != IntPtr.Zero;

                    // Allocates the necessary bytes for the string.
                    if (!fDeleteOld)
                        objectName = Marshal.AllocHGlobal(Marshal.SizeOf(value));

                    // Deallocate old string while writing the new one.
                    Marshal.StructureToPtr(value, objectName, fDeleteOld);
                }
            }

            /// <summary>
            /// Disposes of the actual object name (file name) in question.
            /// </summary>
            public void Dispose()
            {
                if (objectName != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(objectName, typeof(UNICODE_STRING));
                    Marshal.FreeHGlobal(objectName);
                    objectName = IntPtr.Zero;
                }
            }
        }


        /// <summary>
        /// Does this really need to be explained to you?
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            private IntPtr buffer;

            public UNICODE_STRING(string s)
            {
                Length = (ushort)(s.Length * 2);
                MaximumLength = (ushort)(Length + 2);
                buffer = Marshal.StringToHGlobalUni(s);
            }

            /// <summary>
            /// Disposes of the current file name assigned to this Unicode String.
            /// </summary>
            public void Dispose()
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }

            /// <summary>
            /// Returns a string with the contents
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                try
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Memory.Sources.Memory.CurrentProcess.ReadRaw(buffer, out byte[] uniString, Length);
                        return Encoding.Unicode.GetString(uniString);
                    }

                    return "";
                }
                catch { return ""; }
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
}
