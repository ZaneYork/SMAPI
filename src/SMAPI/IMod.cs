namespace StardewModdingAPI;

/// <summary>The implementation for a Stardew Valley mod.</summary>
public interface IMod
{
    /*********
    ** Accessors
    *********/
    /// <summary>Provides simplified APIs for writing mods.</summary>
    IModHelper Helper { get; }

    /// <summary>Writes messages to the console and log file.</summary>
    IMonitor Monitor { get; }

    /// <summary>The mod's manifest.</summary>
    IManifest ModManifest { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    void Entry(IModHelper helper);

    /// <summary>Get an <a href="https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations">API that other mods can access</a>. This is always called after <see cref="Entry"/>, and is only called once even if multiple mods access it.</summary>
    /// <remarks>You can implement <see cref="GetApi()"/> to provide one instance to all mods, or <see cref="GetApi(IModInfo)"/> to provide a separate instance per mod. These are mutually exclusive, so you can only implement one of them.</remarks>
    /// <remarks>Returns the API instance, or <c>null</c> if the mod has no API.</remarks>
    object? GetApi();

    /// <summary>Get an <a href="https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations">API that other mods can access</a>. This is always called after <see cref="Entry"/>, and is called once per mod that accesses the API (even if they access it multiple times).</summary>
    /// <param name="mod">The mod accessing the API.</param>
    /// <remarks>Returns the API instance, or <c>null</c> if the mod has no API. Note that <paramref name="mod"/> is provided for informational purposes only, and that denying API access to specific mods is strongly discouraged and may be considered abusive.</remarks>
    /// <inheritdoc cref="GetApi()" include="/Remarks" />
    object? GetApi(IModInfo mod);
}
