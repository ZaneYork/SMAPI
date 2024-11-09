using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="NetFieldBase{T,TSelf}"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public abstract class NetFieldBaseFacade<T, TSelf> : NetFieldBase<T, TSelf> // don't mark IRewriteFacade; the op_Implicit method is mapped manually
    where TSelf : NetFieldBase<T, TSelf>
{
    /*********
    ** Public methods
    *********/
    public static T op_Implicit(NetFieldBase<T, TSelf> netField)
    {
        return netField.Value;
    }


    /*********
    ** Private methods
    *********/
    private NetFieldBaseFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
