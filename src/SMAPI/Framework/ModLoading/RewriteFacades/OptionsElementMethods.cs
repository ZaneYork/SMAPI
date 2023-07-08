#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public class OptionsElementMethods
{
    private static readonly ThreadLocal<int> _recursiveCounter = new(() => 0);
    private static readonly Dictionary<Type, MethodInfo?> MethodInfos = new();

    public static void draw(OptionsElement instance, SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        if (instance.GetType().IsAssignableFrom(typeof(OptionsElement)))
            instance.draw(b, slotX, slotY);
        else
        {
            if (!MethodInfos.ContainsKey(instance.GetType())) OptionsElementMethods.MethodInfos[instance.GetType()] = AccessTools.Method(instance.GetType(), "draw", new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(IClickableMenu) });
            MethodInfo? pcDraw = MethodInfos[instance.GetType()];
            if (pcDraw != null)
            {
                if (_recursiveCounter.Value == 0)
                {
                    _recursiveCounter.Value++;
                    try
                    {
                        pcDraw.Invoke(instance, new object[] { b, slotX, slotY, context });
                    }
                    finally
                    {
                        _recursiveCounter.Value--;
                    }
                }
                else
                    instance.draw(b, slotX, slotY);
            }
            else
                instance.draw(b, slotX, slotY);
        }
    }
}
