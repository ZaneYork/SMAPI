namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class ModEntry : Mod
    {
        public static float ZoomScale;
        public override void Entry(IModHelper helper)
        {
            new VirtualToggle(helper, this.Monitor);
        }
    }
}
