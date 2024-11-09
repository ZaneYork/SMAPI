using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Tools;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="MeleeWeapon"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class MeleeWeaponFacade : MeleeWeapon, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static MeleeWeapon Constructor(int spriteIndex)
    {
        return new MeleeWeapon(spriteIndex.ToString());
    }

    public bool isScythe(int index = -1)
    {
        return base.isScythe(); // index argument was already ignored
    }

    public static Rectangle getSourceRect(int index)
    {
        return
            ItemRegistry.GetData(ItemRegistry.type_weapon + index)?.GetSourceRect() // get actual source rect if possible
            ?? Game1.getSourceRectForStandardTileSheet(Tool.weaponsTexture, index, Game1.smallestTileSize, Game1.smallestTileSize); // else pre-1.6 logic
    }


    /*********
    ** Private methods
    *********/
    private MeleeWeaponFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
