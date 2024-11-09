using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Tools;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <c>ToolFactory</c> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ToolFactoryFacade : IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public const byte axe = 0;
    public const byte hoe = 1;
    public const byte fishingRod = 2;
    public const byte pickAxe = 3;
    public const byte wateringCan = 4;
    public const byte meleeWeapon = 5;
    public const byte slingshot = 6;


    /*********
    ** Public methods
    *********/
    public static Tool? getToolFromDescription(byte index, int upgradeLevel)
    {
        Tool? t = index switch
        {
            ToolFactoryFacade.axe => new Axe(),
            ToolFactoryFacade.hoe => new Hoe(),
            ToolFactoryFacade.fishingRod => new FishingRod(),
            ToolFactoryFacade.pickAxe => new Pickaxe(),
            ToolFactoryFacade.wateringCan => new WateringCan(),
            ToolFactoryFacade.meleeWeapon => new MeleeWeapon("0"),
            ToolFactoryFacade.slingshot => new Slingshot(),
            _ => null
        };

        if (t != null)
            t.UpgradeLevel = upgradeLevel;

        return t;
    }


    /*********
    ** Private methods
    *********/
    private ToolFactoryFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
