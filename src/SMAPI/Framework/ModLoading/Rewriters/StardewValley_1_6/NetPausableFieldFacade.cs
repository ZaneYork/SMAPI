using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="NetPausableField{T,TField,TBaseField}"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public abstract class NetPausableFieldFacade<T, TField, TBaseField> : NetPausableField<T, TField, TBaseField>, IRewriteFacade
    where TBaseField : NetFieldBase<T, TBaseField>, new()
    where TField : TBaseField, new()
{
    /*********
    ** Public methods
    *********/
    public static T op_Implicit(NetPausableField<T, TField, TBaseField> field)
    {
        return field.Value;
    }


    /*********
    ** Private methods
    *********/
    private NetPausableFieldFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
