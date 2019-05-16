using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialisation.Converters;
using StardewModdingAPI.Toolkit.Serialisation.Models;

namespace ModLoader.Common
{
    public class ModInfo
    {
        public ModInfo() { }
        internal ModInfo(IModMetadata metadata)
        {
            this.Metadata = metadata;
            if(metadata != null)
                this.Name = metadata?.DisplayName;
        }

        public string UniqueID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DownloadUrl { get; set; }
        public ISemanticVersion Version { get; set; }
        [JsonConverter(typeof(ManifestDependencyArrayConverter))]
        public IManifestDependency[] Dependencies { get; set; }
        internal IModMetadata Metadata { get; set; }
    }
}
