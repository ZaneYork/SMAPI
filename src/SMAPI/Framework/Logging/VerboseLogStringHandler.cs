using System.Runtime.CompilerServices;

namespace StardewModdingAPI.Framework.Logging;

/// <summary>An interpolated string handler to handle verbose logging.</summary>
[InterpolatedStringHandler]
public ref struct VerboseLogStringHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying interpolated string handler.</summary>
    private DefaultInterpolatedStringHandler Handler;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="literalLength">The total length of literals used in interpolation.</param>
    /// <param name="formattedCount">The number of interpolation holes to fill.</param>
    /// <param name="monitor">The monitor instance.</param>
    /// <param name="isValid">Whether the handler can receive and output data.</param>
    public VerboseLogStringHandler(int literalLength, int formattedCount, IMonitor monitor, out bool isValid)
    {
        isValid = monitor.IsVerbose;

        if (isValid)
            this.Handler = new(literalLength, formattedCount);
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendLiteral(string)"/>
    public void AppendLiteral(string literal)
    {
        this.Handler.AppendLiteral(literal);
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public void AppendFormatted<T>(T value)
    {
        this.Handler.AppendFormatted(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Handler.ToStringAndClear();
    }
}
