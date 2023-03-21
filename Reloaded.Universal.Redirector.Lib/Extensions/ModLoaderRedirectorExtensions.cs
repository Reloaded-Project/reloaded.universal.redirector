using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace Reloaded.Universal.Redirector.Lib.Extensions;

/// <summary>
/// Extensions 'traits' for the <see cref="Redirector"/> type related to Mod Loader <see cref="IModLoader"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Depends on Mod Loader. No point mocking it in tests.")]
public static class ModLoaderRedirectorExtensions
{
    /// <summary>
    /// Creates a new redirector instance using a specified mod loader and initial set of mods.
    /// </summary>
    /// <param name="modConfigurations">The mod configurations with which to initialise with.</param>
    /// <param name="modLoader">The mod loader in question.</param>
    /// <param name="baseFolder">Folder under which all mods are stored under.</param>
    public static Redirector Create(IEnumerable<IModConfigV1> modConfigurations, IModLoader modLoader, string baseFolder)
    {
        var redirector = new Redirector(baseFolder);
        foreach (var config in modConfigurations)
            redirector.Add(config.ModId, modLoader);

        return redirector;
    }
    
    /// <summary>
    /// Adds a mod ('s `Redirector`) folder to the redirector.
    /// </summary>
    /// <param name="redirector">The redirector to which to use.</param>
    /// <param name="modId">The id of the mod in question.</param>
    /// <param name="modLoader">Instance of the mod loader API.</param>
    public static void Add(this Redirector redirector, string modId, IModLoader modLoader)
    {
        var target = GetRedirectFolder(modLoader, modId);
        if (Directory.Exists(target))
            redirector.Add(target);
    }
    
    /// <summary>
    /// Removes a mod ('s `Redirector`) folder from the redirector.
    /// </summary>
    /// <param name="redirector">The redirector to which to use.</param>
    /// <param name="modId">The id of the mod in question.</param>
    /// <param name="modLoader">Instance of the mod loader API.</param>
    public static void Remove(this Redirector redirector, string modId, IModLoader modLoader)
    {
        redirector.Remove(GetRedirectFolder(modLoader, modId));
    }
    
    /// <summary>
    /// Gets the folder where data to be overlaid/redirected is contained.
    /// </summary>
    /// <param name="modLoader">Instance of the mod loader.</param>
    /// <param name="modId">ID of the individual mod.</param>
    /// <returns>Full path to folder containing data.</returns>
    private static string GetRedirectFolder(this IModLoader modLoader, string modId) => modLoader.GetDirectoryForModId(modId) + "\\Redirector";
}