using System;

namespace StardewModdingAPI;

/// <summary>An API that provides access to a content pack.</summary>
public interface IContentPack
{
    /*********
    ** Accessors
    *********/
    /// <summary>The full path to the content pack's folder.</summary>
    string DirectoryPath { get; }

    /// <summary>The content pack's manifest.</summary>
    IManifest Manifest { get; }

    /// <summary>Provides translations stored in the content pack's <c>i18n</c> folder. See <see cref="IModHelper.Translation"/> for more info.</summary>
    ITranslationHelper Translation { get; }

    /// <summary>An API for loading content assets from the content pack's files.</summary>
    IModContentHelper ModContent { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get whether a given file exists in the content pack.</summary>
    /// <param name="path">The relative file path within the content pack (case-insensitive).</param>
    bool HasFile(string path);

    /// <summary>Read a JSON file from the content pack folder.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="path">The relative file path within the content pack (case-insensitive).</param>
    /// <returns>Returns the deserialized model, or <c>null</c> if the file doesn't exist or is empty.</returns>
    /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
    TModel? ReadJsonFile<TModel>(string path)
        where TModel : class;

    /// <summary>Save data to a JSON file in the content pack's folder.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="path">The relative file path within the content pack (case-insensitive).</param>
    /// <param name="data">The arbitrary data to save.</param>
    /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
    void WriteJsonFile<TModel>(string path, TModel data)
        where TModel : class;
}
