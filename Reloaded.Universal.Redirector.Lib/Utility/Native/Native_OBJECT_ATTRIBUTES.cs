using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    /// <summary>
    /// The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects or object
    /// handles by routines that create objects and/or return handles to objects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OBJECT_ATTRIBUTES
    {
        /// <summary>
        /// Length of this structure.
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
        public UNICODE_STRING* ObjectName;

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

        /// <summary/>
        public OBJECT_ATTRIBUTES()
        {
            Length = sizeof(OBJECT_ATTRIBUTES);
            RootDirectory = 0;
            ObjectName = (UNICODE_STRING*)0;
            Attributes = 0;
            SecurityDescriptor = 0;
            SecurityQualityOfService = 0;
        }

        /// <summary>
        /// Tries to obtain the root directory, if it is not null.
        /// </summary>
        /// <returns>True if extracted, else false.</returns>
        public bool TryGetRootDirectory(out string result)
        {
            result = "";
            if (RootDirectory == IntPtr.Zero)
                return false;

            // Cold Path
            var statusBlock = new IO_STATUS_BLOCK();
            var fileNameBuf = Threading.NtQueryInformationFile64K;
            int queryStatus = NtQueryInformationFile(RootDirectory, ref statusBlock, fileNameBuf,
                Threading.Buffer64KLength, FILE_INFORMATION_CLASS.FileNameInformation);
                
            if (queryStatus != 0)
            {
                ThrowHelpers.Win32Exception(queryStatus);
                return false;
            }

            result = FILE_NAME_INFORMATION.GetName((FILE_NAME_INFORMATION*)fileNameBuf);
            return true;
        }
    }
}