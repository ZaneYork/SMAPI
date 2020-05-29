using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class GameLocationMethods : GameLocation
    {
        public void playSound(string audioName)
        {
            base.playSound(audioName, StardewValley.Network.NetAudio.SoundContext.Default);
        }
    }
}
