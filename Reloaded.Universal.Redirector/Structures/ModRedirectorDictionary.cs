using System;
using System.Collections.Generic;
using System.IO;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Utility;

namespace Reloaded.Universal.Redirector.Structures
{
    public class ModRedirectorDictionary : IDisposable
    {
        public Dictionary<string, string>       FileRedirects   { get; set; } = new Dictionary<String,String>(StringComparer.OrdinalIgnoreCase);
        private FileSystemWatcher _watcher;

        /* Creation/Destruction */
        public ModRedirectorDictionary() { }
        public ModRedirectorDictionary(string redirectFolder)
        {
            SetupFileWatcher(redirectFolder);
            SetupFileRedirects(redirectFolder);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        /// <summary>
        /// Attempts to acquire a redirection.
        /// </summary>
        /// <param name="path">The original path of the file.</param>
        /// <param name="newPath">The new path of the file.</param>
        /// <returns>True if it succeeded, else false.</returns>
        public bool GetRedirection(string path, out string newPath)
        {
            var fileRedirects = FileRedirects;

            if (fileRedirects.ContainsKey(path))
            {
                newPath = fileRedirects[path];
                return true;
            }

            newPath = path;
            return false;
        }

        /* Setup the dictionary of file redirections. */
        private void SetupFileRedirects(string redirectFolder)
        {
            if (Directory.Exists(redirectFolder))
            {
                var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var allModFiles = RelativePaths.GetRelativeFilePaths(redirectFolder);
                var appConfig = Program.ModLoader.GetAppConfig();

                foreach (string modFile in allModFiles)
                {
                    string applicationFileLocation = Path.GetDirectoryName(appConfig.AppLocation) + modFile;
                    string modFileLocation = redirectFolder + modFile;
                    applicationFileLocation = Path.GetFullPath(applicationFileLocation);
                    modFileLocation         = Path.GetFullPath(modFileLocation);

                    redirects[applicationFileLocation] = modFileLocation;
                }

                FileRedirects = redirects;
            }
        }

        /* Sets up the FileSystem watcher that will update redirect paths on file add/modify/delete. */
        private void SetupFileWatcher(string redirectFolder)
        {
            if (Directory.Exists(redirectFolder))
            {
                _watcher = new FileSystemWatcher(redirectFolder);
                _watcher.EnableRaisingEvents   = true;
                _watcher.IncludeSubdirectories = true;
                _watcher.Created += (sender, args) => { SetupFileRedirects(redirectFolder); };
                _watcher.Deleted += (sender, args) => { SetupFileRedirects(redirectFolder); };
                _watcher.Renamed += (sender, args) => { SetupFileRedirects(redirectFolder); };
            }
        }
    }
}
