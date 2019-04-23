using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace VirtualKeyboad
{
    class ModEntry : Mod
    {
        private List<KeyButton> keyboard = new List<KeyButton>();
        private ModConfig modConfig;
        public override void Entry(IModHelper helper)
        {
            this.modConfig = helper.ReadConfig<ModConfig>();
            for (int i = 0; i < this.modConfig.buttons.Length; i++)
            {
                this.keyboard.Add(new KeyButton(helper, this.modConfig.buttons[i]));
            }
            helper.WriteConfig(this.modConfig);
        }
    }
}
