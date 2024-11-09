using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Projectiles;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Projectile"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public abstract class ProjectileFacade : Projectile, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public static int boundingBoxHeight { get; set; } = Game1.tileSize / 3; // field was never used


    /*********
    ** Private methods
    *********/
    private ProjectileFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
