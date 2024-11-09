using System;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Web.Framework.Storage;

/// <summary>The response for a get-file request.</summary>
internal class StoredFileInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the file was successfully fetched.</summary>
    [MemberNotNullWhen(true, nameof(StoredFileInfo.Content))]
    public bool Success => this.Content != null && this.Error == null;

    /// <summary>The fetched file content (if <see cref="Success"/> is <c>true</c>).</summary>
    public string? Content { get; }

    /// <summary>When the file would no longer have been available, before any renewal applied in this request.</summary>
    public DateTimeOffset? OldExpiry { get; }

    /// <summary>When the file will no longer be available, after any renewal applied in this request.</summary>
    public DateTimeOffset? NewExpiry { get; }

    /// <summary>The error message if saving succeeded, but a non-blocking issue was encountered.</summary>
    public string? Warning { get; }

    /// <summary>The error message if saving failed.</summary>
    public string? Error { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="content">The fetched file content (if <see cref="Success"/> is <c>true</c>).</param>
    /// <param name="oldExpiry">When the file would no longer have been available, before any renewal applied in this request.</param>
    /// <param name="newExpiry">When the file will no longer be available, after any renewal applied in this request.</param>
    /// <param name="warning">The error message if saving succeeded, but a non-blocking issue was encountered.</param>
    /// <param name="error">The error message if saving failed.</param>
    public StoredFileInfo(string? content, DateTimeOffset? oldExpiry, DateTimeOffset? newExpiry, string? warning = null, string? error = null)
    {
        this.Content = content;
        this.OldExpiry = oldExpiry;
        this.NewExpiry = newExpiry;
        this.Warning = warning;
        this.Error = error;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="error">The error message if saving failed.</param>
    public StoredFileInfo(string error)
    {
        this.Error = error;
    }
}
