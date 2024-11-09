using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Projectiles;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="DebuffingProjectile"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class DebuffingProjectileFacade : DebuffingProjectile, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static DebuffingProjectile Constructor(int debuff, int parentSheetIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float xVelocity, float yVelocity, Vector2 startingPosition, GameLocation? location = null, Character? owner = null)
    {
        return new DebuffingProjectile(
            debuff: debuff.ToString(),
            spriteIndex: parentSheetIndex,
            bouncesTillDestruct: bouncesTillDestruct,
            tailLength: tailLength,
            rotationVelocity: rotationVelocity,
            xVelocity: xVelocity,
            yVelocity: yVelocity,
            startingPosition: startingPosition,
            location: location,
            owner: owner
        );
    }


    /*********
    ** Private methods
    *********/
    private DebuffingProjectileFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
