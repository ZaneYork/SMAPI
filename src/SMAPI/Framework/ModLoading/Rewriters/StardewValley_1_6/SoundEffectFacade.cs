using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="SoundEffect"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SoundEffectFacade : SoundEffect, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static SoundEffect FromStream(Stream stream)
    {
        return SoundEffect.FromStream(stream);
    }


    /*********
    ** Private methods
    *********/
    private SoundEffectFacade()
        : base(null, 0, AudioChannels.Mono)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
