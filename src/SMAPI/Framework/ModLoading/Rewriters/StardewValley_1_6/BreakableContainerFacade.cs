using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="BreakableContainer"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class BreakableContainerFacade : BreakableContainer, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static BreakableContainer Constructor(Vector2 tile, int type, MineShaft mine)
    {
        var container = BreakableContainer.GetBarrelForMines(tile, mine);

        if (type.ToString() != BreakableContainer.barrelId)
        {
#pragma warning disable CS0618 // obsolete code -- it's used for its intended purpose here
            container.SetIdAndSprite(type);
#pragma warning restore CS0618
        }

        return container;
    }

    public static BreakableContainer Constructor(Vector2 tile, bool isVolcano)
    {
        return BreakableContainer.GetBarrelForVolcanoDungeon(tile);
    }

    public void releaseContents(GameLocation location, Farmer who)
    {
        base.releaseContents(who);
    }


    /*********
    ** Private methods
    *********/
    private BreakableContainerFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
