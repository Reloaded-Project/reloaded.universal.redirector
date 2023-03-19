using System.Runtime.CompilerServices;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;

[module: SkipLocalsInit]
namespace Reloaded.Universal.Monitor;

public class Monitor
{
    private const string RedirectorId = "reloaded.universal.redirector";
    
    private WeakReference<IRedirectorController>? _redirectorController;
    private Config _configuration;
    
    private readonly IModLoader _modLoader;
    private readonly ILogger _logger;

    private bool _oldPrintOpen;
    private bool _oldPrintNonFiles;
    
    public Monitor(IModLoader modLoader, ILogger logger, Config configuration)
    {
        _modLoader = modLoader;
        _logger = logger;
        _configuration = configuration;
        _modLoader.ModLoaded += ModLoaded;
        _modLoader.ModUnloading += ModUnloading;
        Enable();
        SetupEventFromRedirector();
    }

    /// <summary>
    /// Updates the used configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void UpdateConfiguration(Config configuration)
    {
        _configuration = configuration;
        Enable();
    }
    
    /// <summary>
    /// Enables the monitor.
    /// </summary>
    public void Enable()
    {
        if (_redirectorController == null || !_redirectorController.TryGetTarget(out var target)) 
            return;
        
        _oldPrintOpen = target.SetRedirectorSetting(true, RedirectorSettings.PrintOpen);
        _oldPrintNonFiles = target.SetRedirectorSetting(_configuration.FilterNonFiles, RedirectorSettings.DontPrintNonFiles);
    }
    
    /// <summary>
    /// Disables the monitor.
    /// </summary>
    public void Disable()
    {
        if (_redirectorController == null || !_redirectorController.TryGetTarget(out var target)) 
            return;
        
        target.SetRedirectorSetting(_oldPrintOpen, RedirectorSettings.PrintOpen);
        target.SetRedirectorSetting(_oldPrintNonFiles, RedirectorSettings.DontPrintNonFiles);
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
}