using Netcode;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Reimplements implicit conversion operators that were removed in 1.6 for sealed classes.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public static class ImplicitConversionOperatorsFacade
{
    public static bool NetBool_ToBool(NetBool netField)
    {
        return netField.Value;
    }

    public static int NetDirection_ToInt(NetDirection netField)
    {
        return netField.Value;
    }

    public static int NetInt_ToInt(NetInt netField)
    {
        return netField.Value;
    }

    public static string NetString_ToString(NetString netField)
    {
        return netField.Value;
    }
}
