using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Chest"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ChestFacade : Chest, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetObjectList<Item> items => InventoryToNetObjectList.GetCachedWrapperFor(base.Items);


    /*********
    ** Public methods
    *********/
    public static Chest Constructor(bool playerChest, Vector2 tileLocation, int parentSheetIndex = 130)
    {
        return new Chest(playerChest, tileLocation, parentSheetIndex.ToString());
    }

    public static Chest Constructor(bool playerChest, int parentSheedIndex = 130)
    {
        return new Chest(playerChest, parentSheedIndex.ToString());
    }

    public static Chest Constructor(Vector2 location)
    {
        return new Chest { TileLocation = location };
    }

    public static Chest Constructor(int parent_sheet_index, Vector2 tile_location, int starting_lid_frame, int lid_frame_count)
    {
        return new Chest(parent_sheet_index.ToString(), tile_location, starting_lid_frame, lid_frame_count);
    }

    public static Chest Constructor(int coins, List<Item> items, Vector2 location, bool giftbox = false, int giftboxIndex = 0)
    {
        return new Chest(items, location, giftbox, giftboxIndex);
    }

    public void destroyAndDropContents(Vector2 pointToDropAt, GameLocation location)
    {
        base.destroyAndDropContents(pointToDropAt);
    }

    public void dumpContents(GameLocation location)
    {
        base.dumpContents();
    }

    public void updateWhenCurrentLocation(GameTime time, GameLocation environment)
    {
        base.updateWhenCurrentLocation(time);
    }


    /*********
    ** Private methods
    *********/
    private ChestFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
