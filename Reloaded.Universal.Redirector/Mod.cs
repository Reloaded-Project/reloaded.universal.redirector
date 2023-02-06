using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;
using Reloaded.Universal.Redirector.Lib.Extensions;
using Reloaded.Universal.Redirector.Template;

namespace Reloaded.Universal.Redirector;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase, IExports // <= Do not Remove.
{
    /// <summary>
    /// Reports our controller as an exportable interface.
    /// </summary>
    public Type[] GetTypes() => new[] { typeof(IRedirectorController) };
    
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;
    
    private RedirectorApi _redirectorApi;
    private Lib.Redirector _redirector;
    
    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        
        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        var modConfigs  = _modLoader.GetActiveMods().Select(x => x.Generic);
        _redirector = ModLoaderRedirectorExtensions.Create(modConfigs, _modLoader);
        _redirectorApi = new RedirectorApi(_redirector);
        FileAccessServer.Initialize(_hooks!, _redirector, _redirectorApi);

        _modLoader.AddOrReplaceController<IRedirectorController>(_owner, _redirectorApi);
        _modLoader.ModLoading   += ModLoading;
        _modLoader.ModUnloading += ModUnloading;
    }
    
    private void ModLoading(IModV1 mod, IModConfigV1 config)   => _redirector.Add(config.ModId, _modLoader);
    private void ModUnloading(IModV1 mod, IModConfigV1 config) => _redirector.Remove(config.ModId, _modLoader);

    public override void Suspend() => FileAccessServer.Disable();
    public override void Resume()  => FileAccessServer.Enable();
    public override void Unload()  => Suspend();

    public override bool CanUnload()  => true;
    public override bool CanSuspend() => true;

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}