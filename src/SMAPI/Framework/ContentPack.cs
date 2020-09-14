using System;
using System.IO;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework
{
    /// <summary>Manages access to a content pack's metadata and files.</summary>
    internal class ContentPack : IContentPack
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides an API for loading content assets.</summary>
        private readonly IContentHelper Content;

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DirectoryPath { get; }

        /// <inheritdoc />
        public IManifest Manifest { get; }

        /// <inheritdoc />
        public ITranslationHelper Translation { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="directoryPath">The full path to the content pack's folder.</param>
        /// <param name="manifest">The content pack's manifest.</param>
        /// <param name="content">Provides an API for loading content assets.</param>
        /// <param name="translation">Provides translations stored in the content pack's <c>i18n</c> folder.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        public ContentPack(string directoryPath, IManifest manifest, IContentHelper content, ITranslationHelper translation, JsonHelper jsonHelper)
        {
            this.DirectoryPath = directoryPath;
            this.Manifest = manifest;
            this.Content = content;
            this.Translation = translation;
            this.JsonHelper = jsonHelper;
        }

        /// <inheritdoc />
        public bool HasFile(string path)
        {
            this.AssertRelativePath(path, nameof(this.HasFile));

            return File.Exists(Path.Combine(this.DirectoryPath, path));
        }

        /// <inheritdoc />
        public TModel ReadJsonFile<TModel>(string path) where TModel : class
        {
            this.AssertRelativePath(path, nameof(this.ReadJsonFile));

            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalizePath(path));
            return this.JsonHelper.ReadJsonFileIfExists(path, out TModel model)
                ? model
                : null;
        }

        /// <inheritdoc />
        public void WriteJsonFile<TModel>(string path, TModel data) where TModel : class
        {
            this.AssertRelativePath(path, nameof(this.WriteJsonFile));

            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalizePath(path));
            this.JsonHelper.WriteJsonFile(path, data);
        }

        /// <inheritdoc />
        public T LoadAsset<T>(string key)
        {
            return this.Content.Load<T>(key, ContentSource.ModFolder);
        }

        /// <inheritdoc />
        public string GetActualAssetKey(string key)
        {
            return this.Content.GetActualAssetKey(key, ContentSource.ModFolder);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that a relative path was passed it to a content pack method.</summary>
        /// <param name="path">The path to check.</param>
        /// <param name="methodName">The name of the method which was invoked.</param>
        private void AssertRelativePath(string path, string methodName)
        {
            if (!PathUtilities.IsSafeRelativePath(path))
                throw new InvalidOperationException($"You must call {nameof(IContentPack)}.{methodName} with a relative path.");
        }
    }
}
