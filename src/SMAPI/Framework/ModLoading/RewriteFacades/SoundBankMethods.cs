using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public static class SoundBankMethods
{
    public static void AddCue(this ISoundBank iSoundBank, CueDefinition cueDefinition)
    {
        if (iSoundBank is SoundBankWrapper soundBankWrapper)
        {
            SoundBank soundBank = (SoundBank)typeof(SoundBankWrapper)
                .GetField("soundBank", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(soundBankWrapper);
            soundBank?.AddCue(cueDefinition);
        }
        else
            AccessTools.Method(iSoundBank.GetType(), "AddCue", new[] { typeof(CueDefinition) })
                ?.Invoke(iSoundBank, new[] { cueDefinition });
    }

    public static CueDefinition GetCueDefinition(this ISoundBank iSoundBank, string name)
    {
        if (iSoundBank is SoundBankWrapper soundBankWrapper)
        {
            SoundBank soundBank = (SoundBank)typeof(SoundBankWrapper)
                .GetField("soundBank", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(soundBankWrapper);
            return soundBank?.GetCueDefinition(name);
        }

        return (CueDefinition)AccessTools.Method(iSoundBank.GetType(), "GetCueDefinition", new[] { typeof(string) })
            ?.Invoke(iSoundBank, new[] { name });
    }
}
