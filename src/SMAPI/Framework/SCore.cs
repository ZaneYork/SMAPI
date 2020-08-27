using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
#if SMAPI_FOR_MOBILE
using System.Threading.Tasks;
using Android.Widget;
#endif
#if SMAPI_FOR_WINDOWS
using System.Windows.Forms;
#endif
using Newtonsoft.Json;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Logging;
using StardewModdingAPI.Framework.Models;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.Networking;
using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.PerformanceMonitoring;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Rendering;
using StardewModdingAPI.Framework.Serialization;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Patches;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
#if SMAPI_FOR_MOBILE
using StardewValley.Mobile;
#endif
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework
{
    /// <summary>The core class which initializes and manages SMAPI.</summary>
    internal class SCore : IDisposable
    {
        /*********
        ** Fields
        *********/
        /****
        ** Low-level components
        ****/
        /// <summary>Tracks whether the game should exit immediately and any pending initialization should be cancelled.</summary>
        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

        /// <summary>Manages the SMAPI console window and log file.</summary>
        private readonly LogManager LogManager;

        /// <summary>The core logger and monitor for SMAPI.</summary>
        private Monitor Monitor => this.LogManager.Monitor;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection = new Reflector();

        /// <summary>Encapsulates access to SMAPI core translations.</summary>
        private readonly Translator Translator = new Translator();

        /// <summary>The SMAPI configuration settings.</summary>
        private readonly SConfig Settings;

        /// <summary>The mod toolkit used for generic mod interactions.</summary>
        private readonly ModToolkit Toolkit = new ModToolkit();

        /****
        ** Higher-level components
        ****/
        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager = new CommandManager();

        /// <summary>The underlying game instance.</summary>
#if SMAPI_FOR_MOBILE
        internal SGame Game;
#else
        private SGame Game;
#endif
        /// <summary>Manages input visible to the game.</summary>
        private SInputState Input => SGame.Input;

        /// <summary>The game's core multiplayer utility.</summary>
        private SMultiplayer Multiplayer => SGame.Multiplayer;

        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore;

        /// <summary>Tracks the installed mods.</summary>
        /// <remarks>This is initialized after the game starts.</remarks>
        private readonly ModRegistry ModRegistry = new ModRegistry();

        /// <summary>Manages SMAPI events for mods.</summary>
        public readonly EventManager EventManager;

        /// <summary>Monitors the entire game state for changes.</summary>
        private WatcherCore Watchers;

        /// <summary>A snapshot of the current <see cref="Watchers"/> state.</summary>
        private readonly WatcherSnapshot WatcherSnapshot = new WatcherSnapshot();

        /****
        ** State
        ****/
        /// <summary>The path to search for mods.</summary>
        private string ModsPath => Constants.ModsPath;

        /// <summary>Whether the game is currently running.</summary>
        private bool IsGameRunning;

        /// <summary>Whether the program has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;

        /// <summary>Whether post-game-startup initialization has been performed.</summary>
        private bool IsInitialized;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        private readonly Countdown AfterLoadTimer = new Countdown(5);

        /// <summary>Whether custom content was removed from the save data to avoid a crash.</summary>
        private bool IsSaveContentRemoved;

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        private bool IsBetweenSaveEvents;

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        private bool IsBetweenCreateEvents;

        /// <summary>Asset interceptors added or removed since the last tick.</summary>
        private readonly List<AssetInterceptorChange> ReloadAssetInterceptorsQueue = new List<AssetInterceptorChange>();

        /// <summary>A list of queued commands to execute.</summary>
        /// <remarks>This property must be thread-safe, since it's accessed from a separate console input thread.</remarks>
        public ConcurrentQueue<string> CommandQueue { get; } = new ConcurrentQueue<string>();


        /*********
        ** Accessors
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        /// <remarks>This is initialized after the game starts. This is accessed directly because it's not part of the normal class model.</remarks>
        internal static DeprecationManager DeprecationManager { get; private set; }

        /// <summary>Manages performance counters.</summary>
        /// <remarks>This is initialized after the game starts. This is non-private for use by Console Commands.</remarks>
        internal static PerformanceMonitor PerformanceMonitor { get; private set; }

        /// <summary>The number of update ticks which have already executed. This is similar to <see cref="Game1.ticks"/>, but incremented more consistently for every tick.</summary>
        internal static uint TicksElapsed { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modsPath">The path to search for mods.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        public SCore(string modsPath, bool writeToConsole)
        {
            // init paths
            this.VerifyPath(modsPath);
            this.VerifyPath(Constants.LogDir);
            Constants.ModsPath = modsPath;

            // init log file
            this.PurgeNormalLogs();
            string logPath = this.GetLogPath();

            // init basics
            this.Settings = JsonConvert.DeserializeObject<SConfig>(File.ReadAllText(Constants.ApiConfigPath));
            if (File.Exists(Constants.ApiUserConfigPath))
                JsonConvert.PopulateObject(File.ReadAllText(Constants.ApiUserConfigPath), this.Settings);

            this.LogManager = new LogManager(logPath: logPath, colorConfig: this.Settings.ConsoleColors, writeToConsole: writeToConsole, isVerbose: this.Settings.VerboseLogging, isDeveloperMode: this.Settings.DeveloperMode);

            SCore.PerformanceMonitor = new PerformanceMonitor(this.Monitor);
            this.EventManager = new EventManager(this.ModRegistry, SCore.PerformanceMonitor);
            SCore.PerformanceMonitor.InitializePerformanceCounterCollections(this.EventManager);

            SCore.DeprecationManager = new DeprecationManager(this.Monitor, this.ModRegistry);

            SDate.Translations = this.Translator;

            // log SMAPI/OS info
            this.LogManager.LogIntro(modsPath, this.Settings.GetCustomSettings());

            // validate platform
            if (Constants.Platform != Platform.Windows)
            {
                this.Monitor.Log("Oops! You're running Windows, but this version of SMAPI is for Linux or Mac. Please reinstall SMAPI to fix this.", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
            }
        }

        /// <summary>Launch SMAPI.</summary>
        [HandleProcessCorruptedStateExceptions, SecurityCritical] // let try..catch handle corrupted state exceptions
        public void RunInteractively()
        {
            // initialize SMAPI
            try
            {
                JsonConverter[] converters = {
                    new ColorConverter(),
                    new PointConverter(),
                    new Vector2Converter(),
                    new RectangleConverter()
                };
                foreach (JsonConverter converter in converters)
                    this.Toolkit.JsonHelper.JsonSettings.Converters.Add(converter);

                // add error handlers
#if SMAPI_FOR_WINDOWS
                Application.ThreadException += (sender, e) => this.Monitor.Log($"Critical thread exception: {e.Exception.GetLogSummary()}", LogLevel.Error);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
#endif
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => this.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);

                // add more lenient assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => AssemblyLoader.ResolveAssembly(e.Name);

                // hook locale event
                LocalizedContentManager.OnLanguageChange += locale => this.OnLocaleChanged();

                // override game
                var multiplayer = new SMultiplayer(this.Monitor, this.EventManager, this.Toolkit.JsonHelper, this.ModRegistry, this.Reflection, this.OnModMessageReceived, this.Settings.LogNetworkTraffic);
                var modHooks = new SModHooks(this.OnNewDayAfterFade);
                SGame.CreateContentManagerImpl = this.CreateContentManager; // must be static since the game accesses it before the SGame constructor is called
                this.Game = new SGame(
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    eventManager: this.EventManager,
                    modHooks: modHooks,
                    multiplayer: multiplayer,
                    exitGameImmediately: this.ExitGameImmediately
                );
                StardewValley.Program.gamePtr = this.Game;

                // hook game events
                this.Game.OnGameContentLoaded += this.OnLoadContent;
                this.Game.OnGameUpdating += this.OnGameUpdating;
                this.Game.OnGameExiting += this.OnGameExiting;

                // apply game patches
                new GamePatcher(this.Monitor).Apply(
                    new EventErrorPatch(this.LogManager.MonitorForGame),
                    new DialogueErrorPatch(this.LogManager.MonitorForGame, this.Reflection),
                    new ObjectErrorPatch(),
#if SMAPI_FOR_MOBILE
                    new SaveBackupPatch(this.EventManager, this.Monitor),
                    new JunimoHarvesterPatch(this.Monitor),
                    new LocationSwitchPatch(this.Monitor),
                    new SpriteFontPatch(this.Monitor),
                    new ThreadSilenceExitPatch(this.Monitor),
                    new SaveGamePatch(this.Translator, this.Monitor),
                    new OnAppPausePatch(this.Monitor),
#endif
                    new LoadContextPatch(this.Reflection, this.OnLoadStageChanged),
                    new LoadErrorPatch(this.Monitor, this.OnSaveContentRemoved),
                    new ScheduleErrorPatch(this.LogManager.MonitorForGame)
                );

                // add exit handler
                new Thread(() =>
                {
                    this.CancellationToken.Token.WaitHandle.WaitOne();
                    if (this.IsGameRunning)
                    {
                        this.LogManager.WriteCrashLog();
                        this.Game.Exit();
                    }
                }).Start();

                // set window titles
#if !SMAPI_FOR_MOBILE
                this.Game.Window.Title = $"Stardew Valley {Constants.GameVersion} - running SMAPI {Constants.ApiVersion}";
                this.LogManager.SetConsoleTitle($"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GameVersion}");
#endif
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SMAPI failed to initialize: {ex.GetLogSummary()}", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
                return;
            }

            // log basic info
            this.LogManager.HandleMarkerFiles();
            this.LogManager.LogSettingsHeader(this.Settings.DeveloperMode, this.Settings.CheckForUpdates);

#if !SMAPI_FOR_MOBILE
            // set window titles
            this.Game.Window.Title = $"Stardew Valley {Constants.GameVersion} - running SMAPI {Constants.ApiVersion}";
            this.LogManager.SetConsoleTitle($"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GameVersion}");
#endif
            // start game
            this.Monitor.Log("Starting game...", LogLevel.Debug);
            try
            {
                this.IsGameRunning = true;
                StardewValley.Program.releaseBuild = true; // game's debug logic interferes with SMAPI opening the game window
                this.Game.Run();
            }
            catch (Exception ex)
            {
                this.LogManager.LogFatalLaunchError(ex);
                this.LogManager.PressAnyKeyToExit();
            }
            finally
            {
                //this.Dispose();
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            // skip if already disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            this.Monitor.Log("Disposing...");

            // dispose mod data
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
            {
                try
                {
                    (mod.Mod as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod failed during disposal: {ex.GetLogSummary()}.", LogLevel.Warn);
                }
            }

            // dispose core components
            this.IsGameRunning = false;
            this.ContentCore?.Dispose();
            this.CancellationToken?.Dispose();
            this.Game?.Dispose();
            this.LogManager?.Dispose(); // dispose last to allow for any last-second log messages

            // end game (moved from Game1.OnExiting to let us clean up first)
            Process.GetCurrentProcess().Kill();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Initialize mods before the first game asset is loaded. At this point the core content managers are loaded (so mods can load their own assets), but the game is mostly uninitialized.</summary>
        private void InitializeBeforeFirstAssetLoaded()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                this.Monitor.Log("SMAPI shutting down: aborting initialization.", LogLevel.Warn);
                return;
            }

            // init TMX support
            xTile.Format.FormatManager.Instance.RegisterMapFormat(new TMXTile.TMXFormat(Game1.tileSize / Game1.pixelZoom, Game1.tileSize / Game1.pixelZoom, Game1.pixelZoom, Game1.pixelZoom));

#if SMAPI_FOR_MOBILE
            this.Game.IsGameSuspended = true;
            new Thread(() =>
            {
                while (!this.Game.IsAfterInitialize)
                    Thread.Sleep(10);
#endif
            // load mod data
            ModToolkit toolkit = new ModToolkit();
            ModDatabase modDatabase = toolkit.GetModDatabase(Constants.ApiMetadataPath);

            // load mods
            {
                this.Monitor.Log("Loading mod metadata...");
                ModResolver resolver = new ModResolver();

                // log loose files
                {
                    string[] looseFiles = new DirectoryInfo(this.ModsPath).GetFiles().Select(p => p.Name).ToArray();
                    if (looseFiles.Any())
                        this.Monitor.Log($"  Ignored loose files: {string.Join(", ", looseFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))}");
                }

                    // load manifests
                    IModMetadata[] mods = resolver.ReadManifests(toolkit, this.ModsPath, modDatabase).ToArray();

                // filter out ignored mods
                foreach (IModMetadata mod in mods.Where(p => p.IsIgnored))
                    this.Monitor.Log($"  Skipped {mod.GetRelativePathWithRoot()} (folder name starts with a dot).");
                mods = mods.Where(p => !p.IsIgnored).ToArray();

                    // load mods
                    resolver.ValidateManifests(mods, Constants.ApiVersion, toolkit.GetUpdateUrl);
                    mods = resolver.ProcessDependencies(mods, modDatabase).ToArray();
                    this.LoadMods(mods, this.Toolkit.JsonHelper, this.ContentCore, modDatabase);

                    // check for updates
                    this.CheckForUpdatesAsync(mods);
                }

#if !SMAPI_FOR_MOBILE
            // update window titles
            int modsLoaded = this.ModRegistry.GetAll().Count();
            this.Game.Window.Title = $"Stardew Valley {Constants.GameVersion} - running SMAPI {Constants.ApiVersion} with {modsLoaded} mods";
            this.LogManager.SetConsoleTitle($"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GameVersion} with {modsLoaded} mods");
#else
                SGameConsole.Instance.isVisible = false;
                this.Game.IsGameSuspended = false;
            }).Start();
#endif
}

        /// <summary>Raised after the game finishes initializing.</summary>
        private void OnGameInitialized()
        {
            // set initial state
            this.Input.TrueUpdate();

            // init watchers
            this.Watchers = new WatcherCore(this.Input, this.Game.GetObservableLocations());

            // validate XNB integrity
            if (!this.ValidateContentIntegrity())
                this.Monitor.Log("SMAPI found problems in your game's content files which are likely to cause errors or crashes. Consider uninstalling XNB mods or reinstalling the game.", LogLevel.Error);

#if !SMAPI_FOR_MOBILE
            // start SMAPI console
            new Thread(
                () => this.LogManager.RunConsoleInputLoop(
                    commandManager: this.CommandManager,
                    reloadTranslations: this.ReloadTranslations,
                    handleInput: input => this.CommandQueue.Enqueue(input),
                    continueWhile: () => this.IsGameRunning && !this.CancellationToken.IsCancellationRequested
                )
            ).Start();
#endif
        }

        /// <summary>Raised after the game finishes loading its initial content.</summary>
        private void OnLoadContent()
        {
            // override map display device
            Game1.mapDisplayDevice = new SDisplayDevice(Game1.content, Game1.game1.GraphicsDevice);

            // log GPU info
#if SMAPI_FOR_WINDOWS
            this.Monitor.Log($"Running on GPU: {Game1.game1.GraphicsDevice?.Adapter?.Description ?? "<unknown>"}");
#endif
        }

        /// <summary>Raised when the game is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runGameUpdate">Invoke the game's update logic.</param>
        private void OnGameUpdating(GameTime gameTime, Action runGameUpdate)
        {
#if SMAPI_FOR_MOBILE
            if (this.Game.IsGameSuspended)
            {
                if (!this.Game.IsAfterInitialize)
                    this.Game.IsAfterInitialize = true;

                return;
            }
#endif
            var events = this.EventManager;

            try
            {
                SCore.DeprecationManager.PrintQueued();
                SCore.PerformanceMonitor.PrintQueuedAlerts();

                /*********
                ** First-tick initialization
                *********/
                if (!this.IsInitialized)
                {
                    this.IsInitialized = true;
                    this.OnGameInitialized();
                }

                /*********
                ** Update input
                *********/
                // This should *always* run, even when suppressing mod events, since the game uses
                // this too. For example, doing this after mod event suppression would prevent the
                // user from doing anything on the overnight shipping screen.
                SInputState inputState = this.Input;
                if (this.Game.IsActive)
                    inputState.TrueUpdate();

                /*********
                ** Special cases
                *********/
                // Abort if SMAPI is exiting.
                if (this.CancellationToken.IsCancellationRequested)
                {
                    this.Monitor.Log("SMAPI shutting down: aborting update.");
                    return;
                }

                // Run async tasks synchronously to avoid issues due to mod events triggering
                // concurrently with game code.
                bool saveParsed = false;
                if (Game1.currentLoader != null)
                {
                    this.Monitor.Log("Game loader synchronizing...");
                    while (Game1.currentLoader?.MoveNext() == true)
                    {
                        // raise load stage changed
                        switch (Game1.currentLoader.Current)
                        {
                            case 20 when (!saveParsed && SaveGame.loaded != null):
                                saveParsed = true;
                                this.OnLoadStageChanged(LoadStage.SaveParsed);
                                break;

                            case 36:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                                break;

                            case 50:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                                break;

                            default:
                                if (Game1.gameMode == Game1.playingGameMode)
                                    this.OnLoadStageChanged(LoadStage.Preloaded);
                                break;
                        }
                    }

                    Game1.currentLoader = null;
                    this.Monitor.Log("Game loader done.");
                }

                if (SGame.NewDayTask?.Status == TaskStatus.Created)
                {
                    this.Monitor.Log("New day task synchronizing...");
                    SGame.NewDayTask.RunSynchronously();
                    this.Monitor.Log("New day task done.");
                }

                // While a background task is in progress, the game may make changes to the game
                // state while mods are running their code. This is risky, because data changes can
                // conflict (e.g. collection changed during enumeration errors) and data may change
                // unexpectedly from one mod instruction to the next.
                //
                // Therefore we can just run Game1.Update here without raising any SMAPI events. There's
                // a small chance that the task will finish after we defer but before the game checks,
                // which means technically events should be raised, but the effects of missing one
                // update tick are negligible and not worth the complications of bypassing Game1.Update.
                if (SGame.NewDayTask != null || Game1.gameMode == Game1.loadingMode)
                {
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SCore.TicksElapsed++;
                    runGameUpdate();
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                // Raise minimal events while saving.
                // While the game is writing to the save file in the background, mods can unexpectedly
                // fail since they don't have exclusive access to resources (e.g. collection changed
                // during enumeration errors). To avoid problems, events are not invoked while a save
                // is in progress. It's safe to raise SaveEvents.BeforeSave as soon as the menu is
                // opened (since the save hasn't started yet), but all other events should be suppressed.
                if (Context.IsSaving)
                {
                    // raise before-create
                    if (!Context.IsWorldReady && !this.IsBetweenCreateEvents)
                    {
                        this.IsBetweenCreateEvents = true;
                        this.Monitor.Log("Context: before save creation.");
                        events.SaveCreating.RaiseEmpty();
                    }

                    // raise before-save
                    if (Context.IsWorldReady && !this.IsBetweenSaveEvents)
                    {
                        this.IsBetweenSaveEvents = true;
                        this.Monitor.Log("Context: before save.");
                        events.Saving.RaiseEmpty();
                    }

                    // suppress non-save events
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SCore.TicksElapsed++;
                    runGameUpdate();
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                /*********
                ** Reload assets when interceptors are added/removed
                *********/
                if (this.ReloadAssetInterceptorsQueue.Any())
                {
                    // get unique interceptors
                    AssetInterceptorChange[] interceptors = this.ReloadAssetInterceptorsQueue
                        .GroupBy(p => p.Instance, new ObjectReferenceComparer<object>())
                        .Select(p => p.First())
                        .ToArray();
                    this.ReloadAssetInterceptorsQueue.Clear();

                    // log summary
                    this.Monitor.Log("Invalidating cached assets for new editors & loaders...");
                    this.Monitor.Log(
                        "   changed: "
                        + string.Join(", ",
                            interceptors
                                .GroupBy(p => p.Mod)
                                .OrderBy(p => p.Key.DisplayName)
                                .Select(modGroup =>
                                    $"{modGroup.Key.DisplayName} ("
                                    + string.Join(", ", modGroup.GroupBy(p => p.WasAdded).ToDictionary(p => p.Key, p => p.Count()).Select(p => $"{(p.Key ? "added" : "removed")} {p.Value}"))
                                    + ")"
                                )
                        )
                    );

                    // reload affected assets
                    this.ContentCore.InvalidateCache(asset => interceptors.Any(p => p.CanIntercept(asset)));
                }

                /*********
                ** Execute commands
                *********/
                while (this.CommandQueue.TryDequeue(out string rawInput))
                {
                    // parse command
                    string name;
                    string[] args;
                    Command command;
                    try
                    {
                        if (!this.CommandManager.TryParse(rawInput, out name, out args, out command))
                        {
                            this.Monitor.Log("Unknown command; type 'help' for a list of available commands.", LogLevel.Error);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Failed parsing that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        continue;
                    }

                    // execute command
                    try
                    {
                        command.Callback.Invoke(name, args);
                    }
                    catch (Exception ex)
                    {
                        if (command.Mod != null)
                            command.Mod.LogAsMod($"Mod failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        else
                            this.Monitor.Log($"Failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }
                }

                /*********
                ** Update context
                *********/
                bool wasWorldReady = Context.IsWorldReady;
                if ((Context.IsWorldReady && !Context.IsSaveLoaded) || Game1.exitToTitle)
                {
                    Context.IsWorldReady = false;
                    this.AfterLoadTimer.Reset();
                }
                else if (Context.IsSaveLoaded && this.AfterLoadTimer.Current > 0 && Game1.currentLocation != null)
                {
                    if (Game1.dayOfMonth != 0) // wait until new-game intro finishes (world not fully initialized yet)
                        this.AfterLoadTimer.Decrement();
                    Context.IsWorldReady = this.AfterLoadTimer.Current == 0;
                }

                /*********
                ** Update watchers
                **   (Watchers need to be updated, checked, and reset in one go so we can detect any changes mods make in event handlers.)
                *********/
                this.Watchers.Update();
                this.WatcherSnapshot.Update(this.Watchers);
                this.Watchers.Reset();
                WatcherSnapshot state = this.WatcherSnapshot;

                /*********
                ** Display in-game warnings
                *********/
                // save content removed
                if (this.IsSaveContentRemoved && Context.IsWorldReady)
                {
                    this.IsSaveContentRemoved = false;
                    Game1.addHUDMessage(new HUDMessage(this.Translator.Get("warn.invalid-content-removed"), HUDMessage.error_type));
                }

                /*********
                ** Pre-update events
                *********/
                {
                    /*********
                    ** Save created/loaded events
                    *********/
                    if (this.IsBetweenCreateEvents)
                    {
                        // raise after-create
                        this.IsBetweenCreateEvents = false;
                        this.Monitor.Log($"Context: after save creation, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.");
                        this.OnLoadStageChanged(LoadStage.CreatedSaveFile);
                        events.SaveCreated.RaiseEmpty();
                    }

                    if (this.IsBetweenSaveEvents)
                    {
                        // raise after-save
                        this.IsBetweenSaveEvents = false;
                        this.Monitor.Log($"Context: after save, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.");
                        events.Saved.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Locale changed events
                    *********/
                    if (state.Locale.IsChanged)
                        this.Monitor.Log($"Context: locale set to {state.Locale.New}.");

                    /*********
                    ** Load / return-to-title events
                    *********/
                    if (wasWorldReady && !Context.IsWorldReady)
                        this.OnLoadStageChanged(LoadStage.None);
                    else if (Context.IsWorldReady && Context.LoadStage != LoadStage.Ready)
                    {
                        // print context
                        string context = $"Context: loaded save '{Constants.SaveFolderName}', starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}, locale set to {this.ContentCore.Language}.";
                        if (Context.IsMultiplayer)
                        {
                            int onlineCount = Game1.getOnlineFarmers().Count();
                            context += $" {(Context.IsMainPlayer ? "Main player" : "Farmhand")} with {onlineCount} {(onlineCount == 1 ? "player" : "players")} online.";
                        }
                        else
                            context += " Single-player.";

                        this.Monitor.Log(context);

                        // raise events
                        this.OnLoadStageChanged(LoadStage.Ready);
                        events.SaveLoaded.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Window events
                    *********/
                    // Here we depend on the game's viewport instead of listening to the Window.Resize
                    // event because we need to notify mods after the game handles the resize, so the
                    // game's metadata (like Game1.viewport) are updated. That's a bit complicated
                    // since the game adds & removes its own handler on the fly.
                    if (state.WindowSize.IsChanged)
                    {
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Events: window size changed to {state.WindowSize.New}.");

                        events.WindowResized.Raise(new WindowResizedEventArgs(state.WindowSize.Old, state.WindowSize.New));
                    }

                    /*********
                    ** Input events (if window has focus)
                    *********/
                    if (this.Game.IsActive)
                    {
                        // raise events
                        bool isChatInput = Game1.IsChatting || (Context.IsMultiplayer && Context.IsWorldReady && Game1.activeClickableMenu == null && Game1.currentMinigame == null && inputState.IsAnyDown(Game1.options.chatButton));
                        if (!isChatInput)
                        {
                            ICursorPosition cursor = this.Input.CursorPosition;

                            // raise cursor moved event
                            if (state.Cursor.IsChanged)
                                events.CursorMoved.Raise(new CursorMovedEventArgs(state.Cursor.Old, state.Cursor.New));

                            // raise mouse wheel scrolled
                            if (state.MouseWheelScroll.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: mouse wheel scrolled to {state.MouseWheelScroll.New}.");
                                events.MouseWheelScrolled.Raise(new MouseWheelScrolledEventArgs(cursor, state.MouseWheelScroll.Old, state.MouseWheelScroll.New));
                            }

                            // raise input button events
                            foreach (var pair in inputState.LastButtonStates)
                            {
                                SButton button = pair.Key;
                                SButtonState status = pair.Value;

                                if (status == SButtonState.Pressed)
                                {
                                    if (this.Monitor.IsVerbose)
                                        this.Monitor.Log($"Events: button {button} pressed.");

                                    events.ButtonPressed.Raise(new ButtonPressedEventArgs(button, cursor, inputState));
                                }
                                else if (status == SButtonState.Released)
                                {
                                    if (this.Monitor.IsVerbose)
                                        this.Monitor.Log($"Events: button {button} released.");

                                    events.ButtonReleased.Raise(new ButtonReleasedEventArgs(button, cursor, inputState));
                                }
                            }
                        }
                    }

                    /*********
                    ** Menu events
                    *********/
                    if (state.ActiveMenu.IsChanged)
                    {
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Context: menu changed from {state.ActiveMenu.Old?.GetType().FullName ?? "none"} to {state.ActiveMenu.New?.GetType().FullName ?? "none"}.");
                        IClickableMenu was = state.ActiveMenu.Old;
                        IClickableMenu now = state.ActiveMenu.New;
#if SMAPI_FOR_MOBILE
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Context: menu changed from {state.ActiveMenu.Old?.GetType().FullName ?? "none"} to {state.ActiveMenu.New?.GetType().FullName ?? "none"}.", LogLevel.Trace);

                        // raise menu events
                        events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));

                        if (now is GameMenu gameMenu)
                        {
                            foreach (IClickableMenu menu in gameMenu.pages)
                            {
                                OptionsPage optionsPage = menu as OptionsPage;
                                if (optionsPage != null)
                                {
                                    List<OptionsElement> options = this.Reflection.GetField<List<OptionsElement>>(optionsPage, "options").GetValue();
                                    options.Insert(0, new OptionsButton("Console", () => SGameConsole.Instance.Show()));
                                    this.Reflection.GetMethod(optionsPage, "updateContentPositions").Invoke();
                                }
                            }
                        }
                        else if (now is ShopMenu shopMenu)
                        {
                            Dictionary<ISalable, int[]> itemPriceAndStock = this.Reflection.GetField<Dictionary<ISalable, int[]>>(shopMenu, "itemPriceAndStock").GetValue();
                            if (shopMenu.forSaleButtons.Count < itemPriceAndStock.Keys.Select(item => item.Name).Distinct().Count())
                            {
                                this.Monitor.Log("Shop Menu Pop");
                                Game1.activeClickableMenu = new ShopMenu(itemPriceAndStock,
                                    this.Reflection.GetField<int>(shopMenu, "currency").GetValue(),
                                    this.Reflection.GetField<string>(shopMenu, "personName").GetValue(),
                                    shopMenu.onPurchase, shopMenu.onSell, shopMenu.storeContext);
                            }
                        }
#else
                        // raise menu events
                        events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));
#endif
                    }

                    /*********
                    ** World & player events
                    *********/
                    if (Context.IsWorldReady)
                    {
                        bool raiseWorldEvents = !state.SaveID.IsChanged; // don't report changes from unloaded => loaded

                        // location list changes
                        if (state.Locations.LocationList.IsChanged && (events.LocationListChanged.HasListeners() || this.Monitor.IsVerbose))
                        {
                            var added = state.Locations.LocationList.Added.ToArray();
                            var removed = state.Locations.LocationList.Removed.ToArray();

                            if (this.Monitor.IsVerbose)
                            {
                                string addedText = added.Any() ? string.Join(", ", added.Select(p => p.Name)) : "none";
                                string removedText = removed.Any() ? string.Join(", ", removed.Select(p => p.Name)) : "none";
                                this.Monitor.Log($"Context: location list changed (added {addedText}; removed {removedText}).");
                            }

                            events.LocationListChanged.Raise(new LocationListChangedEventArgs(added, removed));
                        }

                        // raise location contents changed
                        if (raiseWorldEvents)
                        {
                            foreach (LocationSnapshot locState in state.Locations.Locations)
                            {
                                var location = locState.Location;

                                // buildings changed
                                if (locState.Buildings.IsChanged)
                                    events.BuildingListChanged.Raise(new BuildingListChangedEventArgs(location, locState.Buildings.Added, locState.Buildings.Removed));

                                // debris changed
                                if (locState.Debris.IsChanged)
                                    events.DebrisListChanged.Raise(new DebrisListChangedEventArgs(location, locState.Debris.Added, locState.Debris.Removed));

                                // large terrain features changed
                                if (locState.LargeTerrainFeatures.IsChanged)
                                    events.LargeTerrainFeatureListChanged.Raise(new LargeTerrainFeatureListChangedEventArgs(location, locState.LargeTerrainFeatures.Added, locState.LargeTerrainFeatures.Removed));

                                // NPCs changed
                                if (locState.Npcs.IsChanged)
                                    events.NpcListChanged.Raise(new NpcListChangedEventArgs(location, locState.Npcs.Added, locState.Npcs.Removed));

                                // objects changed
                                if (locState.Objects.IsChanged)
                                    events.ObjectListChanged.Raise(new ObjectListChangedEventArgs(location, locState.Objects.Added, locState.Objects.Removed));

                                // chest items changed
                                if (events.ChestInventoryChanged.HasListeners())
                                {
                                    foreach (var pair in locState.ChestItems)
                                    {
                                        SnapshotItemListDiff diff = pair.Value;
                                        events.ChestInventoryChanged.Raise(new ChestInventoryChangedEventArgs(pair.Key, location, added: diff.Added, removed: diff.Removed, quantityChanged: diff.QuantityChanged));
                                    }
                                }

                                // terrain features changed
                                if (locState.TerrainFeatures.IsChanged)
                                    events.TerrainFeatureListChanged.Raise(new TerrainFeatureListChangedEventArgs(location, locState.TerrainFeatures.Added, locState.TerrainFeatures.Removed));
                            }
                        }

                        // raise time changed
                        if (raiseWorldEvents && state.Time.IsChanged)
                            events.TimeChanged.Raise(new TimeChangedEventArgs(state.Time.Old, state.Time.New));

                        // raise player events
                        if (raiseWorldEvents)
                        {
                            PlayerSnapshot playerState = state.CurrentPlayer;
                            Farmer player = playerState.Player;

                            // raise current location changed
                            if (playerState.Location.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Context: set location to {playerState.Location.New}.");

#if SMAPI_FOR_MOBILE
                                // Fix tapToMove null pointer error
                                if (playerState.Location.New.tapToMove == null && playerState.Location.New.map != null)
                                {
                                    playerState.Location.New.tapToMove = new TapToMove(playerState.Location.New);
                                }
#endif
                                events.Warped.Raise(new WarpedEventArgs(player, playerState.Location.Old, playerState.Location.New));
                            }

                            // raise player leveled up a skill
                            foreach (var pair in playerState.Skills)
                            {
                                if (!pair.Value.IsChanged)
                                    continue;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: player skill '{pair.Key}' changed from {pair.Value.Old} to {pair.Value.New}.");

                                events.LevelChanged.Raise(new LevelChangedEventArgs(player, pair.Key, pair.Value.Old, pair.Value.New));
                            }

                            // raise player inventory changed
                            if (playerState.Inventory.IsChanged)
                            {
                                var inventory = playerState.Inventory;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log("Events: player inventory changed.");
                                events.InventoryChanged.Raise(new InventoryChangedEventArgs(player, added: inventory.Added, removed: inventory.Removed, quantityChanged: inventory.QuantityChanged));
                            }
                        }
                    }

                    /*********
                    ** Game update
                    *********/
                    // game launched
                    bool isFirstTick = SCore.TicksElapsed == 0;
                    if (isFirstTick)
                    {
                        Context.IsGameLaunched = true;
                        events.GameLaunched.Raise(new GameLaunchedEventArgs());
                    }

                    // preloaded
                    if (Context.IsSaveLoaded && Context.LoadStage != LoadStage.Loaded && Context.LoadStage != LoadStage.Ready && Game1.dayOfMonth != 0)
                        this.OnLoadStageChanged(LoadStage.Loaded);
                }

                /*********
                ** Game update tick
                *********/
                {
                    bool isOneSecond = SCore.TicksElapsed % 60 == 0;
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    events.UpdateTicking.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicking.RaiseEmpty();
                    try
                    {
                        this.Input.ApplyOverrides(); // if mods added any new overrides since the update, process them now
                        SCore.TicksElapsed++;
                        runGameUpdate();
                    }
                    catch (Exception ex)
                    {
                        this.LogManager.MonitorForGame.Log($"An error occured in the base update loop: {ex.GetLogSummary()}", LogLevel.Error);
                    }

                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    events.UpdateTicked.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicked.RaiseEmpty();
                }

                /*********
                ** Update events
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
        }

        /// <summary>Handle the game changing locale.</summary>
        private void OnLocaleChanged()
        {
            this.ContentCore.OnLocaleChanged();

            // get locale
            string locale = this.ContentCore.GetLocale();
            LocalizedContentManager.LanguageCode languageCode = this.ContentCore.Language;

            // update core translations
            this.Translator.SetLocale(locale, languageCode);

            // update mod translation helpers
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
                mod.Translations.SetLocale(locale, languageCode);
        }

        /// <summary>Raised when the low-level stage while loading a save changes.</summary>
        /// <param name="newStage">The new load stage.</param>
        internal void OnLoadStageChanged(LoadStage newStage)
        {
            // nothing to do
            if (newStage == Context.LoadStage)
                return;

            // update data
            LoadStage oldStage = Context.LoadStage;
            Context.LoadStage = newStage;
            this.Monitor.VerboseLog($"Context: load stage changed to {newStage}");
            if (newStage == LoadStage.None)
            {
                this.Monitor.Log("Context: returned to title");
                this.OnReturnedToTitle();
            }

            // raise events
            this.EventManager.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                this.EventManager.ReturnedToTitle.RaiseEmpty();
        }

        /// <summary>Raised after custom content is removed from the save data to avoid a crash.</summary>
        internal void OnSaveContentRemoved()
        {
            this.IsSaveContentRemoved = true;
        }

        /// <summary>A callback invoked before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        protected void OnNewDayAfterFade()
        {
            this.EventManager.DayEnding.RaiseEmpty();
        }

        /// <summary>Raised after the player returns to the title screen.</summary>
        private void OnReturnedToTitle()
        {
            // perform cleanup
            this.Multiplayer.CleanupOnMultiplayerExit();
            if (!(Game1.mapDisplayDevice is SDisplayDevice))
                Game1.mapDisplayDevice = new SDisplayDevice(Game1.content, Game1.game1.GraphicsDevice);
        }

        /// <summary>Raised before the game exits.</summary>
        private void OnGameExiting()
        {
            this.Multiplayer.Disconnect(StardewValley.Multiplayer.DisconnectType.ClosedGame);
            this.Dispose();
        }

        /// <summary>Raised when a mod network message is received.</summary>
        /// <param name="message">The message to deliver to applicable mods.</param>
        private void OnModMessageReceived(ModMessageModel message)
        {
            // get mod IDs to notify
            HashSet<string> modIDs = new HashSet<string>(message.ToModIDs ?? this.ModRegistry.GetAll().Select(p => p.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            if (message.FromPlayerID == Game1.player?.UniqueMultiplayerID)
                modIDs.Remove(message.FromModID); // don't send a broadcast back to the sender

            // raise events
            this.EventManager.ModMessageReceived.Raise(new ModMessageReceivedEventArgs(message), mod => mod != null && modIDs.Contains(mod.Manifest.UniqueID));
        }

        /// <summary>Constructor a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        private LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initializing from SGame constructor
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, this.Monitor, this.Reflection, this.Toolkit.JsonHelper, this.InitializeBeforeFirstAssetLoaded);
                if (this.ContentCore.Language != this.Translator.LocaleEnum)
                    this.Translator.SetLocale(this.ContentCore.GetLocale(), this.ContentCore.Language);

                this.NextContentManagerIsMain = true;
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initializing from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
#if SMAPI_FOR_MOBILE
                SGameConsole.Instance.InitializeContent(this.ContentCore.MainContentManager);
#endif
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }

        /// <summary>Look for common issues with the game's XNB content, and log warnings if anything looks broken or outdated.</summary>
        /// <returns>Returns whether all integrity checks passed.</returns>
        private bool ValidateContentIntegrity()
        {
            this.Monitor.Log("Detecting common issues...");
            bool issuesFound = false;

            // object format (commonly broken by outdated files)
            {
                // detect issues
                bool hasObjectIssues = false;
                void LogIssue(int id, string issue) => this.Monitor.Log($@"Detected issue: item #{id} in Content\Data\ObjectInformation.xnb is invalid ({issue}).");
                foreach (KeyValuePair<int, string> entry in Game1.objectInformation)
                {
                    // must not be empty
                    if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        LogIssue(entry.Key, "entry is empty");
                        hasObjectIssues = true;
                        continue;
                    }

                    // require core fields
                    string[] fields = entry.Value.Split('/');
                    if (fields.Length < SObject.objectInfoDescriptionIndex + 1)
                    {
                        LogIssue(entry.Key, "too few fields for an object");
                        hasObjectIssues = true;
                        continue;
                    }

                    // check min length for specific types
                    switch (fields[SObject.objectInfoTypeIndex].Split(new[] { ' ' }, 2)[0])
                    {
                        case "Cooking":
                            if (fields.Length < SObject.objectInfoBuffDurationIndex + 1)
                            {
                                LogIssue(entry.Key, "too few fields for a cooking item");
                                hasObjectIssues = true;
                            }
                            break;
                    }
                }

                // log error
                if (hasObjectIssues)
                {
                    issuesFound = true;
                    this.Monitor.Log(@"Your Content\Data\ObjectInformation.xnb file seems to be broken or outdated.", LogLevel.Warn);
                }
            }

            return !issuesFound;
        }

        /// <summary>Asynchronously check for a new version of SMAPI and any installed mods, and print alerts to the console if an update is available.</summary>
        /// <param name="mods">The mods to include in the update check (if eligible).</param>
        private void CheckForUpdatesAsync(IModMetadata[] mods)
        {
            if (!this.Settings.CheckForUpdates)
                return;

            new Thread(() =>
            {
                // create client
                string url = this.Settings.WebApiBaseUrl;
#if !SMAPI_FOR_WINDOWS
                url = url.Replace("https://", "http://"); // workaround for OpenSSL issues with the game's bundled Mono on Linux/Mac
#endif
                WebApiClient client = new WebApiClient(url, Constants.ApiVersion);
                this.Monitor.Log("Checking for updates...");

                // check SMAPI version
#if DO_ANDROID_SMAPI_UPDATE_CHECK
                ISemanticVersion updateFound = null;
                try
                {
                    // fetch update check
                    ModEntryModel response = client.GetModInfo(new[] { new ModSearchEntryModel("MartyrPher.SMAPI-Android-Installer", Constants.ApiVersion, new[] { $"GitHub:{this.Settings.GitHubProjectName}" }) }, apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform).Single().Value;
                    if (response.SuggestedUpdate != null)
                        this.Monitor.Log($"You can update SMAPI to {response.SuggestedUpdate.Version}: {Constants.HomePageUrl}", LogLevel.Alert);
                    else
                        this.Monitor.Log("   SMAPI okay.");

                    updateFound = response.SuggestedUpdate?.Version;

                    // show errors
                    if (response.Errors.Any())
                    {
                        this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log($"Error: {string.Join("\n", response.Errors)}");
                    }
                }
                catch (Exception ex)
                {
                    this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you won't be notified of new versions if this keeps happening.", LogLevel.Warn);
                    this.Monitor.Log(ex is WebException && ex.InnerException == null
                        ? $"Error: {ex.Message}"
                        : $"Error: {ex.GetLogSummary()}"
                    );
                }

                // show update message on next launch
                if (updateFound != null)
                    this.LogManager.WriteUpdateMarker(updateFound.ToString());
#endif
                // check mod versions
                if (mods.Any())
                {
                    try
                    {
                        HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);

                        // prepare search model
                        List<ModSearchEntryModel> searchMods = new List<ModSearchEntryModel>();
                        foreach (IModMetadata mod in mods)
                        {
                            if (!mod.HasID() || suppressUpdateChecks.Contains(mod.Manifest.UniqueID))
                                continue;

                            string[] updateKeys = mod
                                .GetUpdateKeys(validOnly: true)
                                .Select(p => p.ToString())
                                .ToArray();
                            searchMods.Add(new ModSearchEntryModel(mod.Manifest.UniqueID, mod.Manifest.Version, updateKeys.ToArray(), isBroken: mod.Status == ModMetadataStatus.Failed));
                        }

                        // fetch results
                        this.Monitor.Log($"   Checking for updates to {searchMods.Count} mods...");
                        IDictionary<string, ModEntryModel> results = client.GetModInfo(searchMods.ToArray(), apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform);

                        // extract update alerts & errors
                        var updates = new List<Tuple<IModMetadata, ISemanticVersion, string>>();
                        var errors = new StringBuilder();
                        foreach (IModMetadata mod in mods.OrderBy(p => p.DisplayName))
                        {
                            // link to update-check data
                            if (!mod.HasID() || !results.TryGetValue(mod.Manifest.UniqueID, out ModEntryModel result))
                                continue;
                            mod.SetUpdateData(result);

                            // handle errors
                            if (result.Errors != null && result.Errors.Any())
                            {
                                errors.AppendLine(result.Errors.Length == 1
                                    ? $"   {mod.DisplayName}: {result.Errors[0]}"
                                    : $"   {mod.DisplayName}:\n      - {string.Join("\n      - ", result.Errors)}"
                                );
                            }

                            // handle update
                            if (result.SuggestedUpdate != null)
                                updates.Add(Tuple.Create(mod, result.SuggestedUpdate.Version, result.SuggestedUpdate.Url));
                        }

                        // show update errors
                        if (errors.Length != 0)
                            this.Monitor.Log("Got update-check errors for some mods:\n" + errors.ToString().TrimEnd());

                        // show update alerts
                        if (updates.Any())
                        {
                            this.Monitor.Newline();
                            this.Monitor.Log($"You can update {updates.Count} mod{(updates.Count != 1 ? "s" : "")}:", LogLevel.Alert);
                            foreach (var entry in updates)
                            {
                                IModMetadata mod = entry.Item1;
                                ISemanticVersion newVersion = entry.Item2;
                                string newUrl = entry.Item3;
                                this.Monitor.Log($"   {mod.DisplayName} {newVersion}: {newUrl}", LogLevel.Alert);
                            }
                        }
                        else
                            this.Monitor.Log("   All mods up to date.");
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for new mod versions. This won't affect your game, but you won't be notified of mod updates if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? ex.Message
                            : ex.ToString()
                        );
                    }
                }
            }).Start();
        }

        /// <summary>Create a directory path if it doesn't exist.</summary>
        /// <param name="path">The directory path.</param>
        private void VerifyPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                // note: this happens before this.Monitor is initialized
                Console.WriteLine($"Couldn't create a path: {path}\n\n{ex.GetLogSummary()}");
            }
        }

        /// <summary>Load and hook up the given mods.</summary>
        /// <param name="mods">The mods to load.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        private void LoadMods(IModMetadata[] mods, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase)
        {
            this.Monitor.Log("Loading mods...");

            // load mods
            IList<IModMetadata> skippedMods = new List<IModMetadata>();
            using (AssemblyLoader modAssemblyLoader = new AssemblyLoader(Constants.Platform, this.Monitor, this.Settings.ParanoidWarnings))
            {
                // init
                HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);
                InterfaceProxyFactory proxyFactory = new InterfaceProxyFactory();

                // load mods
                foreach (IModMetadata mod in mods)
                {
                    if (!this.TryLoadMod(mod, mods, modAssemblyLoader, proxyFactory, jsonHelper, contentCore, modDatabase, suppressUpdateChecks, out string errorPhrase, out string errorDetails))
                    {
                        mod.SetStatus(ModMetadataStatus.Failed, errorPhrase, errorDetails);
                        skippedMods.Add(mod);
                    }
                }
            }

            IModMetadata[] loaded = this.ModRegistry.GetAll().ToArray();
            IModMetadata[] loadedContentPacks = loaded.Where(p => p.IsContentPack).ToArray();
            IModMetadata[] loadedMods = loaded.Where(p => !p.IsContentPack).ToArray();

            // unlock content packs
            this.ModRegistry.AreAllModsLoaded = true;

            // log mod info
            this.LogManager.LogModInfo(loaded, loadedContentPacks, loadedMods, skippedMods.ToArray(), this.Settings.ParanoidWarnings);

            // initialize translations
            this.ReloadTranslations(loaded);

            // initialize loaded non-content-pack mods
            foreach (IModMetadata metadata in loadedMods)
            {
                // add interceptors
                if (metadata.Mod.Helper.Content is ContentHelper helper)
                {
                    // ReSharper disable SuspiciousTypeConversion.Global
                    if (metadata.Mod is IAssetEditor editor)
                        this.ContentCore.Editors.Add(new ModLinked<IAssetEditor>(metadata, editor));
                    if (metadata.Mod is IAssetLoader loader)
                        this.ContentCore.Loaders.Add(new ModLinked<IAssetLoader>(metadata, loader));
                    // ReSharper restore SuspiciousTypeConversion.Global

                    helper.ObservableAssetEditors.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetEditor>(), e.OldItems?.Cast<IAssetEditor>(), this.ContentCore.Editors);
                    helper.ObservableAssetLoaders.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetLoader>(), e.OldItems?.Cast<IAssetLoader>(), this.ContentCore.Loaders);
                }

                // call entry method
                try
                {
                    IMod mod = metadata.Mod;
                    mod.Entry(mod.Helper);
                }
                catch (Exception ex)
                {
                    metadata.LogAsMod($"Mod crashed on entry and might not work correctly. Technical details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // get mod API
                try
                {
                    object api = metadata.Mod.GetApi();
                    if (api != null && !api.GetType().IsPublic)
                    {
                        api = null;
                        this.Monitor.Log($"{metadata.DisplayName} provides an API instance with a non-public type. This isn't currently supported, so the API won't be available to other mods.", LogLevel.Warn);
                    }

                    if (api != null)
                        this.Monitor.Log($"   Found mod-provided API ({api.GetType().FullName}).");
                    metadata.SetApi(api);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed loading mod-provided API for {metadata.DisplayName}. Integrations with other mods may not work. Error: {ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            // unlock mod integrations
            this.ModRegistry.AreAllModsInitialized = true;
        }

        /// <summary>Raised after a mod adds or removes asset interceptors.</summary>
        /// <typeparam name="T">The asset interceptor type (one of <see cref="IAssetEditor"/> or <see cref="IAssetLoader"/>).</typeparam>
        /// <param name="mod">The mod metadata.</param>
        /// <param name="added">The interceptors that were added.</param>
        /// <param name="removed">The interceptors that were removed.</param>
        /// <param name="list">A list of interceptors to update for the change.</param>
        private void OnAssetInterceptorsChanged<T>(IModMetadata mod, IEnumerable<T> added, IEnumerable<T> removed, IList<ModLinked<T>> list)
        {
            foreach (T interceptor in added ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: true));
                list.Add(new ModLinked<T>(mod, interceptor));
            }

            foreach (T interceptor in removed ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: false));
                foreach (ModLinked<T> entry in list.Where(p => p.Mod == mod && object.ReferenceEquals(p.Data, interceptor)).ToArray())
                    list.Remove(entry);
            }
        }

        /// <summary>Load a given mod.</summary>
        /// <param name="mod">The mod to load.</param>
        /// <param name="mods">The mods being loaded.</param>
        /// <param name="assemblyLoader">Preprocesses and loads mod assemblies</param>
        /// <param name="proxyFactory">Generates proxy classes to access mod APIs through an arbitrary interface.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        /// <param name="suppressUpdateChecks">The mod IDs to ignore when validating update keys.</param>
        /// <param name="errorReasonPhrase">The user-facing reason phrase explaining why the mod couldn't be loaded (if applicable).</param>
        /// <param name="errorDetails">More detailed details about the error intended for developers (if any).</param>
        /// <returns>Returns whether the mod was successfully loaded.</returns>
        private bool TryLoadMod(IModMetadata mod, IModMetadata[] mods, AssemblyLoader assemblyLoader, InterfaceProxyFactory proxyFactory, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase, HashSet<string> suppressUpdateChecks, out string errorReasonPhrase, out string errorDetails)
        {
            errorDetails = null;

            // log entry
            {
                string relativePath = mod.GetRelativePathWithRoot();
                if (mod.IsContentPack)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}) [content pack]...");
                else if (mod.Manifest?.EntryDll != null)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}{Path.DirectorySeparatorChar}{mod.Manifest.EntryDll})..."); // don't use Path.Combine here, since EntryDLL might not be valid
                else
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath})...");
            }

            // add warning for missing update key
            if (mod.HasID() && !suppressUpdateChecks.Contains(mod.Manifest.UniqueID) && !mod.HasValidUpdateKeys())
                mod.SetWarning(ModWarning.NoUpdateKeys);

            // validate status
            if (mod.Status == ModMetadataStatus.Failed)
            {
                this.Monitor.Log($"      Failed: {mod.Error}");
                errorReasonPhrase = mod.Error;
                return false;
            }

            // validate dependencies
            // Although dependencies are validated before mods are loaded, a dependency may have failed to load.
            if (mod.Manifest.Dependencies?.Any() == true)
            {
                foreach (IManifestDependency dependency in mod.Manifest.Dependencies.Where(p => p.IsRequired))
                {
                    if (this.ModRegistry.Get(dependency.UniqueID) == null)
                    {
                        string dependencyName = mods
                            .FirstOrDefault(otherMod => otherMod.HasID(dependency.UniqueID))
                            ?.DisplayName ?? dependency.UniqueID;
                        errorReasonPhrase = $"it needs the '{dependencyName}' mod, which couldn't be loaded.";
                        return false;
                    }
                }
            }

            // load as content pack
            if (mod.IsContentPack)
            {
                IManifest manifest = mod.Manifest;
                IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                IContentHelper contentHelper = new ContentHelper(this.ContentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                IContentPack contentPack = new ContentPack(mod.DirectoryPath, manifest, contentHelper, translationHelper, jsonHelper);
                mod.SetMod(contentPack, monitor, translationHelper);
                this.ModRegistry.Add(mod);

                errorReasonPhrase = null;
                return true;
            }

            // load as mod
            else
            {
                IManifest manifest = mod.Manifest;

                // load mod
                string assemblyPath = manifest?.EntryDll != null
                    ? Path.Combine(mod.DirectoryPath, manifest.EntryDll)
                    : null;
                Assembly modAssembly;
                try
                {
                    modAssembly = assemblyLoader.Load(mod, assemblyPath, assumeCompatible: mod.DataRecord?.Status == ModStatus.AssumeCompatible);
                    this.ModRegistry.TrackAssemblies(mod, modAssembly);
                }
                catch (IncompatibleInstructionException) // details already in trace logs
                {
                    string[] updateUrls = new[] { modDatabase.GetModPageUrlFor(manifest.UniqueID), "https://smapi.io/mods" }.Where(p => p != null).ToArray();
                    errorReasonPhrase = $"it's no longer compatible. Please check for a new version at {string.Join(" or ", updateUrls)}";
                    return false;
                }
                catch (SAssemblyLoadFailedException ex)
                {
                    errorReasonPhrase = $"its DLL couldn't be loaded: {ex.Message}";
                    return false;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = "its DLL couldn't be loaded.";
                    errorDetails = $"Error: {ex.GetLogSummary()}";
                    return false;
                }

                // initialize mod
                try
                {
                    // get mod instance
                    if (!this.TryLoadModEntry(modAssembly, out Mod modEntry, out errorReasonPhrase))
                        return false;

                    // get content packs
                    IContentPack[] GetContentPacks()
                    {
                        if (!this.ModRegistry.AreAllModsLoaded)
                            throw new InvalidOperationException("Can't access content packs before SMAPI finishes loading mods.");

                        return this.ModRegistry
                            .GetAll(assemblyMods: false)
                            .Where(p => p.IsContentPack && mod.HasID(p.Manifest.ContentPackFor.UniqueID))
                            .Select(p => p.ContentPack)
                            .ToArray();
                    }

                    // init mod helpers
                    IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                    TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                    IModHelper modHelper;
                    {
                        IContentPack CreateFakeContentPack(string packDirPath, IManifest packManifest)
                        {
                            IMonitor packMonitor = this.LogManager.GetMonitor(packManifest.Name);
                            IContentHelper packContentHelper = new ContentHelper(contentCore, packDirPath, packManifest.UniqueID, packManifest.Name, packMonitor);
                            ITranslationHelper packTranslationHelper = new TranslationHelper(packManifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                            return new ContentPack(packDirPath, packManifest, packContentHelper, packTranslationHelper, this.Toolkit.JsonHelper);
                        }

                        IModEvents events = new ModEvents(mod, this.EventManager);
                        ICommandHelper commandHelper = new CommandHelper(mod, this.CommandManager);
                        IContentHelper contentHelper = new ContentHelper(contentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                        IContentPackHelper contentPackHelper = new ContentPackHelper(manifest.UniqueID, new Lazy<IContentPack[]>(GetContentPacks), CreateFakeContentPack);
                        IDataHelper dataHelper = new DataHelper(manifest.UniqueID, mod.DirectoryPath, jsonHelper);
                        IReflectionHelper reflectionHelper = new ReflectionHelper(manifest.UniqueID, mod.DisplayName, this.Reflection);
                        IModRegistry modRegistryHelper = new ModRegistryHelper(manifest.UniqueID, this.ModRegistry, proxyFactory, monitor);
                        IMultiplayerHelper multiplayerHelper = new MultiplayerHelper(manifest.UniqueID, this.Multiplayer);

                        modHelper = new ModHelper(manifest.UniqueID, mod.DirectoryPath, this.Input, events, contentHelper, contentPackHelper, commandHelper, dataHelper, modRegistryHelper, reflectionHelper, multiplayerHelper, translationHelper);
                    }

                    // init mod
                    modEntry.ModManifest = manifest;
                    modEntry.Helper = modHelper;
                    modEntry.Monitor = monitor;

                    // track mod
                    mod.SetMod(modEntry, translationHelper);
                    this.ModRegistry.Add(mod);
                    return true;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = $"initialization failed:\n{ex.GetLogSummary()}";
                    return false;
                }
            }
        }

        /// <summary>Load a mod's entry class.</summary>
        /// <param name="modAssembly">The mod assembly.</param>
        /// <param name="mod">The loaded instance.</param>
        /// <param name="error">The error indicating why loading failed (if applicable).</param>
        /// <returns>Returns whether the mod entry class was successfully loaded.</returns>
        private bool TryLoadModEntry(Assembly modAssembly, out Mod mod, out string error)
        {
            mod = null;

            // find type
            TypeInfo[] modEntries = modAssembly.DefinedTypes.Where(type => typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract).Take(2).ToArray();
            if (modEntries.Length == 0)
            {
                error = $"its DLL has no '{nameof(Mod)}' subclass.";
                return false;
            }
            if (modEntries.Length > 1)
            {
                error = $"its DLL contains multiple '{nameof(Mod)}' subclasses.";
                return false;
            }

            // get implementation
            mod = (Mod)modAssembly.CreateInstance(modEntries[0].ToString());
            if (mod == null)
            {
                error = "its entry class couldn't be instantiated.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Reload translations for all mods.</summary>
        private void ReloadTranslations()
        {
            this.ReloadTranslations(this.ModRegistry.GetAll());
        }

        /// <summary>Reload translations for the given mods.</summary>
        /// <param name="mods">The mods for which to reload translations.</param>
        private void ReloadTranslations(IEnumerable<IModMetadata> mods)
        {
            // core SMAPI translations
            {
                var translations = this.ReadTranslationFiles(Path.Combine(Constants.InternalFilesPath, "i18n"), out IList<string> errors);
                if (errors.Any() || !translations.Any())
                {
                    this.Monitor.Log("SMAPI couldn't load some core translations. You may need to reinstall SMAPI.", LogLevel.Warn);
                    foreach (string error in errors)
                        this.Monitor.Log($"  - {error}", LogLevel.Warn);
                }
                this.Translator.SetTranslations(translations);
            }

            // mod translations
            foreach (IModMetadata metadata in mods)
            {
                var translations = this.ReadTranslationFiles(Path.Combine(metadata.DirectoryPath, "i18n"), out IList<string> errors);
                if (errors.Any())
                {
                    metadata.LogAsMod("Mod couldn't load some translation files:", LogLevel.Warn);
                    foreach (string error in errors)
                        metadata.LogAsMod($"  - {error}", LogLevel.Warn);
                }
                metadata.Translations.SetTranslations(translations);
            }
        }

        /// <summary>Read translations from a directory containing JSON translation files.</summary>
        /// <param name="folderPath">The folder path to search.</param>
        /// <param name="errors">The errors indicating why translation files couldn't be parsed, indexed by translation filename.</param>
        private IDictionary<string, IDictionary<string, string>> ReadTranslationFiles(string folderPath, out IList<string> errors)
        {
            JsonHelper jsonHelper = this.Toolkit.JsonHelper;

            // read translation files
            var translations = new Dictionary<string, IDictionary<string, string>>();
            errors = new List<string>();
            DirectoryInfo translationsDir = new DirectoryInfo(folderPath);
            if (translationsDir.Exists)
            {
                foreach (FileInfo file in translationsDir.EnumerateFiles("*.json"))
                {
                    string locale = Path.GetFileNameWithoutExtension(file.Name.ToLower().Trim());
                    try
                    {
                        if (!jsonHelper.ReadJsonFileIfExists(file.FullName, out IDictionary<string, string> data))
                        {
                            errors.Add($"{file.Name} file couldn't be read"); // should never happen, since we're iterating files that exist
                            continue;
                        }
                        if(data != null)
                            translations[locale] = data;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file.Name} file couldn't be parsed: {ex.GetLogSummary()}");
                    }
                }
            }

            // validate translations
            foreach (string locale in translations.Keys.ToArray())
            {
                // handle duplicates
                HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in translations[locale].Keys.ToArray())
                {
                    if (!keys.Add(key))
                    {
                        duplicateKeys.Add(key);
                        translations[locale].Remove(key);
                    }
                }
                if (duplicateKeys.Any())
                    errors.Add($"{locale}.json has duplicate translation keys: [{string.Join(", ", duplicateKeys)}]. Keys are case-insensitive.");
            }

            return translations;
        }

        /// <summary>Get the absolute path to the next available log file.</summary>
        private string GetLogPath()
        {
            // default path
            {
                FileInfo defaultFile = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.{Constants.LogExtension}"));
                if (!defaultFile.Exists)
                    return defaultFile.FullName;
            }

            // get first disambiguated path
            for (int i = 2; i < int.MaxValue; i++)
            {
                FileInfo file = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.player-{i}.{Constants.LogExtension}"));
                if (!file.Exists)
                    return file.FullName;
            }

            // should never happen
            throw new InvalidOperationException("Could not find an available log path.");
        }

        /// <summary>Delete normal (non-crash) log files created by SMAPI.</summary>
        private void PurgeNormalLogs()
        {
            DirectoryInfo logsDir = new DirectoryInfo(Constants.LogDir);
            if (!logsDir.Exists)
                return;

            foreach (FileInfo logFile in logsDir.EnumerateFiles())
            {
                // skip non-SMAPI file
                if (!logFile.Name.StartsWith(Constants.LogNamePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                // skip crash log
                if (logFile.FullName == Constants.FatalCrashLog)
                    continue;

                // delete file
                try
                {
                    FileUtilities.ForceDelete(logFile);
                }
                catch (IOException)
                {
                    // ignore file if it's in use
                }
            }
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="message">The fatal log message.</param>
        private void ExitGameImmediately(string message)
        {
            this.Monitor.LogFatal(message);
            this.CancellationToken.Cancel();
        }
    }
}
