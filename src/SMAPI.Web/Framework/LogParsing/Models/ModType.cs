namespace StardewModdingAPI.Web.Framework.LogParsing.Models;

/// <summary>The type for a <see cref="LogModInfo"/> instance.</summary>
public enum ModType
{
    /// <summary>A special non-mod entry (e.g. for SMAPI or the game itself).</summary>
    Special,

    /// <summary>A C# mod.</summary>
    CodeMod,

    /// <summary>A content pack loaded by a C# mod.</summary>
    ContentPack
}
