using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.BellsAndWhistles;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="SpriteText"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SpriteTextFacade : SpriteText, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static void drawString(SpriteBatch b, string s, int x, int y, int characterPosition = maxCharacter, int width = -1, int height = maxHeight, float alpha = 1f, float layerDepth = .88f, bool junimoText = false, int drawBGScroll = -1, string placeHolderScrollWidthText = "", int color = -1, ScrollTextAlignment scroll_text_alignment = ScrollTextAlignment.Left)
    {
        SpriteText.drawString(
            b: b,
            s: s,
            x: x,
            y: y,
            characterPosition: characterPosition,
            width: width,
            height: height,
            alpha: alpha,
            layerDepth: layerDepth,
            junimoText: junimoText,
            drawBGScroll: drawBGScroll,
            placeHolderScrollWidthText: placeHolderScrollWidthText,
            color: color != -1 ? SpriteText.getColorFromIndex(color) : null,
            scroll_text_alignment: scroll_text_alignment
        );
    }

    public static void drawStringWithScrollBackground(SpriteBatch b, string s, int x, int y, string placeHolderWidthText = "", float alpha = 1f, int color = -1, ScrollTextAlignment scroll_text_alignment = ScrollTextAlignment.Left)
    {
        SpriteText.drawStringWithScrollBackground(
            b: b,
            s: s,
            x: x,
            y: y,
            placeHolderWidthText: placeHolderWidthText,
            alpha: alpha,
            color: color != -1 ? SpriteText.getColorFromIndex(color) : null,
            scroll_text_alignment: scroll_text_alignment
        );
    }

    public static void drawStringWithScrollCenteredAt(SpriteBatch b, string s, int x, int y, int width, float alpha = 1f, int color = -1, int scrollType = SpriteText.scrollStyle_scroll, float layerDepth = .88f, bool junimoText = false)
    {
        SpriteText.drawStringWithScrollCenteredAt(
            b: b,
            s: s,
            x: x,
            y: y,
            width: width,
            alpha: alpha,
            color: color != -1 ? SpriteText.getColorFromIndex(color) : null,
            scrollType: scrollType,
            layerDepth: layerDepth,
            junimoText: junimoText
        );
    }

    public static void drawStringWithScrollCenteredAt(SpriteBatch b, string s, int x, int y, string placeHolderWidthText = "", float alpha = 1f, int color = -1, int scrollType = scrollStyle_scroll, float layerDepth = .88f, bool junimoText = false)
    {
        SpriteText.drawStringWithScrollCenteredAt(
            b: b,
            s: s,
            x: x,
            y: y,
            placeHolderWidthText: placeHolderWidthText,
            alpha: alpha,
            color: color != -1 ? SpriteText.getColorFromIndex(color) : null,
            scrollType: scrollType,
            layerDepth: layerDepth,
            junimoText: junimoText
        );
    }

    public static void drawStringHorizontallyCenteredAt(SpriteBatch b, string s, int x, int y, int characterPosition = maxCharacter, int width = -1, int height = maxHeight, float alpha = 1f, float layerDepth = .88f, bool junimoText = false, int color = -1, int maxWidth = 99999)
    {
        SpriteText.drawStringHorizontallyCenteredAt(
            b: b,
            s: s,
            x: x,
            y: y,
            characterPosition: characterPosition,
            width: width,
            height: height,
            alpha: alpha,
            layerDepth: layerDepth,
            junimoText: junimoText,
            color: color != -1 ? SpriteText.getColorFromIndex(color) : null,
            maxWidth: maxWidth
        );
    }


    /*********
    ** Private methods
    *********/
    private SpriteTextFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
