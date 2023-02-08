using Microsoft.Xna.Framework.Audio;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public interface ISoundBankMethods : ISoundBank
{
    CueDefinition GetCueDefinition(string name);
}
