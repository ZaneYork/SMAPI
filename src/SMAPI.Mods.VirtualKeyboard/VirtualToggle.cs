using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class VirtualToggle
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;

        private int EnabledStage = 0;
        private bool AutoHidden = true;
        private bool IsDefault = true;
        private ClickableTextureComponent VirtualToggleButton;

        private List<KeyButton> Keyboard = new();
        private List<KeyButton> KeyboardExtend = new();
        private ModConfig ModConfig;
        private Texture2D Texture;
        private int LastPressTick = 0;

        public VirtualToggle(IModHelper helper, IMonitor monitor)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            this.Texture = this.Helper.ModContent.Load<Texture2D>("assets/togglebutton.png");

            this.ModConfig = helper.ReadConfig<ModConfig>();
            for (int i = 0; i < this.ModConfig.buttons.Length; i++)
                this.Keyboard.Add(new KeyButton(helper, this.ModConfig.buttons[i], this.Monitor));
            for (int i = 0; i < this.ModConfig.buttonsExtend.Length; i++)
                this.KeyboardExtend.Add(new KeyButton(helper, this.ModConfig.buttonsExtend[i], this.Monitor));

            if (this.ModConfig.vToggle.rectangle.X != 36 || this.ModConfig.vToggle.rectangle.Y != 12)
                this.IsDefault = false;
            this.AutoHidden = this.ModConfig.vToggle.autoHidden;

            this.VirtualToggleButton = new ClickableTextureComponent(new Rectangle(Game1.toolbarPaddingX + 64, 12, 128, 128), this.Texture, new Rectangle(0, 0, 16, 16), 4f, false);
            helper.WriteConfig(this.ModConfig);

            this.Helper.Events.Display.Rendered += this.OnRendered;
            this.Helper.Events.Display.MenuChanged += this.OnMenuChanged;
            this.Helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if(this.AutoHidden && e.NewMenu != null) {
                foreach (var keys in this.Keyboard)
                {
                    keys.Hidden = true;
                }
                foreach (var keys in this.KeyboardExtend)
                {
                    keys.Hidden = true;
                }
                this.EnabledStage = 0;
            }
        }

        private void VirtualToggleButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);
            if (this.ModConfig.vToggle.key != SButton.None && e.Button == this.ModConfig.vToggle.key)
                this.ToggleLogic();
            else if (e.Button == SButton.MouseLeft && this.ShouldTrigger(screenPixels))
                this.ToggleLogic();
        }

        private void ToggleLogic()
        {
            switch (this.EnabledStage)
            {
                case 0:
                    foreach (var keys in this.Keyboard)
                    {
                        keys.Hidden = false;
                    }

                    foreach (var keys in this.KeyboardExtend)
                    {
                        keys.Hidden = true;
                    }

                    this.EnabledStage = 1;
                    break;
                case 1 when this.KeyboardExtend.Count > 0:
                    foreach (var keys in this.KeyboardExtend)
                    {
                        keys.Hidden = false;
                    }

                    this.EnabledStage = 2;
                    break;
                default:
                    foreach (var keys in this.Keyboard)
                    {
                        keys.Hidden = true;
                    }

                    foreach (var keys in this.KeyboardExtend)
                    {
                        keys.Hidden = true;
                    }

                    this.EnabledStage = 0;
                    if (Game1.activeClickableMenu is IClickableMenu menu && !(Game1.activeClickableMenu is DialogueBox))
                    {
                        menu.exitThisMenu();
                        Toolbar.toolbarPressed = true;
                    }

                    break;
            }
        }

        private bool ShouldTrigger(Vector2 screenPixels)
        {
            int tick = Game1.ticks;
            if(tick - this.LastPressTick <= 6)
            {
                return false;
            }
            if (this.VirtualToggleButton.containsPoint((int)screenPixels.X, (int)screenPixels.Y))
            {
                this.LastPressTick = tick;
                Toolbar.toolbarPressed = true;
                return true;
            }
            return false;
        }

        private void OnRendered(object sender, EventArgs e)
        {
            if (this.IsDefault)
            {
                if (Game1.options.verticalToolbar)
                    this.VirtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 200;
                else
                    this.VirtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 50;

                if (Game1.toolbar.alignTop == true && !Game1.options.verticalToolbar)
                {
                    object toolbarHeight = this.Helper.Reflection.GetField<int>(Game1.toolbar, "toolbarHeight").GetValue();
                    this.VirtualToggleButton.bounds.Y = (int)toolbarHeight + 50;
                }
                else
                {
                    this.VirtualToggleButton.bounds.Y = 12;
                }
            }
            else
            {
                this.VirtualToggleButton.bounds.X = this.ModConfig.vToggle.rectangle.X;
                this.VirtualToggleButton.bounds.Y = this.ModConfig.vToggle.rectangle.Y;
            }

            float scale = 1f;
            if (this.EnabledStage == 0)
            {
                scale = 0.5f;
            }
            if (!Game1.eventUp && Game1.activeClickableMenu is GameMenu == false && Game1.activeClickableMenu is ShopMenu == false)
                scale = 0.25f;

            System.Reflection.FieldInfo spriteEffectField = Game1.spriteBatch.GetType().GetField("_spriteEffect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            SpriteEffect originSpriteEffect = spriteEffectField?.GetValue(Game1.spriteBatch) as SpriteEffect;
            var originMatrix = originSpriteEffect?.TransformMatrix;
            Game1.spriteBatch.End();
            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Microsoft.Xna.Framework.Matrix.CreateScale(1f));
            this.VirtualToggleButton.draw(Game1.spriteBatch, Color.White * scale, 0.000001f);
            Game1.spriteBatch.End();
            if (originMatrix != null)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, (Matrix)originMatrix);
            }
            else
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
        }
    }
}
