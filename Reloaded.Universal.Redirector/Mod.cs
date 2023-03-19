#if DEBUG
using System.Diagnostics;
#endif
using FileEmulationFramework.Lib.Utilities;
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

    private string _modId;
    private RedirectorApi _redirectorApi;
    private Logger _logger;
    
    public Mod(ModContext context)
    {
        #if DEBUG
        Debugger.Launch();
        #endif
        _modLoader = context.ModLoader;
        var hooks = context.Hooks;
        var logger = context.Logger;
        var owner = context.Owner;
        var modConfig = context.ModConfig;
        var config = context.Configuration;
        _modId = modConfig.ModId;
        
        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/
        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.
        var modConfigs  = _modLoader.GetActiveMods().Select(x => x.Generic);
        var modsFolder = Path.GetDirectoryName(_modLoader.GetDirectoryForModId(context.ModConfig.ModId));
        _redirectorApi = new RedirectorApi(ModLoaderRedirectorExtensions.Create(modConfigs, _modLoader, modsFolder!));
        _logger = new Logger(logger, config.LogLevel);
        InitSettings(config);
        FileAccessServer.Initialize(hooks!, _redirectorApi, _modLoader.GetDirectoryForModId(modConfig.ModId), _logger);

        _modLoader.AddOrReplaceController<IRedirectorController>(owner, _redirectorApi);
        _modLoader.ModLoading   += ModLoading;
        _modLoader.ModUnloading += ModUnloading;
    }

    private void InitSettings(Config configuration)
    {
        _redirectorApi.SetRedirectorSetting(configuration.PrintRedirections, RedirectorSettings.PrintRedirect);
        _redirectorApi.SetRedirectorSetting(configuration.PrintOpenFiles, RedirectorSettings.PrintOpen);
        _redirectorApi.SetRedirectorSetting(configuration.FilterNonFiles, RedirectorSettings.DontPrintNonFiles);
        if (_logger == null!) 
            return;
        
        _logger.LogLevel = configuration.LogLevel;
        _logger.Info($"[{_modId}] Config Updated: Applying");
    }
    
    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration) => InitSettings(configuration);
    #endregion

    private void ModLoading(IModV1 mod, IModConfigV1 config)   => _redirectorApi.Redirector.Add(config.ModId, _modLoader);
    private void ModUnloading(IModV1 mod, IModConfigV1 config) => _redirectorApi.Redirector.Remove(config.ModId, _modLoader);

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