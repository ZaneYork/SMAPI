using System;
using StardewValley;

namespace StardewModdingAPI.Events;

/// <summary>Events related to UI and drawing to the screen.</summary>
public interface IDisplayEvents
{
    /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<MenuChangedEventArgs> MenuChanged;

    /// <summary>Raised before the game draws a specific step in the rendering cycle.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderingStepEventArgs> RenderingStep;

    /// <summary>Raised after the game draws a specific step in the rendering cycle.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderedStepEventArgs> RenderedStep;

    /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderingEventArgs> Rendering;

    /// <summary>Raised after the game draws to the sprite batch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderedEventArgs> Rendered;

    /// <summary>Raised before the game world is drawn to the screen. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderingWorldEventArgs> RenderingWorld;

    /// <summary>Raised after the game world is drawn to the sprite batch, before it's rendered to the screen. Content drawn to the sprite batch at this point will be drawn over the world, but under any active menu, HUD elements, or cursor.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderedWorldEventArgs> RenderedWorld;

    /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised before that menu is drawn to the screen. This includes the game's internal menus like the title screen. Content drawn to the sprite batch at this point will appear under the menu.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderingActiveMenuEventArgs> RenderingActiveMenu;

    /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen. Content drawn to the sprite batch at this point will appear over the menu and menu cursor.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderedActiveMenuEventArgs> RenderedActiveMenu;

    /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderingHudEventArgs> RenderingHud;

    /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear over the HUD.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<RenderedHudEventArgs> RenderedHud;

    /// <summary>Raised after the game window is resized.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<WindowResizedEventArgs> WindowResized;
}
