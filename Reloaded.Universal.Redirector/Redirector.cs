using System;
using System.Collections.Generic;
using System.Linq;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Structures;

namespace Reloaded.Universal.Redirector
{
    public class Redirector
    {
        private List<ModRedirectorDictionary> _redirections = new List<ModRedirectorDictionary>();
        private ModRedirectorDictionary _customRedirections = new ModRedirectorDictionary();
        private bool _isDisabled = false;

        /* Constructor */
        public Redirector(IEnumerable<IModConfigV1> modConfigurations)
        {
            foreach (var config in modConfigurations)
            {
                Add(config);
            }
        }

        /* Business Logic */
        public void AddCustomRedirect(string oldPath, string newPath)
        {
            _customRedirections.FileRedirects[oldPath] = newPath;
        }

        public void RemoveCustomRedirect(string oldPath)
        {
            _customRedirections.FileRedirects.Remove(oldPath);
        }

        public void Add(string redirectFolder)
        {
            _redirections.Add(new ModRedirectorDictionary(redirectFolder));
        }

        internal void Add(string folderPath, string sourceFolder)
        {
            _redirections.Add(new ModRedirectorDictionary(folderPath, sourceFolder));
        }

        public void Add(IModConfigV1 configuration)
        {
            Add(GetRedirectFolder(configuration.ModId));
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

        public void Remove(IModConfigV1 configuration)
        {
            Remove(GetRedirectFolder(configuration.ModId));
        }

        public bool TryRedirect(string path, out string newPath)
        {
            // Check if disabled.
            newPath = path;
            if (_isDisabled)
                return false;

            // Custom redirections.
            if (_customRedirections.GetRedirection(path, out newPath))
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

        private string GetRedirectFolder(string modId) => Program.ModLoader.GetDirectoryForModId(modId) + "\\Redirector";

        public void Disable() => _isDisabled = true;
        public void Enable() => _isDisabled = false;
    }
}
