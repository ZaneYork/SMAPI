using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Mods;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IDisplayEvents.RenderedStep"/> event.</summary>
public class RenderedStepEventArgs : EventArgs
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached instance for each render step.</summary>
    private static readonly Dictionary<RenderSteps, RenderedStepEventArgs> Instances = new();


    /*********
    ** Accessors
    *********/
    /// <summary>The current step in the render cycle.</summary>
    public RenderSteps Step { get; }

    /// <summary>The sprite batch being drawn. Add anything you want to appear on-screen to this sprite batch.</summary>
    public SpriteBatch SpriteBatch => Game1.spriteBatch;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="step">The current step in the render cycle.</param>
    public RenderedStepEventArgs(RenderSteps step)
    {
        this.Step = step;
    }

    /// <summary>Get an instance for a render step.</summary>
    /// <param name="step">The current step in the render cycle.</param>
    internal static RenderedStepEventArgs Instance(RenderSteps step)
    {
        if (!RenderedStepEventArgs.Instances.TryGetValue(step, out RenderedStepEventArgs? instance))
            RenderedStepEventArgs.Instances[step] = instance = new(step);

        return instance;
    }
}
