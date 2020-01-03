using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Structures;

namespace Reloaded.Universal.Redirector
{
    public class Redirector
    {
        private Dictionary<string, ModRedirectorDictionary> _redirections = new Dictionary<string, ModRedirectorDictionary>();
        private ModRedirectorDictionary _customRedirections = new ModRedirectorDictionary();

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
            _redirections[redirectFolder] = new ModRedirectorDictionary(redirectFolder);
        }

        internal void Add(string folderPath, string sourceFolder)
        {
            _redirections[folderPath] = new ModRedirectorDictionary(folderPath, sourceFolder);
        }

        public void Add(IModConfigV1 configuration)
        {
            Add(GetRedirectFolder(configuration.ModId));
        }

        public void Remove(string redirectFolder)
        {
            if (!_redirections.ContainsKey(redirectFolder))
                return;

            _redirections[redirectFolder].Dispose();
            _redirections.Remove(redirectFolder);
        }

        public void Remove(IModConfigV1 configuration)
        {
            Remove(GetRedirectFolder(configuration.ModId));
        }

        public bool TryRedirect(string path, out string newPath)
        {
            // Custom redirections.
            if (_customRedirections.GetRedirection(path, out newPath))
                return true;

            // Doing this in reverse because mods with highest priority get loaded last.
            // We want to look at those mods first.
            var values = _redirections.Values.ToArray();
            for (int i = _redirections.Values.Count - 1; i >= 0; i--)
            {
                if (values[i].GetRedirection(path, out newPath))
                    return true;
            }

            newPath = path;
            return false;
        }

        private string GetRedirectFolder(string modId)
        {
            string modFolder = Program.ModLoader.GetDirectoryForModId(modId);
            return $"{modFolder}\\Redirector";
        }
    }
}
