using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Mods;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IDisplayEvents.RenderingStep"/> event.</summary>
public class RenderingStepEventArgs : EventArgs
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached instance for each render step.</summary>
    private static readonly Dictionary<RenderSteps, RenderingStepEventArgs> Instances = new();


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
    public RenderingStepEventArgs(RenderSteps step)
    {
        this.Step = step;
    }

    /// <summary>Get an instance for a render step.</summary>
    /// <param name="step">The current step in the render cycle.</param>
    internal static RenderingStepEventArgs Instance(RenderSteps step)
    {
        if (!RenderingStepEventArgs.Instances.TryGetValue(step, out RenderingStepEventArgs? instance))
            RenderingStepEventArgs.Instances[step] = instance = new(step);

        return instance;
    }
}
