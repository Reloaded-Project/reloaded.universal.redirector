using Reloaded.Universal.Redirector.Interfaces;

namespace Reloaded.Universal.Redirector;

public class RedirectorApi : IRedirectorController
{
    internal Lib.Redirector Redirector;
    
    /// <summary>
    /// Called when the hooks should be disabled.
    /// </summary>
    public event Action? OnDisable;
    
    /// <summary>
    /// Called when the hooks should be enabled.
    /// </summary>
    public event Action? OnEnable;

    public RedirectorApi(Lib.Redirector redirector)
    {
        Redirector = redirector;
    }

    [Obsolete] 
    public Redirecting? Redirecting { get; set; }
    
    [Obsolete]
    public Loading? Loading { get; set; }
    
    public void AddRedirect(string oldFilePath, string newFilePath) => Redirector.AddCustomRedirect(oldFilePath, newFilePath);
    public void RemoveRedirect(string oldFilePath)                  => Redirector.RemoveCustomRedirect(oldFilePath);
    public void AddRedirectFolder(string folderPath)                => Redirector.Add(folderPath);
    public void RemoveRedirectFolder(string folderPath)             => Redirector.Remove(folderPath);
    public void AddRedirectFolder(string folderPath, string sourceFolder) => Redirector.Add(folderPath, sourceFolder);
    public void RemoveRedirectFolder(string folderPath, string sourceFolder) => Redirector.Remove(folderPath, sourceFolder);
    public void Disable() => OnDisable?.Invoke();
    public void Enable() => OnEnable?.Invoke();

    public bool GetRedirectorSetting(RedirectorSettings setting) => Redirector.GetRedirectorSetting(setting);
    public bool SetRedirectorSetting(bool enable, RedirectorSettings setting) => Redirector.SetRedirectorSetting(enable, setting);
}