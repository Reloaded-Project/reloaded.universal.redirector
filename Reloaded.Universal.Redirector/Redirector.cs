using System.Collections.Generic;
using System.Collections.Specialized;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Structures;

namespace Reloaded.Universal.Redirector
{
    public class Redirector
    {
        private List<ModRedirectorDictionary> _redirections = new List<ModRedirectorDictionary>();
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

        public void Add(IModConfigV1 configuration)
        {
            var redirectorTuple = new ModRedirectorDictionary(configuration);
            _redirections.Add(redirectorTuple);
        }

        public void Remove(IModConfigV1 configuration)
        {
            var dictionary = _redirections.Find(x => x.ModConfig.ModId == configuration.ModId);
            dictionary?.Dispose();
        }

        public bool TryRedirect(string path, out string newPath)
        {
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

            newPath = path;
            return false;
        }
    }
}
