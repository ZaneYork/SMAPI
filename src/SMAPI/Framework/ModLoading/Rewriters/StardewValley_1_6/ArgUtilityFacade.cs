using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="ArgUtility"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ArgUtilityFacade : IRewriteFacade
{
    /*********
    ** Fields
    *********/
    /// <summary>The default argument name for mods that predate the name field.</summary>
    private const string? DefaultName = null; // ArgUtility will fall back to the old behavior if it's null


    /*********
    ** Public methods
    *********/
    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGet(string[] array, int index, out string value, out string error, bool allowBlank = true)
    {
        return ArgUtility.TryGet(array, index, out value, out error, allowBlank, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptional(string[] array, int index, out string value, out string error, string? defaultValue = null, bool allowBlank = true)
    {
        return ArgUtility.TryGetOptional(array, index, out value, out error, defaultValue, allowBlank, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetBool(string[] array, int index, out bool value, out string error)
    {
        return ArgUtility.TryGetBool(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptionalBool(string[] array, int index, out bool value, out string error, bool defaultValue = false)
    {
        return ArgUtility.TryGetOptionalBool(array, index, out value, out error, defaultValue, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetDirection(string[] array, int index, out int value, out string error)
    {
        return ArgUtility.TryGetDirection(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptionalDirection(string[] array, int index, out int value, out string error, int defaultValue = Game1.up)
    {
        return ArgUtility.TryGetOptionalDirection(array, index, out value, out error, defaultValue, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetEnum<TEnum>(string[] array, int index, out TEnum value, out string error)
        where TEnum : struct
    {
        return ArgUtility.TryGetEnum<TEnum>(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptionalEnum<TEnum>(string[] array, int index, out TEnum value, out string error, TEnum defaultValue = default)
        where TEnum : struct
    {
        return ArgUtility.TryGetOptionalEnum<TEnum>(array, index, out value, out error, defaultValue, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetFloat(string[] array, int index, out float value, out string error)
    {
        return ArgUtility.TryGetFloat(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptionalFloat(string[] array, int index, out float value, out string error, float defaultValue = 0)
    {
        return ArgUtility.TryGetOptionalFloat(array, index, out value, out error, defaultValue, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetInt(string[] array, int index, out int value, out string error)
    {
        return ArgUtility.TryGetInt(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetOptionalInt(string[] array, int index, out int value, out string error, int defaultValue = 0)
    {
        return ArgUtility.TryGetOptionalInt(array, index, out value, out error, defaultValue, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetPoint(string[] array, int index, out Point value, out string error)
    {
        return ArgUtility.TryGetPoint(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetRectangle(string[] array, int index, out Rectangle value, out string error)
    {
        return ArgUtility.TryGetRectangle(array, index, out value, out error, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetVector2(string[] array, int index, out Vector2 value, out string error, bool integerOnly = false)
    {
        return ArgUtility.TryGetVector2(array, index, out value, out error, integerOnly, ArgUtilityFacade.DefaultName);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static bool TryGetRemainder(string[] array, int index, out string value, out string error, char delimiter = ' ')
    {
        return ArgUtility.TryGetRemainder(array, index, out value, out error, delimiter, ArgUtilityFacade.DefaultName);
    }


    /*********
    ** Private methods
    *********/
    private ArgUtilityFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
