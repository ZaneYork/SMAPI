namespace StardewModdingAPI.Toolkit.Framework.GameScanning;

/// <summary>The detected validity for a Stardew Valley game folder based on file structure heuristics.</summary>
public enum GameFolderType
{
    /// <summary>The folder seems to contain a valid Stardew Valley 1.5.5+ install.</summary>
    Valid,

    /// <summary>The folder doesn't contain Stardew Valley.</summary>
    NoGameFound,

    /// <summary>The folder contains Stardew Valley 1.5.6 or earlier, which isn't compatible with current versions of SMAPI.</summary>
    LegacyVersion,

    /// <summary>The folder contains Stardew Valley from the game's legacy compatibility branch, which backports newer changes to the <see cref="LegacyVersion"/> format.</summary>
    LegacyCompatibilityBranch,

    /// <summary>The folder seems to contain Stardew Valley files, but they failed to load for unknown reasons (e.g. corrupted executable).</summary>
    InvalidUnknown
}
