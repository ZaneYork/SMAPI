using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="PathFindController"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class PathFindControllerFacade : PathFindController, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static PathFindController Constructor(Character c, GameLocation location, Point endPoint, int finalFacingDirection, bool eraseOldPathController, bool clearMarriageDialogues = true)
    {
        return new PathFindController(c, location, endPoint, finalFacingDirection, clearMarriageDialogues);
    }

    public static PathFindController Constructor(Character c, GameLocation location, isAtEnd endFunction, int finalFacingDirection, bool eraseOldPathController, endBehavior endBehaviorFunction, int limit, Point endPoint, bool clearMarriageDialogues = true)
    {
        return new PathFindController(c, location, endFunction, finalFacingDirection, endBehaviorFunction, limit, endPoint, clearMarriageDialogues);
    }


    /*********
    ** Private methods
    *********/
    private PathFindControllerFacade()
        : base(null, null, Point.Zero, 0)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
