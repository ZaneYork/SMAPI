using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="AnimalHouse"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class AnimalHouseFacade : AnimalHouse, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public Building getBuilding()
    {
        return base.ParentBuilding; // new in 1.6.9
    }


    /*********
    ** Private methods
    *********/
    private AnimalHouseFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
