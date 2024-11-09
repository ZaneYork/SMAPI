using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_5;

/// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility with mods written for XNA Framework before Stardew Valley 1.5.5.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SpriteBatchFacade : SpriteBatch, IRewriteFacade
{
    /****
    ** XNA signatures
    ****/
    public void Begin()
    {
        base.Begin();
    }

    public void Begin(SpriteSortMode sortMode, BlendState blendState)
    {
        base.Begin(sortMode, blendState);
    }

    public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
    {
        base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState);
    }

    public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
    {
        base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
    }

    public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
    {
        base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
    }


    /*********
    ** Private methods
    *********/
    private SpriteBatchFacade()
        : base(null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
