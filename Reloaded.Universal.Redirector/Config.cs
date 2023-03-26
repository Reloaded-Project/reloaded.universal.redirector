using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Template.Configuration;

namespace Reloaded.Universal.Redirector;

// Do not upgrade template without adding trim here
public class Config : Configurable<Config>, IGetTypeInfo<Config>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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

    public static JsonTypeInfo<Config> GetTypeInfo() => ConfigJsonContext.Default.Config;
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Config))]
public partial class ConfigJsonContext : JsonSerializerContext { }

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}

public interface IGetTypeInfo<TParent> where TParent : IGetTypeInfo<TParent>
{
    public static abstract JsonTypeInfo<TParent> GetTypeInfo();
}