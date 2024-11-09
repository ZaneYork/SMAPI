using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Locations;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="LibraryMuseum"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class LibraryMuseumFacade : LibraryMuseum, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public bool museumAlreadyHasArtifact(int index)
    {
        return LibraryMuseum.HasDonatedArtifact(index.ToString());
    }


    /*********
    ** Private methods
    *********/
    private LibraryMuseumFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
