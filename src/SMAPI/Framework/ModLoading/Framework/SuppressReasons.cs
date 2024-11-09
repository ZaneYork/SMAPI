namespace StardewModdingAPI.Framework.ModLoading.Framework;

/// <summary>Provides common reasons for suppressing warnings in rewriter code.</summary>
internal static class SuppressReasons
{
    /// <summary>A message indicating the code matches the original game code.</summary>
    public const string MatchesOriginal = "This matches the original game code.";

    /// <summary>A message indicating the code is used via assembly rewriting.</summary>
    public const string UsedViaRewriting = "This code is used via assembly rewriting.";

    /// <summary>A message indicating the code is used via assembly rewriting.</summary>
    public const string BaseForClarity = "This code deliberately uses 'base' to ensure we're calling the real method instead of a rewritten one.";
}
