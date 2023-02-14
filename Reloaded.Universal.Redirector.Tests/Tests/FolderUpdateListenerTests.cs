using Moq;
using Reloaded.Universal.Redirector.Lib;
using Reloaded.Universal.Redirector.Lib.Interfaces;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests;

/// <summary>
/// Tests related to listening to folder updates.
/// </summary>
public class FolderUpdateListenerTests
{
    [Fact]
    public void Add_New_File()
    {
        // Arrange
        var receiver = new MockUpdateReceiver();
        using var clonedFolder = new TemporaryClonedFolder(Paths.Base);
        using var folderUpdateReceiver = new FolderUpdateListener<IFolderRedirectionUpdateReceiver>(clonedFolder.FolderPath, receiver);
        folderUpdateReceiver.Register(new FolderRedirection(clonedFolder.FolderPath, clonedFolder.FolderPath));

        // Act (Add File)
        var path = Path.Combine(clonedFolder.FolderPath, "test.txt");
        File.Create(path).Dispose();

        // Time for FSW to pick up our changes.
        Thread.Sleep(100);
        
        // Verify
        Assert.True(receiver.CalledOnAddition);
    }
    
    [Fact]
    public void Rename_File()
    {
        // Arrange
        var receiver = new Mock<IFolderRedirectionUpdateReceiver>();
        using var clonedFolder = new TemporaryClonedFolder(Paths.Base);
        using var folderUpdateReceiver = new FolderUpdateListener<IFolderRedirectionUpdateReceiver>(clonedFolder.FolderPath, receiver.Object);
        folderUpdateReceiver.Register(new FolderRedirection(clonedFolder.FolderPath, clonedFolder.FolderPath));

        // Act (Rename File)
        var path = Directory.GetFiles(clonedFolder.FolderPath)[0];
        File.Move(path, $"{path}.uwu");

        // Time for FSW to pick up our changes.
        Thread.Sleep(100);
        
        // Verify
        receiver.Verify(x => x.OnOtherUpdate(It.IsAny<FolderRedirection>()), Times.Once);
    }
    
    [Fact]
    public void Delete_File()
    {
        // Arrange
        var receiver = new Mock<IFolderRedirectionUpdateReceiver>();
        using var clonedFolder = new TemporaryClonedFolder(Paths.Base);
        using var folderUpdateReceiver = new FolderUpdateListener<IFolderRedirectionUpdateReceiver>(clonedFolder.FolderPath, receiver.Object);
        folderUpdateReceiver.Register(new FolderRedirection(clonedFolder.FolderPath, clonedFolder.FolderPath));

        // Act (Delete File)
        var path = Directory.GetFiles(clonedFolder.FolderPath)[0];
        File.Delete(path);

        // Time for FSW to pick up our changes.
        Thread.Sleep(100);
        
        // Verify
        receiver.Verify(x => x.OnOtherUpdate(It.IsAny<FolderRedirection>()), Times.Once);
    }
    
    // Moq can't mock Spans at the moment; so this is needed.
    private class MockUpdateReceiver : IFolderRedirectionUpdateReceiver
    {
        public bool CalledOnAddition { get; set; }
        
        public void OnOtherUpdate(FolderRedirection sender) => throw new NotImplementedException();

        public void OnFileAddition(FolderRedirection sender, ReadOnlySpan<char> relativePath)
        {
            CalledOnAddition = true;
        }
    }
}