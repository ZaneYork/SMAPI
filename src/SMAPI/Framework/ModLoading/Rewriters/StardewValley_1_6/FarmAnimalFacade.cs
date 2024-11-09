using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="FarmAnimal"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FarmAnimalFacade : FarmAnimal, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public bool isCoopDweller()
    {
        FarmAnimalData? data = base.GetAnimalData();
        return data?.House == "Coop";
    }

    public void warpHome(Farm f, FarmAnimal a)
    {
        base.warpHome();
    }


    /*********
    ** Private methods
    *********/
    private FarmAnimalFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
