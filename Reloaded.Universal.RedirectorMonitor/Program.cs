using System;
using System.Drawing;
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

    private IModLoader _modLoader;
    private WeakReference<IRedirectorController> _redirectorController;

    private bool _oldPrintRedirect;

    public static void Main() { }
    public void Start(IModLoaderV1 loader)
    {
#if DEBUG
        Debugger.Launch();
#endif
        _modLoader = (IModLoader)loader;

        // Auto-subscribe on loaded redirector.
        ((ILogger)_modLoader.GetLogger()).WriteLine($"{nameof(RedirectorMonitor)} This mod is deprecated, you can now enable redirection printing in main 'Reloaded File Redirector' mod", Color.Aqua);
        _modLoader.ModLoaded += ModLoaded;
        _modLoader.ModUnloading += ModUnloading;
        SetupEventFromRedirector();
    }

    private void SetupEventFromRedirector()
    {
        _redirectorController = _modLoader.GetController<IRedirectorController>();
        Enable();
    }

    private void ModLoaded(IModV1 mod, IModConfigV1 modConfig)
    {
        if (modConfig.ModId == RedirectorId)
            SetupEventFromRedirector();
    }
    
    private void ModUnloading(IModV1 mod, IModConfigV1 modConfig)
    {
        if (modConfig.ModId == RedirectorId)
            Disable();
    }

    /* Mod loader actions. */
    public void Suspend() => Disable();
    public void Resume() => Enable();
    public void Unload() => Disable();
    public bool CanUnload() => true;
    public bool CanSuspend() => true;
    
    public Action Disposing { get; }

    private void Enable()
    {
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            _oldPrintRedirect = target.SetRedirectorSetting(true, RedirectorSettings.PrintRedirect);
    }
    
    private void Disable()
    {
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.SetRedirectorSetting(_oldPrintRedirect, RedirectorSettings.PrintRedirect);
    }
}