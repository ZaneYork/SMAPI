using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.GameData;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Wallpaper"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class WallpaperFacade : Wallpaper, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetString modDataID => base.itemId;


    /*********
    ** Public methods
    *********/
    public virtual ModWallpaperOrFlooring GetModData()
    {
        return base.GetSetData();
    }


    /*********
    ** Private methods
    *********/
    private WallpaperFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
