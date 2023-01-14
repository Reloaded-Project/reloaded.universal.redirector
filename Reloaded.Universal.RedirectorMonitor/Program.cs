using System;
#if DEBUG
using System.Diagnostics;
#endif
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;

namespace Reloaded.Universal.RedirectorMonitor;

public class Program : IMod
{
    private const string RedirectorId = "reloaded.universal.redirector";

    private ILogger _logger;
    private IModLoader _modLoader;
    private WeakReference<IRedirectorController> _redirectorController;
    private bool _printRedirecting = true;

    public static void Main() { }
    public void Start(IModLoaderV1 loader)
    {
#if DEBUG
        Debugger.Launch();
#endif
        _modLoader = (IModLoader)loader;
        _logger = (ILogger)_modLoader.GetLogger();

        // Auto-subscribe on loaded redirector.
        _modLoader.ModLoaded += ModLoaded;
        _modLoader.ModUnloading += ModUnloading;
        SetupEventFromRedirector();
    }

    private void SetupEventFromRedirector()
    {
        _redirectorController = _modLoader.GetController<IRedirectorController>();
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.Redirecting += Redirecting;
    }

    private void ModLoaded(IModV1 mod, IModConfigV1 modConfig)
    {
        if (modConfig.ModId == RedirectorId)
            SetupEventFromRedirector();
    }
    
    private void ModUnloading(IModV1 mod, IModConfigV1 modConfig)
    {
        if (modConfig.ModId != RedirectorId) 
            return;
        
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.Redirecting -= Redirecting;
    }

    private void Redirecting(string oldPath, string newPath)
    {
        if (!_printRedirecting) 
            return;
        
        _logger.PrintMessage($"RII Redirector Old Path: {oldPath}", _logger.ColorLightBlue);
        _logger.PrintMessage($"RII Redirector New Path: {newPath}", _logger.ColorBlue);
    }

    /* Mod loader actions. */
    public void Suspend()   => _printRedirecting = false;
    public void Resume()    => _printRedirecting = true;

    public void Unload()
    {
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.Redirecting -= Redirecting;
    }

    public bool CanUnload()     => true;
    public bool CanSuspend()    => true;
    public Action Disposing { get; }
}