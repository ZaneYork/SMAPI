using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="FruitTree"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FruitTreeFacade : FruitTree, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetString fruitSeason
    {
        get
        {
            List<Season>? seasons = base.GetData()?.Seasons;
            string value = seasons?.Count > 0
                ? string.Join(",", seasons)
                : string.Empty;

            return new ReadOnlyValueToNetString($"{nameof(FruitTree)}.{nameof(this.fruitSeason)}", value);
        }
    }


    /*********
    ** Public methods
    *********/
    public static FruitTree Constructor(int saplingIndex)
    {
        return new FruitTree(saplingIndex.ToString());
    }

    public static FruitTree Constructor(int saplingIndex, int growthStage)
    {
        return new FruitTree(saplingIndex.ToString(), growthStage);
    }

    public bool IsInSeasonHere(GameLocation location)
    {
        return base.IsInSeasonHere();
    }

    public void shake(Vector2 tileLocation, bool doEvenIfStillShaking, GameLocation location)
    {
        base.shake(tileLocation, doEvenIfStillShaking);
    }


    /*********
    ** Private methods
    *********/
    private FruitTreeFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
