using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static StardewModdingAPI.Mods.VirtualKeyboard.ModConfig;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class KeyButton
    {
        private readonly Rectangle ButtonRectangle;

        private readonly SButton ButtonKey;
        private readonly float Transparency;
        private readonly string Alias;
        private readonly string Command;
        public bool Hidden;
        private bool RaisingPressed;
        private bool RaisingReleased;

        public KeyButton(IModHelper helper, VirtualButton buttonDefine, IMonitor monitor)
        {
            this.Hidden = true;
            this.ButtonRectangle = new Rectangle(buttonDefine.rectangle.X, buttonDefine.rectangle.Y, buttonDefine.rectangle.Width, buttonDefine.rectangle.Height);
            this.ButtonKey = buttonDefine.key;

            if (buttonDefine.alias == null)
                this.Alias = this.ButtonKey.ToString();
            else
                this.Alias = buttonDefine.alias;
            this.Command = buttonDefine.command;

            if (buttonDefine.transparency <= 0.01f || buttonDefine.transparency > 1f)
            {
                buttonDefine.transparency = 0.5f;
            }
            this.Transparency = buttonDefine.transparency;

            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonReleased += this.EventInputButtonReleased;
            helper.Events.Input.ButtonPressed += this.EventInputButtonPressed;
        }

        private bool ShouldTrigger(Vector2 screenPixels, SButton button)
        {
            if (this.ButtonRectangle.Contains(screenPixels.X, screenPixels.Y) && !this.Hidden && button == SButton.MouseLeft)
            {
                if (!this.Hidden)
                    Toolbar.toolbarPressed = true;
                return true;
            }
            return false;
        }

        private void EventInputButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (this.RaisingPressed)
            {
                return;
            }

            Vector2 screenPixels = e.Cursor.ScreenPixels;
            if (this.ButtonKey != SButton.None && this.ShouldTrigger(screenPixels, e.Button))
            {
                this.RaisingPressed = true;
                SInputState input = Game1.input as SInputState;
                input?.OverrideButton(this.ButtonKey, true);
                this.RaisingPressed = false;
            }
        }

        private void EventInputButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (this.RaisingReleased)
            {
                return;
            }

            Vector2 screenPixels = e.Cursor.ScreenPixels;
            if (this.ShouldTrigger(screenPixels, e.Button))
            {
                if (this.ButtonKey == SButton.RightWindows)
                {
                    KeyboardInput.Show("Command", "").ContinueWith(delegate (Task<string> s) {
                        string command;
                        command = s.Result;
                        if (command.Length > 0)
                        {
                            this.SendCommand(command);
                        }
                        return command;
                    });
                    return;
                }
                if (this.ButtonKey == SButton.RightControl)
                {
                    SGameConsole.Instance.Show();
                    return;
                }
                if (!string.IsNullOrEmpty(this.Command))
                {
                    this.SendCommand(this.Command);
                    return;
                }
                this.RaisingReleased = true;
                SInputState input = Game1.input as SInputState;
                input?.OverrideButton(this.ButtonKey, false);
                this.RaisingReleased = false;
            }
        }

        private void SendCommand(string command)
        {
            SCore score = SMainActivity.Instance.core;
            CommandQueue commandQueue = score.RawCommandQueue;
            if (commandQueue != null)
            {
                commandQueue.Add(command);
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, EventArgs e)
        {
            if (!this.Hidden)
            {
                float scale = this.Transparency;
                if (!Game1.eventUp && Game1.activeClickableMenu is GameMenu == false && Game1.activeClickableMenu is ShopMenu == false && Game1.activeClickableMenu is IClickableMenu == false)
                {
                    scale *= 0.5f;
                }
                System.Reflection.FieldInfo spriteEffectField = Game1.spriteBatch.GetType().GetField("_spriteEffect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                SpriteEffect originSpriteEffect = spriteEffectField?.GetValue(Game1.spriteBatch) as SpriteEffect;
                var originMatrix = originSpriteEffect?.TransformMatrix;
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(1f));
                IClickableMenu.drawTextureBoxWithIconAndText(Game1.spriteBatch, Game1.smallFont, Game1.mouseCursors, new Rectangle(0x100, 0x100, 10, 10), null, new Rectangle(0, 0, 1, 1),
                    this.Alias, this.ButtonRectangle.X, this.ButtonRectangle.Y, this.ButtonRectangle.Width, this.ButtonRectangle.Height, Color.BurlyWood * scale, 4f,
                    true, false, true, false, false, false, false); // Remove bold to fix the text position issue
                Game1.spriteBatch.End();
                if(originMatrix != null)
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
}
