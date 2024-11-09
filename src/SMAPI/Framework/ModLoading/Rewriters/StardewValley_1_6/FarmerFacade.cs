using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Farmer"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FarmerFacade : Farmer, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetObjectList<Item> items => InventoryToNetObjectList.GetCachedWrapperFor(base.Items);

    public int attack => base.buffs.Attack;
    public int immunity => base.buffs.Immunity;
    public int resilience => base.buffs.Defense;

    public float attackIncreaseModifier => base.buffs.AttackMultiplier;
    public float critChanceModifier => base.buffs.CriticalChanceMultiplier;
    public float critPowerModifier => base.buffs.CriticalPowerMultiplier;
    public float knockbackModifier => base.buffs.KnockbackMultiplier;
    public float weaponPrecisionModifier => base.buffs.WeaponPrecisionMultiplier;
    public float weaponSpeedModifier => base.buffs.WeaponSpeedMultiplier;

    public new IList<Item> Items
    {
        get => base.Items;
        set => base.Items.OverwriteWith(value);
    }

    public new int toolPower
    {
        get => base.toolPower.Value;
        set => base.toolPower.Value = value;
    }

    public new int toolHold
    {
        get => base.toolHold.Value;
        set => base.toolHold.Value = value;
    }

    public int visibleQuestCount
    {
        get
        {
            int count = 0;
            foreach (var quest in base.team.specialOrders)
            {
                if (quest?.IsHidden() is false)
                    count++;
            }

            foreach (var quest in base.questLog)
            {
                if (quest?.IsHidden() is false)
                    count++;
            }
            return count;
        }
    }


    /*********
    ** Public methods
    *********/
    public void addQuest(int questID)
    {
        base.addQuest(questID.ToString());
    }

    public bool areAllItemsNull()
    {
        return base.Items.CountItemStacks() == 0;
    }

    public bool caughtFish(int index, int size, bool from_fish_pond = false, int numberCaught = 1)
    {
        return base.caughtFish(index.ToString(), size, from_fish_pond, numberCaught);
    }

    public void changePants(Color color)
    {
        base.changePantsColor(color);
    }

    public void changePantStyle(int whichPants, bool is_customization_screen = false)
    {
        base.changePantStyle(whichPants.ToString());
    }

    public void changeShirt(int whichShirt, bool is_customization_screen = false)
    {
        base.changeShirt(whichShirt.ToString());
    }

    public void changeShoeColor(int which)
    {
        base.changeShoeColor(which.ToString());
    }

    public void completeQuest(int questID)
    {
        base.completeQuest(questID.ToString());
    }

    public void cookedRecipe(int index)
    {
        base.cookedRecipe(index.ToString());
    }

    public bool couldInventoryAcceptThisObject(int index, int stack, int quality = 0)
    {
        return base.couldInventoryAcceptThisItem(index.ToString(), stack, quality);
    }

    public void foundArtifact(int index, int number)
    {
        base.foundArtifact(index.ToString(), number);
    }

    public void foundMineral(int index)
    {
        base.foundMineral(index.ToString());
    }

    public int GetEffectsOfRingMultiplier(int ring_index)
    {
        return base.GetEffectsOfRingMultiplier(ring_index.ToString());
    }

    public int getItemCount(int item_index, int min_price = 0)
    {
        // minPrice field was always ignored

#pragma warning disable CS0618 // Type or member is obsolete: this is backwards-compatibility code.
        return base.getItemCount(item_index.ToString());
#pragma warning restore CS0618
    }

    public bool hasBuff(int whichBuff)
    {
        return base.hasBuff(whichBuff.ToString());
    }

    public bool hasGiftTasteBeenRevealed(NPC npc, int item_index)
    {
        return base.hasGiftTasteBeenRevealed(npc, item_index.ToString());
    }

    public bool hasItemBeenGifted(NPC npc, int item_index)
    {
        return base.hasItemBeenGifted(npc, item_index.ToString());
    }

    public bool hasItemInInventory(int itemIndex, int quantity, int minPrice = 0)
    {
        // minPrice field was always ignored

        switch (itemIndex)
        {
            case 858:
                return base.QiGems >= quantity;

            case 73:
                return Game1.netWorldState.Value.GoldenWalnuts >= quantity;

            default:
#pragma warning disable CS0618 // Type or member is obsolete: this is backwards-compatibility code.
                return base.getItemCount(ItemRegistry.type_object + itemIndex) >= quantity;
#pragma warning restore CS0618
        }
    }

    public bool hasItemInInventoryNamed(string? name)
    {
        if (name is not null)
        {
            foreach (Item item in base.Items)
            {
                if (item?.Name == name)
                    return true;
            }
        }

        return false;
    }

    public Item? hasItemWithNameThatContains(string name)
    {
        foreach (Item item in base.Items)
        {
            if (item?.Name is not null && item.Name.Contains(name))
                return item;
        }

        return null;
    }

    public bool hasQuest(int id)
    {
        return base.hasQuest(id.ToString());
    }

    public bool isMarried()
    {
        return base.isMarriedOrRoommates();
    }

    public bool isWearingRing(int ringIndex)
    {
        return base.isWearingRing(ringIndex.ToString());
    }

    public void removeFirstOfThisItemFromInventory(int parentSheetIndexOfItem)
    {
        base.removeFirstOfThisItemFromInventory(parentSheetIndexOfItem.ToString());
    }

    public bool removeItemsFromInventory(int index, int stack)
    {
        if (this.hasItemInInventory(index, stack))
        {
            switch (index)
            {
                case 858:
                    base.QiGems -= stack;
                    return true;

                case 73:
                    Game1.netWorldState.Value.GoldenWalnuts -= stack;
                    return true;

                default:
                    for (int i = 0; i < base.Items.Count; i++)
                    {
                        if (base.Items[i] is SObject obj && obj.parentSheetIndex.Value == index)
                        {
                            if (obj.Stack > stack)
                            {
                                obj.Stack -= stack;
                                return true;
                            }

                            stack -= obj.Stack;
                            base.Items[i] = null;
                        }

                        if (stack <= 0)
                            return true;
                    }
                    return false;
            }
        }

        return false;
    }

    public void removeQuest(int questID)
    {
        base.removeQuest(questID.ToString());
    }

    public void revealGiftTaste(NPC npc, int parent_sheet_index)
    {
        base.revealGiftTaste(npc.Name, parent_sheet_index.ToString());
    }

    public void revealGiftTaste(NPC npc, SObject item)
    {
        if (!item.bigCraftable.Value)
            base.revealGiftTaste(npc.Name, item.ItemId);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public static void showReceiveNewItemMessage(Farmer who, Item item)
    {
        Farmer.showReceiveNewItemMessage(who, item, item.Stack);
    }



    /*********
    ** Private methods
    *********/
    private FarmerFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
