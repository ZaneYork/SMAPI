using StardewModdingAPI.Framework.Input;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for checking and changing input state.</summary>
    internal class InputHelper : BaseHelper, IInputHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Manages the game's input state.</summary>
        private readonly SInputState InputState;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="inputState">Manages the game's input state.</param>
        public InputHelper(string modID, SInputState inputState)
            : base(modID)
        {
            this.InputState = inputState;
        }

        /// <inheritdoc />
        public ICursorPosition GetCursorPosition()
        {
            return this.InputState.CursorPosition;
        }

        /// <inheritdoc />
        public bool IsDown(SButton button)
        {
            return this.InputState.IsDown(button);
        }

        /// <inheritdoc />
        public bool IsSuppressed(SButton button)
        {
            return this.InputState.IsSuppressed(button);
        }

        /// <inheritdoc />
        public void Suppress(SButton button)
        {
            this.InputState.OverrideButton(button, setDown: false);
        }

        /// <inheritdoc />
        public SButtonState GetState(SButton button)
        {
            return this.InputState.GetState(button);
        }
    }
}
