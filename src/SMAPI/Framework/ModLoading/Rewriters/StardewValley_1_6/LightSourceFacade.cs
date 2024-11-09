using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="LightSource"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class LightSourceFacade : LightSource, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static LightSource Constructor(int textureIndex, Vector2 position, float radius, Color color, LightContext light_context = LightContext.None, long playerID = 0L)
    {
        string id = Guid.NewGuid().ToString("N");

        return new LightSource(id, textureIndex, position, radius, color, light_context, playerID);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static LightSource Constructor(int textureIndex, Vector2 position, float radius, Color color, int identifier, LightContext light_context = LightContext.None, long playerID = 0L)
    {
        return new LightSource(identifier.ToString(), textureIndex, position, radius, color, light_context, playerID);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static LightSource Constructor(int textureIndex, Vector2 position, float radius, LightContext light_context = LightContext.None, long playerID = 0L)
    {
        string id = Guid.NewGuid().ToString("N");
        return new LightSource(id, textureIndex, position, radius, light_context, playerID);
    }


    /*********
    ** Private methods
    *********/
    private LightSourceFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
