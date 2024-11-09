using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Framework.Logging;

/// <summary>The cache key for the <see cref="Monitor.LogOnceCache"/>.</summary>
/// <param name="Message">The log message.</param>
/// <param name="Level">The log level.</param>
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "This is only used as a lookup key.")]
internal readonly record struct LogOnceCacheKey(string Message, LogLevel Level);
