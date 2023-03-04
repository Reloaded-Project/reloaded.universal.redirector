// ReSharper disable InconsistentNaming
#pragma warning disable CS1591
namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    // Enumeration for file creation options
    [Flags]
    public enum CreateOptions : uint
    {
        DirectoryFile = 0x00000001,
        WriteThrough = 0x00000002,
        SequentialOnly = 0x00000004,
        NoIntermediateBuffering = 0x00000008,
        SynchronousIoNonAlert = 0x00000010,
        SynchronousIoAlert = 0x00000020,
        NonDirectoryFile = 0x00000040,
        CreateTreeConnection = 0x00000080,
        CompleteIfOplocked = 0x00000100,
        NoEaKnowledge = 0x00000200,
        OpenRemoteInstance = 0x00000400,
        RandomAccess = 0x00000800,
        DeleteOnClose = 0x00001000,
        OpenByFileId = 0x00002000,
        OpenForBackupIntent = 0x00004000,
        NoCompression = 0x00008000,
        OpenRequiringOplock = 0x00010000,
        DisallowExclusive = 0x00020000,
        SessionAware = 0x00040000,
        ReserveOpfilter = 0x00100000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00400000,
        OpenForFreeSpaceQuery = 0x00800000
    }
    
    public enum CreateDisposition : uint
    {
        Supersede = 0x00000000,
        Open = 0x00000001,
        Create = 0x00000002,
        OpenIf = 0x00000003,
        Overwrite = 0x00000004,
        OverwriteIf = 0x00000005
    }
    
    [Flags]
    public enum ACCESS_MASK : uint
    {
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
        STANDARD_RIGHTS_REQUIRED = 0x000F0000,
        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
        STANDARD_RIGHTS_ALL = 0x001F0000,
        SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
        FILE_READ_DATA = 0x0001,
        FILE_LIST_DIRECTORY = 0x0001,
        FILE_WRITE_DATA = 0x0002,
        FILE_ADD_FILE = 0x0002,
        FILE_APPEND_DATA = 0x0004,
        FILE_ADD_SUBDIRECTORY = 0x0004,
        FILE_CREATE_PIPE_INSTANCE = 0x0004,
        FILE_READ_EA = 0x0008,
        FILE_WRITE_EA = 0x0010,
        FILE_EXECUTE = 0x0020,
        FILE_TRAVERSE = 0x0020,
        FILE_DELETE_CHILD = 0x0040,
        FILE_READ_ATTRIBUTES = 0x0080,
        FILE_WRITE_ATTRIBUTES = 0x0100,
        FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF,
        FILE_GENERIC_READ = STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE,
        FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA | SYNCHRONIZE,
        FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE
    }
}