using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Framework.Input
{
    /// <summary>Manages mouse state.</summary>
    internal class MouseStateBuilder : IInputStateBuilder<MouseStateBuilder, MouseState>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying mouse state.</summary>
        private MouseState? State;

        /// <summary>The current button states.</summary>
        private IDictionary<SButton, ButtonState> ButtonStates;

        /// <summary>The mouse wheel scroll value.</summary>
        private int ScrollWheelValue;


        /*********
        ** Accessors
        *********/
        /// <summary>The X cursor position.</summary>
        public int X { get; private set; }

        /// <summary>The Y cursor position.</summary>
        public int Y { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="state">The initial state, or <c>null</c> to get the latest state.</param>
        public MouseStateBuilder(MouseState? state = null)
        {
            this.Reset(state);
        }

        /// <summary>Reset the tracked state.</summary>
        /// <param name="state">The state from which to reset, or <c>null</c> to get the latest state.</param>
        public MouseStateBuilder Reset(MouseState? state = null)
        {
            this.State = state ??= Mouse.GetState();

            this.ButtonStates = new Dictionary<SButton, ButtonState>
            {
                [SButton.MouseLeft] = state.Value.LeftButton,
                [SButton.MouseMiddle] = state.Value.MiddleButton,
                [SButton.MouseRight] = state.Value.RightButton,
                [SButton.MouseX1] = state.Value.XButton1,
                [SButton.MouseX2] = state.Value.XButton2
            };
            this.X = state.Value.X;
            this.Y = state.Value.Y;
            this.ScrollWheelValue = state.Value.ScrollWheelValue;

            return this;
        }

        /// <summary>Override the states for a set of buttons.</summary>
        /// <param name="overrides">The button state overrides.</param>
        public MouseStateBuilder OverrideButtons(IDictionary<SButton, SButtonState> overrides)
        {
            foreach (var pair in overrides)
            {
                bool isDown = pair.Value.IsDown();
                if (this.ButtonStates.ContainsKey(pair.Key))
                    this.ButtonStates[pair.Key] = isDown ? ButtonState.Pressed : ButtonState.Released;
            }

            return this;
        }

        /// <summary>Get the currently pressed buttons.</summary>
        public IEnumerable<SButton> GetPressedButtons()
        {
            foreach (var pair in this.ButtonStates)
            {
                if (pair.Value == ButtonState.Pressed)
                    yield return pair.Key;
            }
        }

        /// <summary>Get the equivalent state.</summary>
        public MouseState GetState()
        {
            if (this.State == null)
            {
                this.State = new MouseState(
                    x: this.X,
                    y: this.Y,
                    scrollWheel: this.ScrollWheelValue,
                    leftButton: this.ButtonStates[SButton.MouseLeft],
                    middleButton: this.ButtonStates[SButton.MouseMiddle],
                    rightButton: this.ButtonStates[SButton.MouseRight],
                    xButton1: this.ButtonStates[SButton.MouseX1],
                    xButton2: this.ButtonStates[SButton.MouseX2]
                );
            }

            return this.State.Value;
        }
    }
}
