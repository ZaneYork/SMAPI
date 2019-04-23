using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using static VirtualKeyboad.ModConfig;

namespace VirtualKeyboad
{
    class KeyButton
    {
        private readonly IModHelper helper;
        private readonly Rectangle buttonRectangle;
        private readonly int padding;
        private readonly IReflectedMethod RaiseButtonPressed;
        private readonly IReflectedMethod RaiseButtonReleased;
        private readonly IReflectedMethod Legacy_KeyPressed;
        private readonly IReflectedMethod Legacy_KeyReleased;

        private readonly SButton button;
        private readonly float transparency;
        private readonly bool autoHidden;
        private bool raisingPressed = false;
        private bool raisingReleased = false;
        public KeyButton(IModHelper helper, Button buttonDefine)
        {
            this.helper = helper;
            this.buttonRectangle = new Rectangle(buttonDefine.rectangle.X, buttonDefine.rectangle.Y, buttonDefine.rectangle.Width, buttonDefine.rectangle.Height);
            this.padding = buttonDefine.rectangle.Padding;
            this.button = buttonDefine.key;
            this.autoHidden = buttonDefine.autoHidden;
            if (buttonDefine.transparency <= 0.01f || buttonDefine.transparency > 1f)
            {
                buttonDefine.transparency = 0.5f;
            }
            this.transparency = buttonDefine.transparency;

            helper.Events.Display.RenderingHud += this.OnRenderingHud;
            helper.Events.Input.ButtonReleased += this.Input_ButtonReleased;
            helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
            SMDroid.ModEntry entry = helper.Reflection.GetField<SMDroid.ModEntry>(typeof(Game1), "hooks").GetValue();
            object score = helper.Reflection.GetField<object>(entry, "core").GetValue();
            object eventManager = helper.Reflection.GetField<object>(score, "EventManager").GetValue();

            object buttonPressed = helper.Reflection.GetField<object>(eventManager, "ButtonPressed").GetValue();
            object buttonReleased = helper.Reflection.GetField<object>(eventManager, "ButtonReleased").GetValue();
            this.RaiseButtonPressed = helper.Reflection.GetMethod(buttonPressed, "Raise");
            this.RaiseButtonReleased = helper.Reflection.GetMethod(buttonReleased, "Raise");

            object legacyButtonPressed = helper.Reflection.GetField<object>(eventManager, "Legacy_KeyPressed").GetValue();
            object legacyButtonReleased = helper.Reflection.GetField<object>(eventManager, "Legacy_KeyReleased").GetValue();
            this.Legacy_KeyPressed = helper.Reflection.GetMethod(legacyButtonPressed, "Raise");
            this.Legacy_KeyReleased = helper.Reflection.GetMethod(legacyButtonReleased, "Raise");
        }

        private bool shouldTrigger(Vector2 point)
        {
            if (this.autoHidden && Game1.activeClickableMenu != null)
            {
                return false;
            }
            if (!this.buttonRectangle.Contains(point.X * Game1.options.zoomLevel, point.Y * Game1.options.zoomLevel))
            {
                return false;
            }
            //if (Game1.activeClickableMenu != null && !this.buttonRectangle.Contains(point.X, point.Y))
            //{
            //    return false;
            //}
            return true;
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (this.raisingPressed)
            {
                return;
            }
            Vector2 point = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(point)){
                object inputState = this.helper.Reflection.GetField<object>(e, "InputState").GetValue();
                object buttonPressedEventArgs = Activator.CreateInstance(typeof(ButtonPressedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.button, e.Cursor, inputState }, null);
                EventArgsKeyPressed eventArgsKeyPressed = new EventArgsKeyPressed((Keys)this.button);
                try
                {
                    this.raisingPressed = true;
                    this.RaiseButtonPressed.Invoke(new object[] { buttonPressedEventArgs });
                    this.Legacy_KeyPressed.Invoke(new object[] { eventArgsKeyPressed });
                }
                finally
                {
                    this.raisingPressed = false;
                }
            }
        }

        private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (this.raisingReleased)
            {
                return;
            }
            Vector2 point = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(point))
            {
                object inputState = this.helper.Reflection.GetField<object>(e, "InputState").GetValue();
                object buttonReleasedEventArgs = Activator.CreateInstance(typeof(ButtonReleasedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.button, e.Cursor, inputState }, null);
                EventArgsKeyPressed eventArgsKeyReleased = new EventArgsKeyPressed((Keys)this.button);
                try
                {
                    this.raisingReleased = true;
                    this.RaiseButtonReleased.Invoke(new object[] { buttonReleasedEventArgs });
                    this.Legacy_KeyReleased.Invoke(new object[] { eventArgsKeyReleased });
                }
                finally
                {
                    this.raisingReleased = false;
                }
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, EventArgs e)
        {
            if (!Game1.eventUp && (!this.autoHidden || (this.autoHidden && Game1.activeClickableMenu == null)))
            {
                Game1.spriteBatch.Draw(Game1.staminaRect, this.buttonRectangle, Color.LightGray * this.transparency);
                Rectangle shrinkRectangle = new Rectangle(this.buttonRectangle.X + this.padding, this.buttonRectangle.Y + this.padding, this.buttonRectangle.Width - 2 * this.padding, this.buttonRectangle.Height - 2 * this.padding);
                Game1.spriteBatch.Draw(Game1.staminaRect, shrinkRectangle, Color.DarkGray * this.transparency * 0.5f);
                Game1.spriteBatch.DrawString(Game1.dialogueFont, this.button.ToString(), new Vector2(this.buttonRectangle.X + 8, this.buttonRectangle.Y + 8), Color.White * this.transparency);
            }
        }
    }
}
