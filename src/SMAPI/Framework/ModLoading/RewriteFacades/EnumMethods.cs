using System;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public static class EnumMethods
{
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        Type enumType = typeof(TEnum);
        return Enum.IsDefined(enumType, value);
    }
}
