using System.IO;
using System.Reflection;
using Android.Support.V4.Provider;
using HarmonyLib;
using Java.IO;
using MonoMod.Utils;
using StardewModdingAPI;
using StardewValley;

namespace System.Runtime.CompilerServices;

public class FarmMigrationPatch
{
    public static void Apply()
    {
        var harmony = new Harmony("FarmMigrationPatch");

        harmony.Patch(
            original: typeof(StardewValley.MainActivity)
                .GetMethod("DirectoryCopy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(FarmMigrationPatch), nameof(DirectoryCopyPrefix))
        );
    }

    private static void DirectoryCopy(DocumentFile source, string dest, int depth)
    {
        if (!source.Exists()) return;

        if (source.IsDirectory)
        {
            if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);

            DocumentFile[] array = source.ListFiles();
            foreach (DocumentFile documentFile in array)
            {
                if (depth == 0)
                {
                    if (documentFile.IsDirectory)
                    {
                        if (documentFile.Name.Equals("Mods") ||
                            documentFile.Name.Equals("smapi-internal") ||
                            documentFile.Name.Equals("ErrorLogs"))
                            continue;
                    }
                    else
                        continue;
                }

                string text = Path.Combine(dest, documentFile.Name);
                if (!System.IO.File.Exists(text)) FarmMigrationPatch.DirectoryCopy(documentFile, text, depth + 1);
            }

            return;
        }

        Stream stream = SMainActivity.instance.ContentResolver.OpenInputStream(source.Uri);
        FileOutputStream fileOutputStream = new FileOutputStream(dest);
        byte[] array3 = new byte[1024];
        int num;
        while ((num = stream.Read(array3)) > 0) fileOutputStream.Write(array3, 0, num);

        stream.Close();
        fileOutputStream.Close();
    }

    private static bool DirectoryCopyPrefix(DocumentFile source, string dest)
    {
        DirectoryCopy(source, dest, 0);
        return false;
    }
}
