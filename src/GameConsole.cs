using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    class GameConsole : IClickableMenu
    {
        public static GameConsole Instance;
        public bool IsVisible;

        private readonly LinkedList<KeyValuePair<ConsoleLogLevel, string>> _consoleMessageQueue = new LinkedList<KeyValuePair<ConsoleLogLevel, string>>();
        private readonly TextBox Textbox;
        private Rectangle TextboxBounds;

        private SpriteFont _smallFont;

        internal GameConsole()
        {
            Instance = this;
            this.IsVisible = true;
            this.Textbox = new TextBox(null, null, Game1.dialogueFont, Game1.textColor)
            {
                X = 0,
                Y = 0,
                Width = 1280,
                Height = 320
            };
            this.TextboxBounds = new Rectangle(this.Textbox.X, this.Textbox.Y, this.Textbox.Width, this.Textbox.Height);
        }

        internal void InitContent(LocalizedContentManager content)
        {
            this._smallFont = content.Load<SpriteFont>(@"Fonts\SmallFont");
        }

        public void Show()
        {
            Game1.activeClickableMenu = this;
            this.IsVisible = true;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.TextboxBounds.Contains(x, y))
            {
                this.Textbox.OnEnterPressed += sender => { SGame.instance.CommandQueue.Enqueue(sender.Text); this.Textbox.Text = ""; };
                Game1.keyboardDispatcher.Subscriber = this.Textbox;
                typeof(TextBox).GetMethod("ShowAndroidKeyboard", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this.Textbox, new object[] { });
            }
            else
            {
                Game1.activeClickableMenu = null;
                this.IsVisible = false;
            }
        }

        public void WriteLine(string consoleMessage, ConsoleLogLevel level)
        {
            lock (this._consoleMessageQueue)
            {
                this._consoleMessageQueue.AddFirst(new KeyValuePair<ConsoleLogLevel, string>(level, consoleMessage));
                if (this._consoleMessageQueue.Count > 2000)
                {
                    this._consoleMessageQueue.RemoveLast();
                }
            }
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            Vector2 size = this._smallFont.MeasureString("aA");
            float y = Game1.game1.screen.Height - size.Y * 2;
            lock (this._consoleMessageQueue)
            {
                foreach (var log in this._consoleMessageQueue)
                {
                    string text = log.Value;
                    switch (log.Key)
                    {
                        case ConsoleLogLevel.Critical:
                        case ConsoleLogLevel.Error:
                            spriteBatch.DrawString(this._smallFont, text, new Vector2(16, y), Color.Red);
                            break;
                        case ConsoleLogLevel.Alert:
                        case ConsoleLogLevel.Warn:
                            spriteBatch.DrawString(this._smallFont, text, new Vector2(16, y), Color.Orange);
                            break;
                        case ConsoleLogLevel.Info:
                        case ConsoleLogLevel.Success:
                            spriteBatch.DrawString(this._smallFont, text, new Vector2(16, y), Color.AntiqueWhite);
                            break;
                        case ConsoleLogLevel.Debug:
                        case ConsoleLogLevel.Trace:
                            spriteBatch.DrawString(this._smallFont, text, new Vector2(16, y), Color.LightGray);
                            break;
                        default:
                            spriteBatch.DrawString(this._smallFont, text, new Vector2(16, y), Color.LightGray);
                            break;
                    }

                    size = this._smallFont.MeasureString(text);
                    if (y < 0)
                    {
                        break;
                    }
                    y -= size.Y;
                }
            }
        }
    }
}
