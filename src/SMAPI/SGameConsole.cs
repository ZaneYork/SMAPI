#if SMAPI_FOR_MOBILE
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    public class SGameConsole : IClickableMenu
    {
        public static SGameConsole Instance;
        public bool isVisible;

        private readonly LinkedList<KeyValuePair<ConsoleLogLevel, string>> consoleMessageQueue = new LinkedList<KeyValuePair<ConsoleLogLevel, string>>();
        private Dictionary<string, LinkedList<string>> parseTextCache = new Dictionary<string, LinkedList<string>>();
        private MobileScrollbox scrollbox;
        private MobileScrollbar scrollbar;

        private ClickableTextureComponent commandButton;

        private SpriteFont smallFont;

        private bool scrolling = false;

        private int scrollLastFakeY = 0;

        private int scrollLastY = 0;

        private int MaxScrollBoxHeight => (int)(Game1.graphics.PreferredBackBufferHeight * 100 / Game1.NativeZoomLevel);
        private int ScrollBoxHeight => (int)(Game1.graphics.PreferredBackBufferHeight / Game1.NativeZoomLevel);

        private int MaxTextAreaWidth => (int)((Game1.graphics.PreferredBackBufferWidth - 32) / Game1.NativeZoomLevel);

        internal SGameConsole()
        {
            Instance = this;
            this.isVisible = true;
        }

        internal void InitializeContent(LocalizedContentManager content)
        {
            this.smallFont = content.Load<SpriteFont>(@"Fonts\SmallFont");
            Game1.mobileSpriteSheet = content.Load<Texture2D>(@"LooseSprites\\MobileAtlas_manually_made");
            this.scrollbar = new MobileScrollbar(0, 96, 16, this.ScrollBoxHeight  - 192);
            this.scrollbox = new MobileScrollbox(0, 0, this.MaxTextAreaWidth, this.ScrollBoxHeight, this.MaxScrollBoxHeight,
                new Rectangle(0, 0, (int)(Game1.graphics.PreferredBackBufferWidth / Game1.NativeZoomLevel), this.ScrollBoxHeight),
                this.scrollbar
                );
        }

        public void Show()
        {
            if (this.upperRightCloseButton == null)
                this.initializeUpperRightCloseButton();
            if (this.commandButton == null)
                this.commandButton = new ClickableTextureComponent(new Rectangle(16, 0, 64, 64), Game1.mobileSpriteSheet, new Rectangle(0, 44, 16, 16), 4f, false);
            Game1.activeClickableMenu = this;
            this.isVisible = true;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.scrollbar.sliderContains(x, y) || this.scrollbar.sliderRunnerContains(x, y))
            {
                float num = this.scrollbar.setY(y);
                this.scrollbox.setYOffsetForScroll(-((int)((num * this.scrollbox.getMaxYOffset()) / 100f)));
                Game1.playSound("shwip");
            }

            if (this.upperRightCloseButton.bounds.Contains(x, y))
            {
                this.isVisible = false;
                Game1.activeClickableMenu = null;
                Game1.playSound("bigDeSelect");
            }
            else if (this.commandButton.bounds.Contains(x, y))
            {
                KeyboardInput.Show("Command", "", "", false).ContinueWith<string>(delegate (Task<string> s) {
                    string str;
                    str = s.Result;
                    this.textBoxEnter(str);
                    return str;
                });
            }
            else
            {
                this.scrollLastFakeY = y;
                this.scrollLastY = y;
                this.scrolling = true;
                this.scrollbox.receiveLeftClick(x, y);
            }
        }

        public void textBoxEnter(string text)
        {
            string command = text.Trim();
            if (command.Length > 0)
            {
                if (command.EndsWith(";"))
                {
                    command = command.TrimEnd(';');
                    this.isVisible = false;
                    Game1.activeClickableMenu = null;
                    Game1.playSound("bigDeSelect");
                    SMainActivity.Instance.core.CommandQueue.Enqueue(command);
                    return;
                }
                SMainActivity.Instance.core.CommandQueue.Enqueue(command);
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.scrolling)
            {
                int tmp = y;
                y = this.scrollLastFakeY + this.scrollLastY - y;
                this.scrollLastY = tmp;
                this.scrollLastFakeY = y;
                this.scrollbox.leftClickHeld(x, y);
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            this.scrolling = false;
            this.scrollbox.releaseLeftClick(x, y);
        }

        internal void WriteLine(string consoleMessage, ConsoleLogLevel level)
        {
            lock (this.consoleMessageQueue)
            {
                this.consoleMessageQueue.AddFirst(new KeyValuePair<ConsoleLogLevel, string>(level, consoleMessage));
                if (this.consoleMessageQueue.Count > 2000)
                {
                    this.parseTextCache.Remove(this.consoleMessageQueue.Last.Value.Value);
                    this.consoleMessageQueue.RemoveLast();
                }
            }
        }

        public override void update(GameTime time)
        {
            this.scrollbox.update(time);
        }

        private LinkedList<string> _parseText(string text)
        {
            if (this.parseTextCache.TryGetValue(text, out LinkedList<string> returnString))
            {
                return returnString;
            }
            returnString = new LinkedList<string>();
            string line = string.Empty;
            string[] strings = text.Split("\n");
            foreach (string t in strings)
            {
                if (this.smallFont.MeasureString(t).X < this.MaxTextAreaWidth)
                {
                    returnString.AddFirst(t);
                    continue;
                }
                string[] wordArray = t.Split(' ');
                Vector2 masureResult = new Vector2(0,0);
                foreach (string word in wordArray)
                {
                    masureResult += this.smallFont.MeasureString(word + " ");
                    if (masureResult.X > this.MaxTextAreaWidth)
                    {
                        returnString.AddFirst(line);
                        line = string.Empty;
                        masureResult = new Vector2(0, 0);
                    }
                    line = line + word + ' ';
                }
                returnString.AddFirst(line);
                line = string.Empty;
            }
            this.parseTextCache.TryAdd(text, returnString);
            return returnString;
        }

        public string getLatestCrashText()
        {
            StringBuilder sb = new StringBuilder();
            lock (this.consoleMessageQueue)
            {
                foreach (var log in this.consoleMessageQueue)
                {
                    switch (log.Key)
                    {
                        case ConsoleLogLevel.Critical:
                        case ConsoleLogLevel.Error:
                            sb.Append(log.Value);
                            break;
                        case ConsoleLogLevel.Alert:
                        case ConsoleLogLevel.Warn:
                            sb.Append(log.Value);
                            break;
                    }
                }
            }
            return sb.ToString();
        }

        public override void draw(SpriteBatch b)
        {
            this.scrollbar.draw(b);
            this.scrollbox.setUpForScrollBoxDrawing(b);
            lock (this.consoleMessageQueue)
            {
                float offset = 0;
                Vector2 size = this.smallFont.MeasureString("Aa");
                foreach (var log in this.consoleMessageQueue)
                {
                    LinkedList<string> textArray = this._parseText(log.Value);
                    float baseOffset = Game1.game1.screen.Height - this.scrollbox.getYOffsetForScroll();
                    if (baseOffset - size.Y * textArray.Count - offset > this.ScrollBoxHeight)
                    {
                        offset += size.Y * textArray.Count;
                    }
                    else
                    {
                        foreach (string text in textArray)
                        {
                            float y = baseOffset - size.Y - offset;
                            if (y < -16)
                                continue;
                            offset += size.Y;
                            switch (log.Key)
                            {
                                case ConsoleLogLevel.Critical:
                                case ConsoleLogLevel.Error:
                                    b.DrawString(this.smallFont, text, new Vector2(16, y), Color.Red);
                                    break;
                                case ConsoleLogLevel.Alert:
                                case ConsoleLogLevel.Warn:
                                    b.DrawString(this.smallFont, text, new Vector2(16, y), Color.Orange);
                                    break;
                                case ConsoleLogLevel.Info:
                                case ConsoleLogLevel.Success:
                                    b.DrawString(this.smallFont, text, new Vector2(16, y), Color.AntiqueWhite);
                                    break;
                                case ConsoleLogLevel.Debug:
                                case ConsoleLogLevel.Trace:
                                    b.DrawString(this.smallFont, text, new Vector2(16, y), Color.LightGray);
                                    break;
                                default:
                                    b.DrawString(this.smallFont, text, new Vector2(16, y), Color.LightGray);
                                    break;
                            }
                        }
                    }
                    if (offset > this.MaxScrollBoxHeight)
                    {
                        break;
                    }
                }
            }
            this.scrollbox.finishScrollBoxDrawing(b);
            if (Context.IsWorldReady)
            {
                this.upperRightCloseButton.draw(b);
                this.commandButton.draw(b);
            }
        }
    }
}
#endif
