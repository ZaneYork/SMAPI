using System;
using Netcode;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;

/// <summary>An implementation of <see cref="NetString"/> which returns a predetermined value and doesn't allow editing.</summary>
internal class ReadOnlyValueToNetString : NetString
{
    /*********
    ** Fields
    *********/
    /// <summary>A human-readable name for the original field to show in error messages.</summary>
    private readonly string FieldLabel;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fieldLabel">A human-readable name for the original field to show in error messages.</param>
    /// <param name="value">The value to set.</param>
    public ReadOnlyValueToNetString(string fieldLabel, string value)
        : base(value)
    {
        this.FieldLabel = fieldLabel;
    }

    /// <inheritdoc />
    public override void Set(string newValue)
    {
        throw new InvalidOperationException($"The {this.FieldLabel} is no longer editable in Stardew Valley 1.6 and later.");
    }
}
