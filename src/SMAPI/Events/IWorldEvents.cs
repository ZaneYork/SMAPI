using System;

namespace StardewModdingAPI.Events;

/// <summary>Events raised when something changes in the world.</summary>
public interface IWorldEvents
{
    /// <summary>Raised after a game location is added or removed.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<LocationListChangedEventArgs> LocationListChanged;

    /// <summary>Raised after buildings are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<BuildingListChangedEventArgs> BuildingListChanged;

    /// <summary>Raised after debris are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<DebrisListChangedEventArgs> DebrisListChanged;

    /// <summary>Raised after large terrain features (like bushes) are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged;

    /// <summary>Raised after NPCs are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<NpcListChangedEventArgs> NpcListChanged;

    /// <summary>Raised after objects are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<ObjectListChangedEventArgs> ObjectListChanged;

    /// <summary>Raised after items are added or removed from a chest.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<ChestInventoryChangedEventArgs> ChestInventoryChanged;

    /// <summary>Raised after terrain features (like floors and trees) are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged;

    /// <summary>Raised after furniture are added or removed in a location.</summary>
    /// <param name="sender">The event sender. This isn't applicable to SMAPI events, and is always null.</param>
    /// <param name="e">The event data.</param>
    event EventHandler<FurnitureListChangedEventArgs> FurnitureListChanged;
}
