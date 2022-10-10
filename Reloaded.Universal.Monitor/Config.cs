using System.ComponentModel;
using Reloaded.Universal.Monitor.Template.Configuration;

namespace Reloaded.Universal.Monitor;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.

        By default, configuration saves as "Config.json" in mod user config folder.    
        Need more config files/classes? See Configuration.cs

        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    [DisplayName("Print Async")]
    [Description("Improves performance, but messages might not print realtime.")]
    [DefaultValue(false)]
    public bool PrintAsync { get; set; } = false;
    
    [DisplayName("Filter Non-Files")]
    [Description("Tries to filter out non-files from the output.")]
    [DefaultValue(true)]
    public bool FilterNonFiles { get; set; } = true;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}