using System;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides an API for checking and changing input state.</summary>
internal class InputHelper : BaseHelper, IInputHelper
{
    /*********
    ** Accessors
    *********/
    /// <summary>Manages the game's input state for the current player instance. That may not be the main player in split-screen mode.</summary>
    private readonly Func<SInputState> CurrentInputState;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="currentInputState">Manages the game's input state for the current player instance. That may not be the main player in split-screen mode.</param>
    public InputHelper(IModMetadata mod, Func<SInputState> currentInputState)
        : base(mod)
    {
        this.CurrentInputState = currentInputState;
    }

    /// <inheritdoc />
    public ICursorPosition GetCursorPosition()
    {
        return this.CurrentInputState().CursorPosition;
    }

    /// <inheritdoc />
    public bool IsDown(SButton button)
    {
        return this.CurrentInputState().IsDown(button);
    }

    /// <inheritdoc />
    public bool IsSuppressed(SButton button)
    {
        return this.CurrentInputState().IsSuppressed(button);
    }

    /// <inheritdoc />
    public void Suppress(SButton button)
    {
        this.CurrentInputState().OverrideButton(button, setDown: false);
    }

    /// <inheritdoc />
    public void SuppressScrollWheel()
    {
        Game1.oldMouseState = new MouseState(
            x: Game1.oldMouseState.X,
            y: Game1.oldMouseState.Y,
            scrollWheel: this.CurrentInputState().MouseState.ScrollWheelValue,
            leftButton: Game1.oldMouseState.LeftButton,
            middleButton: Game1.oldMouseState.MiddleButton,
            rightButton: Game1.oldMouseState.RightButton,
            xButton1: Game1.oldMouseState.XButton1,
            xButton2: Game1.oldMouseState.XButton2
        );
    }

    /// <inheritdoc />
    public void SuppressActiveKeybinds(KeybindList keybindList)
    {
        foreach (Keybind keybind in keybindList.Keybinds)
        {
            if (!keybind.GetState().IsDown())
                continue;

            foreach (SButton button in keybind.Buttons)
                this.Suppress(button);
        }
    }

    /// <inheritdoc />
    public SButtonState GetState(SButton button)
    {
        return this.CurrentInputState().GetState(button);
    }
}
