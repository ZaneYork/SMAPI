using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Forest"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ForestFacade : Forest, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public ResourceClump? log
    {
        get
        {
            foreach (ResourceClump clump in base.resourceClumps)
            {
                if (clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex && (int)clump.Tile.X == 2 && (int)clump.Tile.Y == 6)
                    return clump;
            }

            return null;
        }
        set
        {
            // remove previous value
            ResourceClump? clump = this.log;
            if (clump != null)
                base.resourceClumps.Remove(clump);

            // add new value
            if (value != null)
                base.resourceClumps.Add(value);
        }
    }


    /*********
    ** Private methods
    *********/
    private ForestFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
