using System.IO.Enumeration;
using Microsoft.Win32.SafeHandles;
using Reloaded.Universal.Redirector.Lib.Utility;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Lib.Utility.Native.Structures;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

/// <summary>
/// Tests fetching data for an individual item using <see cref="NtQueryInformationFile"/>
/// and mapping to one of the <see cref="IFileDirectoryInformationDerivative"/> values.
/// </summary>
public class FileDirectoryInformationDerivativeMapping : BaseHookTest
{
    private const int JunkCount = 1024;
    
    [Fact]
    public void Map_FILE_NAMES_INFORMATION()
    {
        Map_COMMON<FILE_NAMES_INFORMATION>((_, _, _) => true);
    }
    
    [Fact]
    public void Map_FILE_DIRECTORY_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_DIRECTORY_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            return true;
        }
        
        Map_COMMON<FILE_DIRECTORY_INFORMATION>(AssertDirectoryInformation);
    }
    
    [Fact]
    public void Map_FILE_BOTH_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_BOTH_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            Assert.Equal(0, infoPtr->ShortNameLength);
            return true;
        }
        
        Map_COMMON<FILE_BOTH_DIR_INFORMATION>(AssertDirectoryInformation);
    }

    [Fact]
    public void Map_FILE_FULL_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_FULL_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            return true;
        }
        
        Map_COMMON<FILE_FULL_DIR_INFORMATION>(AssertDirectoryInformation);
    }

    [Fact]
    public void Map_FILE_ID_BOTH_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_ID_BOTH_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            Assert.Equal(fsInfo->InternalInformation.FileId, infoPtr->FileId);
            Assert.Equal(0, infoPtr->ShortNameLength);
            return true;
        }
        
        Map_COMMON<FILE_ID_BOTH_DIR_INFORMATION>(AssertDirectoryInformation);
    }
    
    [Fact]
    public void Map_FILE_ID_EXTD_BOTH_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_ID_EXTD_BOTH_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            Assert.Equal(fsInfo->InternalInformation.FileId, infoPtr->FileId);
            Assert.Equal(0, infoPtr->ShortNameLength);
            Assert.Equal((uint)0, infoPtr->ReparsePointTag);
            return true;
        }
        
        Map_COMMON<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(AssertDirectoryInformation);
    }
    
    [Fact]
    public void Map_FILE_ID_EXTD_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_ID_EXTD_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            Assert.Equal(fsInfo->InternalInformation.FileId, infoPtr->FileId);
            Assert.Equal((uint)0, infoPtr->ReparsePointTag);
            return true;
        }
        
        Map_COMMON<FILE_ID_EXTD_DIR_INFORMATION>(AssertDirectoryInformation);
    }
    
    [Fact]
    public void Map_FILE_ID_FULL_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_ID_FULL_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->EaInformation.EaSize, infoPtr->EaSize);
            Assert.Equal(fsInfo->InternalInformation.FileId, infoPtr->FileId);
            return true;
        }
        
        Map_COMMON<FILE_ID_FULL_DIR_INFORMATION>(AssertDirectoryInformation);
    }
    
    [Fact]
    public void Map_FILE_ID_GLOBAL_TX_DIR_INFORMATION()
    {
        unsafe bool AssertDirectoryInformation(string filePath, BlittablePointer<FILE_ID_GLOBAL_TX_DIR_INFORMATION> information, nint handle)
        {
            var infoPtr = information.Pointer;
            var fsInfo = NtQueryInformationFileHelper(handle);
            
            Assert.Equal((uint)0, infoPtr->FileIndex);
            Assert.Equal(fsInfo->BasicInformation.CreationTime, infoPtr->CreationTime);
            Assert.Equal(fsInfo->BasicInformation.LastAccessTime, infoPtr->LastAccessTime);
            Assert.Equal(fsInfo->BasicInformation.LastWriteTime, infoPtr->LastWriteTime);
            Assert.Equal(fsInfo->BasicInformation.ChangeTime, infoPtr->ChangeTime);
            Assert.Equal(fsInfo->StandardInformation.EndOfFile, infoPtr->EndOfFile);
            Assert.Equal(fsInfo->StandardInformation.AllocationSize, infoPtr->AllocationSize);
            Assert.Equal(fsInfo->BasicInformation.FileAttributes, infoPtr->FileAttributes);
            Assert.Equal(fsInfo->InternalInformation.FileId, infoPtr->FileId);
            Assert.Equal(default, infoPtr->LockingTransactionId);
            Assert.Equal(default, infoPtr->TxInfoFlags);
            return true;
        }
        
        Map_COMMON<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(AssertDirectoryInformation);
    }
    
    private unsafe void Map_COMMON<T>(Func<string, BlittablePointer<T>, nint, bool> assert) where T : unmanaged, IFileDirectoryInformationDerivative
    {
        using var items = new TemporaryJunkFolder(JunkCount);
        var fileNames = Directory.GetFiles(items.FolderPath);
        var result    = new byte[1024 * 1024 * 32]; // should be enough
        
        fixed (byte* resultPtr = result)
        {
            var info = (T*)resultPtr;
            var lengthRemaining = result.Length;

            for (int x = 0; x < fileNames.Length; x++)
            {
                using var handle = new SafeFileHandle(NtOpenFileOpen(Strings.PrefixLocalDeviceStr + fileNames[x]), true);
                if (!T.TryPopulate(info, lengthRemaining, handle.DangerousGetHandle()))
                    Assert.Fail($"Cannot populate {nameof(T)}");

                // Assert fileName correctly transferred
                var fileName = info->GetFileName(info).ToString();
                Assert.Equal(Path.GetFileName(fileNames[x]), fileName);
                
                // Assert custom
                Assert.True(assert(fileNames[x], info, handle.DangerousGetHandle()));

                lengthRemaining -= info->GetNextEntryOffset();
                info = (T*)((byte*)(info) + info->GetNextEntryOffset());
            }
        }
    }
}