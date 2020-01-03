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

        /// <summary>
        /// Creates a mapping from a given folder's files to files in the target application directory.
        /// </summary>
        /// <param name="redirectFolder">Full path of the folder to redirect to.</param>
        /// <param name="sourceFolder">Path of the source folder to redirect from inside the application directory.</param>
        public ModRedirectorDictionary(string redirectFolder, string sourceFolder = "")
        {
            SetupFileWatcher(redirectFolder, sourceFolder);
            SetupFileRedirects(redirectFolder, sourceFolder);
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
        private void SetupFileRedirects(string redirectFolder, string relativeFolder)
        {
            if (Directory.Exists(redirectFolder))
            {
                var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var allModFiles = RelativePaths.GetRelativeFilePaths(redirectFolder);
                var appConfig = Program.ModLoader.GetAppConfig();

                foreach (string modFile in allModFiles)
                {
                    string applicationFileLocation = GetSourceFolderPath(appConfig, relativeFolder) + modFile;
                    string modFileLocation = redirectFolder + modFile;
                    applicationFileLocation = Path.GetFullPath(applicationFileLocation);
                    modFileLocation         = Path.GetFullPath(modFileLocation);

                    redirects[applicationFileLocation] = modFileLocation;
                }

                FileRedirects = redirects;
            }
        }

        /* Sets up the FileSystem watcher that will update redirect paths on file add/modify/delete. */
        private void SetupFileWatcher(string redirectFolder, string relativeFolder)
        {
            if (Directory.Exists(redirectFolder))
            {
                _watcher = new FileSystemWatcher(redirectFolder);
                _watcher.EnableRaisingEvents   = true;
                _watcher.IncludeSubdirectories = true;
                _watcher.Created += (sender, args) => { SetupFileRedirects(redirectFolder, relativeFolder); };
                _watcher.Deleted += (sender, args) => { SetupFileRedirects(redirectFolder, relativeFolder); };
                _watcher.Renamed += (sender, args) => { SetupFileRedirects(redirectFolder, relativeFolder); };
            }
        }

        /* Gets path of the source folder to redirect from. */
        private string GetSourceFolderPath(IApplicationConfigV1 config, string sourceFolder)
        {
            return Path.GetDirectoryName(config.AppLocation) + sourceFolder;
        }
    }
}
