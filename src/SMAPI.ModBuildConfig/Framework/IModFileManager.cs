using System.Collections.Generic;
using System.IO;

namespace StardewModdingAPI.ModBuildConfig.Framework;

/// <summary>Manages the files that are part of a mod in the release package.</summary>
public interface IModFileManager
{
    /// <summary>Get the files in the mod package.</summary>
    public IDictionary<string, FileInfo> GetFiles();
}
