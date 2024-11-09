using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="HUDMessage"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class HudMessageFacade : HUDMessage, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public string Message
    {
        get => base.message;
        set => base.message = value;
    }


    /*********
    ** Public methods
    *********/
    public static HUDMessage Constructor(string message, bool achievement)
    {
        return HUDMessage.ForAchievement(message);
    }

    public static HUDMessage Constructor(string type, int number, bool add, Color color, Item? messageSubject = null)
    {
        if (!add)
            number = -number;

        if (type == "Hay" && messageSubject is null)
            return HUDMessage.ForItemGained(ItemRegistry.Create("(O)178"), number);

        return new HUDMessage(null)
        {
            type = type,
            timeLeft = HUDMessage.defaultTime,
            number = number,
            messageSubject = messageSubject
        };
    }

    public static HUDMessage Constructor(string message, Color color, float timeLeft)
    {
        return Constructor(message, color, timeLeft, false);
    }

    public static HUDMessage Constructor(string message, string leaveMeNull)
    {
        return HUDMessage.ForCornerTextbox(message);
    }

    public static HUDMessage Constructor(string message, Color color, float timeLeft, bool fadeIn)
    {
        return new HUDMessage(message, timeLeft, fadeIn);
    }


    /*********
    ** Private methods
    *********/
    private HudMessageFacade()
        : base(null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
