using System.Diagnostics.CodeAnalysis;
using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="NetFields"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public abstract class NetFieldsFacade : NetFields, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "Deliberate to include index in field name")]
    public void AddFields(params INetSerializable[] fields)
    {
        for (int i = 0; i < fields.Length; i++)
            base.AddField(fields[i]);
    }


    /*********
    ** Private methods
    *********/
    private NetFieldsFacade()
        : base(null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
