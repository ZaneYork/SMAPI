using System.Reflection;
using Microsoft.Xna.Framework.Audio;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public interface ISoundBankMethods : ISoundBank
{
    void AddCue(CueDefinition cueDefinition)
    {
        SoundBank soundBank = (SoundBank)this.GetType()
            .GetField("soundBank", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.GetValue(this);
        soundBank?.AddCue(cueDefinition);
    }

    CueDefinition GetCueDefinition(string name)
    {
        SoundBank soundBank = (SoundBank)this.GetType()
            .GetField("soundBank", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.GetValue(this);
        return soundBank?.GetCueDefinition(name);
    }
}
