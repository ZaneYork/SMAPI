using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
#if SMAPI_FOR_MOBILE
using System.Runtime.CompilerServices;
using Android.App;
#endif
#if SMAPI_FOR_WINDOWS
#endif
using StardewModdingAPI.Framework;

namespace StardewModdingAPI
{
    /// <summary>The main entry point for SMAPI, responsible for hooking into and launching the game.</summary>
    internal class Program
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path to search for SMAPI's internal DLLs.</summary>
        internal static readonly string DllSearchPath = EarlyConstants.InternalFilesPath;


        /*********
        ** Public methods
        *********/
        /// <summary>The main entry point which hooks into and launches the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += Program.CurrentDomain_AssemblyResolve;
#if SMAPI_FOR_MOBILE
                Program.AssertAndroidGameVersion();
#else
                Program.AssertGamePresent();
                Program.AssertGameVersion();
                Program.Start(args);
#endif
            }
            catch (BadImageFormatException ex) when (ex.FileName == "StardewValley" || ex.FileName == "Stardew Valley") // don't use EarlyConstants.GameAssemblyName, since we want to check both possible names
            {
#if SMAPI_FOR_MOBILE
                SAlertDialogUtil.AlertMessage(
                    $"SMAPI failed to initialize because your game's {ex.FileName}.exe seems to be invalid.\nThis may be a pirated version which modified the executable in an incompatible way; if so, you can try a different download or buy a legitimate version.\n\nTechnical details:\n{ex}",
                    callback: type => { SMainActivity.Instance.Finish(); });
#else
                Console.WriteLine($"SMAPI failed to initialize because your game's {ex.FileName}.exe seems to be invalid.\nThis may be a pirated version which modified the executable in an incompatible way; if so, you can try a different download or buy a legitimate version.\n\nTechnical details:\n{ex}");
#endif
            }
            catch (Exception ex)
            {
#if SMAPI_FOR_MOBILE
                SAlertDialogUtil.AlertMessage($"SMAPI failed to initialize: {ex}",
                    callback: type => { SMainActivity.Instance.Finish(); });
#else
                Console.WriteLine($"SMAPI failed to initialize: {ex}");
                Program.PressAnyKeyToExit(true);
#endif
            }
        }

        /*********
        ** Private methods
        *********/
#if SMAPI_FOR_MOBILE
        private static void AssertAndroidGameVersion()
        {
            if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
            {
                SAlertDialogUtil.AlertMessage($"Oops! You're running Stardew Valley {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. Please update your game before using SMAPI.",
                    callback: type => SMainActivity.Instance.Finish());
            }
        }
#endif
        /// <summary>Method called when assembly resolution fails, which may return a manually resolved assembly.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                AssemblyName name = new AssemblyName(e.Name);
                foreach (FileInfo dll in new DirectoryInfo(Program.DllSearchPath).EnumerateFiles("*.dll"))
                {
                    if (name.Name.Equals(AssemblyName.GetAssemblyName(dll.FullName).Name, StringComparison.OrdinalIgnoreCase))
                        return Assembly.LoadFrom(dll.FullName);
                }

                return null;
            }
            catch (Exception ex)
            {
#if !SMAPI_FOR_MOBILE
                Console.WriteLine($"Error resolving assembly: {ex}");
#endif
                return null;
            }
        }

        /// <summary>Assert that the game is available.</summary>
        /// <remarks>This must be checked *before* any references to <see cref="Constants"/>, and this method should not reference <see cref="Constants"/> itself to avoid errors in Mono or when the game isn't present.</remarks>
        private static void AssertGamePresent()
        {
            if (Type.GetType($"StardewValley.Game1, {EarlyConstants.GameAssemblyName}", throwOnError: false) == null)
                Program.PrintErrorAndExit("Oops! SMAPI can't find the game. Make sure you're running StardewModdingAPI.exe in your game folder. See the readme.txt file for details.");
        }

        /// <summary>Assert that the game version is within <see cref="Constants.MinimumGameVersion"/> and <see cref="Constants.MaximumGameVersion"/>.</summary>
        private static void AssertGameVersion()
        {
            // min version
            if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
            {
                ISemanticVersion suggestedApiVersion = Constants.GetCompatibleApiVersion(Constants.GameVersion);
                Program.PrintErrorAndExit(suggestedApiVersion != null
                    ? $"Oops! You're running Stardew Valley {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. You can install SMAPI {suggestedApiVersion} instead to fix this error, or update your game to the latest version."
                    : $"Oops! You're running Stardew Valley {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. Please update your game before using SMAPI."
                );
            }

            // max version
            else if (Constants.MaximumGameVersion != null && Constants.GameVersion.IsNewerThan(Constants.MaximumGameVersion))
                Program.PrintErrorAndExit($"Oops! You're running Stardew Valley {Constants.GameVersion}, but this version of SMAPI is only compatible up to Stardew Valley {Constants.MaximumGameVersion}. Please check for a newer version of SMAPI: https://smapi.io.");
        }

        /// <summary>Initialize SMAPI and launch the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        /// <remarks>This method is separate from <see cref="Main"/> because that can't contain any references to assemblies loaded by <see cref="CurrentDomain_AssemblyResolve"/> (e.g. via <see cref="Constants"/>), or Mono will incorrectly show an assembly resolution error before assembly resolution is set up.</remarks>
        private static void Start(string[] args)
        {
            // get flags
            bool writeToConsole = !args.Contains("--no-terminal") && Environment.GetEnvironmentVariable("SMAPI_NO_TERMINAL") == null;

            // get mods path
            string modsPath;
            {
                string rawModsPath = null;

                // get from command line args
                int pathIndex = Array.LastIndexOf(args, "--mods-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                    rawModsPath = args[pathIndex];

                // get from environment variables
                if (string.IsNullOrWhiteSpace(rawModsPath))
                    rawModsPath = Environment.GetEnvironmentVariable("SMAPI_MODS_PATH");

                // normalise
                modsPath = !string.IsNullOrWhiteSpace(rawModsPath)
                    ? Path.Combine(Constants.ExecutionPath, rawModsPath)
                    : Constants.DefaultModsPath;
            }

            // load SMAPI
            using SCore core = new SCore(modsPath, writeToConsole);
            core.RunInteractively();
        }

        /// <summary>Write an error directly to the console and exit.</summary>
        /// <param name="message">The error message to display.</param>
        private static void PrintErrorAndExit(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            Program.PressAnyKeyToExit(showMessage: true);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        /// <param name="showMessage">Whether to print a 'press any key to exit' message to the console.</param>
        private static void PressAnyKeyToExit(bool showMessage)
        {
            if (showMessage)
                Console.WriteLine("Game has ended. Press any key to exit.");
            Thread.Sleep(100);
#if !SMAPI_FOR_MOBILE
            Console.ReadKey();
            Environment.Exit(0);
#endif
        }
    }
}
