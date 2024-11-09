using System.Collections.Generic;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewValley;
using StardewValley.Inventories;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>A watcher which detects changes to an item inventory.</summary>
internal class InventoryWatcher : BaseDisposableWatcher, ICollectionWatcher<Item>
{
    /*********
    ** Fields
    *********/
    /// <summary>The inventory being watched.</summary>
    private readonly Inventory Inventory;

    /// <summary>The pairs added since the last reset.</summary>
    private readonly ISet<Item> AddedImpl = new HashSet<Item>(new ObjectReferenceComparer<Item>());

    /// <summary>The pairs removed since the last reset.</summary>
    private readonly ISet<Item> RemovedImpl = new HashSet<Item>(new ObjectReferenceComparer<Item>());


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsChanged => this.AddedImpl.Count > 0 || this.RemovedImpl.Count > 0;

    /// <inheritdoc />
    public IEnumerable<Item> Added => this.AddedImpl;

    /// <inheritdoc />
    public IEnumerable<Item> Removed => this.RemovedImpl;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="inventory">The inventory to watch.</param>
    public InventoryWatcher(string name, Inventory inventory)
    {
        this.Name = name;
        this.Inventory = inventory;

        inventory.OnSlotChanged += this.OnSlotChanged;
        inventory.OnInventoryReplaced += this.OnInventoryReplaced;
    }

    /// <inheritdoc />
    public void Reset()
    {
        this.AddedImpl.Clear();
        this.RemovedImpl.Clear();
    }

    /// <inheritdoc />
    public void Update()
    {
        this.AssertNotDisposed();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!this.IsDisposed)
        {
            this.Inventory.OnSlotChanged -= this.OnSlotChanged;
            this.Inventory.OnInventoryReplaced -= this.OnInventoryReplaced;
        }

        base.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>A callback invoked when the value list is replaced.</summary>
    /// <param name="inventory">The net field whose values changed.</param>
    /// <param name="oldValues">The previous list of values.</param>
    /// <param name="newValues">The new list of values.</param>
    private void OnInventoryReplaced(Inventory inventory, IList<Item> oldValues, IList<Item> newValues)
    {
        ISet<Item> oldSet = new HashSet<Item>(oldValues, new ObjectReferenceComparer<Item>());
        ISet<Item> changed = new HashSet<Item>(newValues, new ObjectReferenceComparer<Item>());

        foreach (Item value in oldSet)
        {
            if (!changed.Contains(value))
                this.Remove(value);
        }
        foreach (Item value in changed)
        {
            if (!oldSet.Contains(value))
                this.Add(value);
        }
    }

    /// <summary>A callback invoked when an entry is replaced.</summary>
    /// <param name="inventory">The inventory whose values changed.</param>
    /// <param name="index">The list index which changed.</param>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    private void OnSlotChanged(Inventory inventory, int index, Item? oldValue, Item? newValue)
    {
        this.Remove(oldValue);
        this.Add(newValue);
    }

    /// <summary>Track an added item.</summary>
    /// <param name="value">The value that was added.</param>
    private void Add(Item? value)
    {
        if (value == null)
            return;

        if (this.RemovedImpl.Contains(value))
        {
            this.AddedImpl.Remove(value);
            this.RemovedImpl.Remove(value);
        }
        else
            this.AddedImpl.Add(value);
    }

    /// <summary>Track a removed item.</summary>
    /// <param name="value">The value that was removed.</param>
    private void Remove(Item? value)
    {
        if (value == null)
            return;

        if (this.AddedImpl.Contains(value))
        {
            this.AddedImpl.Remove(value);
            this.RemovedImpl.Remove(value);
        }
        else
            this.RemovedImpl.Add(value);
    }
}
