using System;

namespace StardewModdingAPI.Events;

/// <summary>Events raised when the player provides input using a controller, keyboard, or mouse.</summary>
public interface IInputEvents
{
    /// <summary>Raised after the player presses or releases any buttons on the keyboard, controller, or mouse.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<ButtonsChangedEventArgs> ButtonsChanged;

    /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<ButtonPressedEventArgs> ButtonPressed;

    /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<ButtonReleasedEventArgs> ButtonReleased;

    /// <summary>Raised after the player moves the in-game cursor.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<CursorMovedEventArgs> CursorMoved;

    /// <summary>Raised after the player scrolls the mouse wheel.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<MouseWheelScrolledEventArgs> MouseWheelScrolled;
}
