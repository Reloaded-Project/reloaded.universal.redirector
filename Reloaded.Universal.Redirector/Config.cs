using System.ComponentModel;
using FileEmulationFramework.Lib.Utilities;
using Reloaded.Universal.Redirector.Template.Configuration;

namespace Reloaded.Universal.Redirector;

public class Config : Configurable<Config>
{
    [DisplayName("Log Level")]
    [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
    [DefaultValue(LogSeverity.Warning)]
    public LogSeverity LogLevel { get; set; } = LogSeverity.Information;
    
    [DisplayName("Log Open Files")]
    [Description("Logs files opened by the problem.")]
    [DefaultValue(false)]
    public bool PrintOpenFiles { get; set; } = false;
    
    [DisplayName("Filter Non-Files")]
    [Description("Tries to filter out non-files from the Log Open Files output.")]
    [DefaultValue(false)]
    public bool FilterNonFiles { get; set; } = false;
    
    [DisplayName("Log Redirections")]
    [Description("Logs when files are redirected to the console.")]
    [DefaultValue(false)]
    public bool PrintRedirections { get; set; } = false;
    
    [DisplayName("Log Attribute Fetches")]
    [Description("Logs when files' attroibutes are .")]
    [DefaultValue(false)]
    public bool PrintGetAttributes { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}