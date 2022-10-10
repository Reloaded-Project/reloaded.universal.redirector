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
    private bool _isEnabled = true;
    private Config _configuration;
    
    private readonly IModLoader _modLoader;
    private readonly ILogger _logger;

    public Monitor(IModLoader modLoader, ILogger logger, Config configuration)
    {
        _modLoader = modLoader;
        _logger = logger;
        _configuration = configuration;
        Enable();
        SetupEventFromRedirector();
    }

    /// <summary>
    /// Unloads the mod.
    /// </summary>
    public void Unload()
    {
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.Loading -= PrintOnFileLoad;
    }
    
    /// <summary>
    /// Updates the used configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void UpdateConfiguration(Config configuration) => _configuration = configuration;

    /// <summary>
    /// Disables the mod.
    /// </summary>
    public void Disable()
    {
        _isEnabled = false;
        _modLoader.ModLoaded -= ModLoaded;
    }

    /// <summary>
    /// Re-enables the mod.
    /// </summary>
    public void Enable()
    {
        _isEnabled = true;
        _modLoader.ModLoaded += ModLoaded;
    }
    
    private void SetupEventFromRedirector()
    {
        _redirectorController = _modLoader.GetController<IRedirectorController>();
        if (_redirectorController != null && _redirectorController.TryGetTarget(out var target))
            target.Loading += PrintOnFileLoad;
    }

    private void ModLoaded(IModV1 mod, IModConfigV1 modConfig)
    {
        if (modConfig.ModId == RedirectorId)
            SetupEventFromRedirector();
    }

    private void PrintOnFileLoad(string path)
    {
        if (!_isEnabled)
            return;

        if (_configuration.FilterNonFiles)
        {
            if (path.Contains("usb#vid") || path.Contains("hid#vid"))
                return;
        }
        
        PrintMessage($"RII File Monitor: {path}");
    }

    private void PrintMessage(string text)
    {
        if (_configuration.PrintAsync)
            _logger.WriteLineAsync(text, _logger.TextColor);
        else
            _logger.PrintMessage(text, _logger.TextColor);
    }
}