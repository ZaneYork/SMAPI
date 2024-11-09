using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework;
using StardewValley;
using StardewValley.Events;
using StardewValley.Menus;
using StardewValley.Mods;

namespace StardewModdingAPI.Utilities;

/// <summary>An implementation of <see cref="ModHooks"/> which automatically calls the parent instance for any method that's not overridden.</summary>
/// <remarks>The mod hooks are primarily meant for SMAPI to use. Using this directly in mods is a last resort, since it's very easy to break SMAPI this way. This class requires that SMAPI is present in the parent chain.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Inherited from the game code.")]
public class DelegatingModHooks : ModHooks
{
    /*********
    ** Accessors
    *********/
    /// <summary>The underlying instance to delegate to by default.</summary>
    public ModHooks Parent { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="modHooks">The underlying instance to delegate to by default.</param>
    public DelegatingModHooks(ModHooks modHooks)
    {
        this.AssertSmapiInChain(modHooks);

        this.Parent = modHooks;
    }

    /// <summary>Raised before the in-game clock changes.</summary>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <remarks>In mods, consider using <see cref="IGameLoopEvents.TimeChanged"/> instead.</remarks>
    public override void OnGame1_PerformTenMinuteClockUpdate(Action action)
    {
        this.Parent.OnGame1_PerformTenMinuteClockUpdate(action);
    }

    /// <summary>Raised before initializing the new day and saving.</summary>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <remarks>In mods, consider using <see cref="IGameLoopEvents.DayEnding"/> or <see cref="IGameLoopEvents.Saving"/> instead.</remarks>
    public override void OnGame1_NewDayAfterFade(Action action)
    {
        this.Parent.OnGame1_NewDayAfterFade(action);
    }

    /// <summary>Raised before showing the end-of-day menus (e.g. shipping menus, level-up screen, etc).</summary>
    /// <param name="action">Run the vanilla update logic.</param>
    public override void OnGame1_ShowEndOfNightStuff(Action action)
    {
        this.Parent.OnGame1_ShowEndOfNightStuff(action);
    }

    /// <summary>Raised before updating the gamepad, mouse, and keyboard input state.</summary>
    /// <param name="keyboardState">The keyboard state.</param>
    /// <param name="mouseState">The mouse state.</param>
    /// <param name="gamePadState">The gamepad state.</param>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <remarks>In mods, consider using <see cref="IInputEvents"/> instead.</remarks>
    public override void OnGame1_UpdateControlInput(ref KeyboardState keyboardState, ref MouseState mouseState, ref GamePadState gamePadState, Action action)
    {
        this.Parent.OnGame1_UpdateControlInput(ref keyboardState, ref mouseState, ref gamePadState, action);
    }

    /// <summary>Raised before a location is updated for the local player entering it.</summary>
    /// <param name="location">The location that will be updated.</param>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <remarks>In mods, consider using <see cref="IPlayerEvents.Warped"/> instead.</remarks>
    public override void OnGameLocation_ResetForPlayerEntry(GameLocation location, Action action)
    {
        this.Parent.OnGameLocation_ResetForPlayerEntry(location, action);
    }

    /// <summary>Raised before the game checks for an action to trigger for a player interaction with a tile.</summary>
    /// <param name="location">The location being checked.</param>
    /// <param name="tileLocation">The tile position being checked.</param>
    /// <param name="viewport">The game's current position and size within the map, measured in pixels.</param>
    /// <param name="who">The player interacting with the tile.</param>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <returns>Returns whether the interaction was handled.</returns>
    public override bool OnGameLocation_CheckAction(GameLocation location, xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who, Func<bool> action)
    {
        return this.Parent.OnGameLocation_CheckAction(location, tileLocation, viewport, who, action);
    }

    /// <summary>Raised before the game picks a night event to show on the farm after the player sleeps.</summary>
    /// <param name="action">Run the vanilla update logic.</param>
    /// <returns>Returns the selected farm event.</returns>
    public override FarmEvent OnUtility_PickFarmEvent(Func<FarmEvent> action)
    {
        return this.Parent.OnUtility_PickFarmEvent(action);
    }

    /// <summary>Raised after the player crosses a mutex barrier in the new-day initialization before saving.</summary>
    /// <param name="barrier_id">The barrier ID set in the new-day code.</param>
    public override void AfterNewDayBarrier(string barrier_id)
    {
        this.Parent.AfterNewDayBarrier(barrier_id);
    }

    /// <summary>Raised when creating a new save slot, after the game has added the location instances but before it fully initializes them.</summary>
    public override void CreatedInitialLocations()
    {
        this.Parent.CreatedInitialLocations();
    }

    /// <summary>Raised when loading a save slot, after the game has added the location instances but before it restores their save data. Not applicable when connecting to a multiplayer host.</summary>
    public override void SaveAddedLocations()
    {
        this.Parent.SaveAddedLocations();
    }

    /// <summary>Raised before the game renders content to the screen in the draw loop.</summary>
    /// <param name="step">The render step being started.</param>
    /// <param name="sb">The sprite batch being drawn (which might not always be open yet).</param>
    /// <param name="time">A snapshot of the game timing state.</param>
    /// <param name="target_screen">The render target, if any.</param>
    /// <returns>Returns whether to continue with the render step.</returns>
    public override bool OnRendering(RenderSteps step, SpriteBatch sb, GameTime time, RenderTarget2D target_screen)
    {
        return this.Parent.OnRendering(step, sb, time, target_screen);
    }

    /// <summary>Raised after the game renders content to the screen in the draw loop.</summary>
    /// <param name="step">The render step being started.</param>
    /// <param name="sb">The sprite batch being drawn (which might not always be open yet).</param>
    /// <param name="time">A snapshot of the game timing state.</param>
    /// <param name="target_screen">The render target, if any.</param>
    /// <returns>Returns whether to continue with the render step.</returns>
    public override void OnRendered(RenderSteps step, SpriteBatch sb, GameTime time, RenderTarget2D target_screen)
    {
        this.Parent.OnRendered(step, sb, time, target_screen);
    }

    /// <summary>Draw a menu (or child menu) if possible.</summary>
    /// <param name="menu">The menu to draw.</param>
    /// <param name="draw_menu_action">The action which draws the menu.</param>
    /// <returns>Returns whether the menu was successfully drawn.</returns>
    public override bool TryDrawMenu(IClickableMenu menu, Action draw_menu_action)
    {
        return this.Parent.TryDrawMenu(menu, draw_menu_action);
    }

    /// <summary>Start an asynchronous task for the game.</summary>
    /// <param name="task">The task to start.</param>
    /// <param name="id">A unique key which identifies the task.</param>
    public override Task StartTask(Task task, string id)
    {
        return this.Parent.StartTask(task, id);
    }

    /// <summary>Start an asynchronous task for the game.</summary>
    /// <typeparam name="T">The type returned by the task when it completes.</typeparam>
    /// <param name="task">The task to start.</param>
    /// <param name="id">A unique key which identifies the task.</param>
    public override Task<T> StartTask<T>(Task<T> task, string id)
    {
        return this.Parent.StartTask<T>(task, id);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Assert that SMAPI's mod hook implementation is in the inheritance chain.</summary>
    /// <param name="hooks">The mod hooks to check.</param>
    private void AssertSmapiInChain(ModHooks hooks)
    {
        // this is SMAPI
        if (this is SModHooks)
            return;

        // SMAPI in delegated chain
        for (ModHooks? cur = hooks; cur != null; cur = (cur as DelegatingModHooks)?.Parent)
        {
            if (cur is SModHooks)
                return;
        }

        // SMAPI not found
        throw new InvalidOperationException($"Can't create a {nameof(DelegatingModHooks)} instance without SMAPI's mod hooks in the parent chain.");
    }
}
