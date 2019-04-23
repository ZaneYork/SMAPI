using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace VirtualKeyboad
{
    class ModConfig
    {
        public Button[] buttons { get; set;} = new Button[] {
            new Button(SButton.Q, new Rect(192, 64, 90, 90, 6), 0.5f, true),
            new Button(SButton.I, new Rect(288, 64, 90, 90, 6), 0.5f, true),
            new Button(SButton.O, new Rect(384, 64, 90, 90, 6), 0.5f, true),
            new Button(SButton.P, new Rect(480, 64, 90, 90, 6), 0.5f, true)
        };
        internal class Button {
            public SButton key;
            public Rect rectangle;
            public bool autoHidden;
            public float transparency;
            public Button(SButton key, Rect rectangle, float transparency, bool autoHidden)
            {
                this.key = key;
                this.rectangle = rectangle;
                this.transparency = transparency;
                this.autoHidden = autoHidden;
            }
        }
        internal class Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int Padding;

            public Rect(int x, int y, int width, int height, int padding)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
                this.Padding = padding;
            }
        }
    }
}
