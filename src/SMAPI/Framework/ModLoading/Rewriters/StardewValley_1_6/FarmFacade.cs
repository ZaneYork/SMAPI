using System.Drawing;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Farm"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FarmFacade : Farm, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public Point GetPetStartLocation()
    {
        var petBowl = Game1.player?.getPet()?.GetPetBowl();
        if (petBowl is not null)
            return new Point(petBowl.tileX.Value - 1, petBowl.tileY.Value + 1);

        var petBowlPosition = base.GetStarterPetBowlLocation();
        return new Point((int)petBowlPosition.X - 1, (int)petBowlPosition.Y + 1);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void showShipment(Object item, bool playThrowSound = true)
    {
        base.showShipment(item, playThrowSound);
    }


    /*********
    ** Private methods
    *********/
    private FarmFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
