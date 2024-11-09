using StardewModdingAPI.Framework.ModLoading.Framework;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Layer"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class LayerFacade : Layer, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public void Draw(IDisplayDevice displayDevice, Rectangle mapViewport, Location displayOffset, bool wrapAround, int pixelZoom)
    {
        base.Draw(displayDevice, mapViewport, displayOffset, wrapAround, pixelZoom);
    }


    /*********
    ** Private methods
    *********/
    private LayerFacade()
        : base(null, null, Size.Zero, Size.Zero)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
