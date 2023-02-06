using Reloaded.Universal.Redirector.Lib.Structures;
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib;

[Obsolete]
public class Redirector
{
    private List<ModRedirectorDictionary> _redirections = new();
    private Dictionary<string, string> _customRedirections = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisabled;

    /* Business Logic */
    public void AddCustomRedirect(string oldPath, string newPath)
    {
        _customRedirections[oldPath] = newPath;
    }

    public void RemoveCustomRedirect(string oldPath)
    {
        _customRedirections.Remove(oldPath);
    }

    public void Add(string redirectFolder)
    {
        _redirections.Add(new ModRedirectorDictionary(redirectFolder));
    }

    public void Add(string folderPath, string sourceFolder)
    {
        _redirections.Add(new ModRedirectorDictionary(folderPath, sourceFolder));
    }

    public void Remove(string redirectFolder, string sourceFolder)
    {
        _redirections = _redirections.Where(x => !x.RedirectFolder.Equals(redirectFolder, StringComparison.OrdinalIgnoreCase) &&
                                                 !x.SourceFolder.Equals(sourceFolder, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void Remove(string redirectFolder)
    {
        _redirections = _redirections.Where(x => !x.RedirectFolder.Equals(redirectFolder, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    public bool TryRedirect(string path, out string newPath)
    {
        // Check if disabled.
        newPath = path;
        if (_isDisabled)
            return false;

        // Custom redirections.
        if (_customRedirections.TryGetValue(path, out newPath!))
            return true;

        // Doing this in reverse because mods with highest priority get loaded last.
        // We want to look at those mods first.
        for (int i = _redirections.Count - 1; i >= 0; i--)
        {
            if (_redirections[i].GetRedirection(path, out newPath))
                return true;
        }

        return false;
    }

    public void Disable() => _isDisabled = true;
    public void Enable() => _isDisabled = false;
}