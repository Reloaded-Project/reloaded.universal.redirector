using System;
using System.Diagnostics;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;

namespace Reloaded.Universal.Monitor
{
    public class Program : IMod
    {
        private const string RedirectorId = "reloaded.universal.redirector";

        private ILogger _logger;
        private IModLoader _modLoader;
        private WeakReference<IRedirectorController> _redirectorController;
        private bool _printLoading = true;

        public void Start(IModLoaderV1 loader)
        {
            #if DEBUG
            Debugger.Launch();
            #endif
            _modLoader = (IModLoader)loader;
            _logger = (ILogger)_modLoader.GetLogger();

            // Auto-subscribe on loaded redirector.
            _modLoader.ModLoaded += ModLoaded;
            SetupEventFromRedirector();
        }

        private void SetupEventFromRedirector()
        {
            _redirectorController = _modLoader.GetController<IRedirectorController>();
            if (_redirectorController != null &&
                _redirectorController.TryGetTarget(out var target))
            {
                target.Loading += TargetOnLoading;
            }
        }

        private void ModLoaded(IModV1 mod, IModConfigV1 modConfig)
        {
            if (modConfig.ModId == RedirectorId)
                SetupEventFromRedirector();
        }

        private void TargetOnLoading(string path)
        {
            if (_printLoading)
                _logger.PrintMessage($"RII File Monitor: {path}", _logger.TextColor);
        }

        /* Mod loader actions. */
        public void Suspend()   => _printLoading = false;
        public void Resume()    => _printLoading = true;

        public void Unload()
        {
            if (_redirectorController != null && 
                _redirectorController.TryGetTarget(out var target))
            {
                target.Loading -= TargetOnLoading;
            }
        }

        public bool CanUnload()     => true;
        public bool CanSuspend()    => true;
        public Action Disposing { get; }
    }
}
