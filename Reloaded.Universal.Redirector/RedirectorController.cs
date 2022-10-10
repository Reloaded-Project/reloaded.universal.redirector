using Reloaded.Universal.Redirector.Interfaces;

namespace Reloaded.Universal.Redirector;

public class RedirectorController : IRedirectorController
{
    private Redirector _redirector;

    public RedirectorController(Redirector redirector)
    {
        _redirector = redirector;
    }

    public Redirecting? Redirecting { get; set; }
    public Loading? Loading { get; set; }
    public void AddRedirect(string oldFilePath, string newFilePath) => _redirector.AddCustomRedirect(oldFilePath, newFilePath);
    public void RemoveRedirect(string oldFilePath)                  => _redirector.RemoveCustomRedirect(oldFilePath);
    public void AddRedirectFolder(string folderPath)                => _redirector.Add(folderPath);
    public void RemoveRedirectFolder(string folderPath)             => _redirector.Remove(folderPath);
    public void AddRedirectFolder(string folderPath, string sourceFolder) => _redirector.Add(folderPath, sourceFolder);
    public void RemoveRedirectFolder(string folderPath, string sourceFolder) => _redirector.Remove(folderPath, sourceFolder);
    public void Disable() => _redirector.Disable();
    public void Enable() => _redirector.Enable();
}