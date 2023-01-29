using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
#if SMAPI_FOR_MOBILE
using System.Collections.Generic;
using System.Threading.Tasks;
using StardewValley.Minigames;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;
#endif

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    internal class SGame : Game1
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager Events;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new(60); // 60 ticks = roughly one second

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        /// <summary>The initial override for <see cref="Input"/>. This value is null after initialization.</summary>
        private SInputState? InitialInput;

        /// <summary>The initial override for <see cref="Multiplayer"/>. This value is null after initialization.</summary>
        private SMultiplayer? InitialMultiplayer;

        /// <summary>Raised when the instance is updating its state (roughly 60 times per second).</summary>
        private readonly Action<SGame, GameTime, Action> OnUpdating;

        /// <summary>Raised after the instance finishes loading its initial content.</summary>
        private readonly Action OnContentLoaded;

#if SMAPI_FOR_MOBILE
        private readonly IReflectedField<StringBuilder> DebugStringBuilderField;
        private readonly IReflectedField<Task> NewDayTaskField;

        private readonly IReflectedMethod DrawTapToMoveTargetMethod;
        private readonly IReflectedMethod DrawGreenPlacementBoundsMethod;
#endif

        /*********
        ** Accessors
        *********/
        /// <summary>Manages input visible to the game.</summary>
        public SInputState Input => (SInputState)Game1.input;

        /// <summary>Monitors the entire game state for changes.</summary>
        public WatcherCore Watchers { get; private set; } = null!; // initialized on first update tick

        /// <summary>A snapshot of the current <see cref="Watchers"/> state.</summary>
        public WatcherSnapshot WatcherSnapshot { get; } = new();

        /// <summary>Whether the current update tick is the first one for this instance.</summary>
        public bool IsFirstTick = true;

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        public Countdown AfterLoadTimer { get; } = new(5);

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        public bool IsBetweenSaveEvents { get; set; }

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        public bool IsBetweenCreateEvents { get; set; }

        /// <summary>The cached <see cref="Farmer.UniqueMultiplayerID"/> value for this instance's player.</summary>
        public long? PlayerId { get; private set; }

        /// <summary>Construct a content manager to read game content files.</summary>
        /// <remarks>This must be static because the game accesses it before the <see cref="SGame"/> constructor is called.</remarks>
        [NonInstancedStatic] public static Func<IServiceProvider, string, LocalizedContentManager>? CreateContentManagerImpl;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="instanceIndex">The instance index.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="input">Manages the game's input state.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        /// <param name="onUpdating">Raised when the instance is updating its state (roughly 60 times per second).</param>
        /// <param name="onContentLoaded">Raised after the game finishes loading its initial content.</param>
        public SGame(PlayerIndex playerIndex, int instanceIndex, Monitor monitor, Reflector reflection, EventManager eventManager, SInputState input, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action<SGame, GameTime, Action> onUpdating, Action onContentLoaded)
            : base(playerIndex, instanceIndex)
        {
            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // hook into game
            Game1.input = this.InitialInput = input;
            Game1.multiplayer = this.InitialMultiplayer = multiplayer;
            Game1.hooks = modHooks;
            // this._locations = new ObservableCollection<GameLocation>();

            // init SMAPI
            this.Monitor = monitor;
            this.Events = eventManager;
            this.Reflection = reflection;
            this.ExitGameImmediately = exitGameImmediately;
            this.OnUpdating = onUpdating;
            this.OnContentLoaded = onContentLoaded;

#if SMAPI_FOR_MOBILE
            // init reflection fields
            this.DebugStringBuilderField = this.Reflection.GetField<StringBuilder>(typeof(Game1), "_debugStringBuilder");
            this.NewDayTaskField = this.Reflection.GetField<Task>(this, "_newDayTask");

            this.DrawTapToMoveTargetMethod = this.Reflection.GetMethod(this, "DrawTapToMoveTarget");
            this.DrawGreenPlacementBoundsMethod = this.Reflection.GetMethod(this, "DrawGreenPlacementBounds");
#endif
        }

        /// <summary>Get the current input state for a button.</summary>
        /// <param name="button">The button to check.</param>
        /// <remarks>This is intended for use by <see cref="Keybind"/> and shouldn't be used directly in most cases.</remarks>
        internal static SButtonState GetInputState(SButton button)
        {
            if (Game1.input is not SInputState inputHandler)
                throw new InvalidOperationException("SMAPI's input state is not in a ready state yet.");

            return inputHandler.GetState(button);
        }

        /// <inheritdoc />
        protected override void LoadContent()
        {
            base.LoadContent();

            this.OnContentLoaded();
        }

        /*********
        ** Protected methods
        *********/
        /// <summary>Construct a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        protected override LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (SGame.CreateContentManagerImpl == null)
                throw new InvalidOperationException($"The {nameof(SGame)}.{nameof(SGame.CreateContentManagerImpl)} must be set.");

            return SGame.CreateContentManagerImpl(serviceProvider, rootDirectory);
        }

        /// <summary>Initialize the instance when the game starts.</summary>
        protected override void Initialize()
        {
            base.Initialize();

            // The game resets public static fields after the class is constructed (see GameRunner.SetInstanceDefaults), so SMAPI needs to re-override them here.
            Game1.input = this.InitialInput;
            Game1.multiplayer = this.InitialMultiplayer;

            // The Initial* fields should no longer be used after this point, since mods may further override them after initialization.
            this.InitialInput = null;
            this.InitialMultiplayer = null;
        }

        /// <summary>The method called when the instance is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            // set initial state
            if (this.IsFirstTick)
            {
                this.Input.TrueUpdate();
                this.Watchers = new WatcherCore(this.Input, this._locations);
            }

            // update
            try
            {
                this.OnUpdating(this, gameTime, () => base.Update(gameTime));
                this.PlayerId = Game1.player?.UniqueMultiplayerID;
            }
            finally
            {
                this.IsFirstTick = false;
            }
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
#if SMAPI_FOR_MOBILE
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen)
        {
            Context.IsInDrawLoop = true;
            try
            {
                if (SGameConsole.Instance.isVisible)
                {
                    Game1.game1.GraphicsDevice.SetRenderTarget(Game1.game1.screen);
                    Game1.game1.GraphicsDevice.Clear(Color.Black);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        null,
                        null,
                        null,
                        null);
                    SGameConsole.Instance.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                    Game1.game1.GraphicsDevice.SetRenderTarget(null);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        DepthStencilState.Default,
                        RasterizerState.CullNone,
                        null,
                        null);
                    Game1.spriteBatch.Draw(Game1.game1.screen,
                        Vector2.Zero,
                        Game1.game1.screen.Bounds,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        Game1.options.zoomLevel,
                        SpriteEffects.None,
                        1f);
                    Game1.spriteBatch.End();
                    return;
                }

                this.DrawImpl(gameTime, target_screen);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }
            }
            finally
            {
                // recover sprite batch
                try
                {
                    if (Game1.spriteBatch.IsOpen(this.Reflection))
                    {
                        this.Monitor.Log("Recovering sprite batch from error...");
                        Game1.spriteBatch.End();
                    }
                }
                catch (Exception innerEx)
                {
                    this.Monitor.Log($"Could not recover sprite batch state: {innerEx.GetLogSummary()}", LogLevel.Error);
                }
            }

            Context.IsInDrawLoop = false;
        }
#else
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen)
        {
            Context.IsInDrawLoop = true;
            try
            {
                this.DrawImpl(gameTime, target_screen);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occurred in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }

                // recover draw state
                try
                {
                    if (Game1.spriteBatch.IsOpen(this.Reflection))
                    {
                        this.Monitor.Log("Recovering sprite batch from error...");
                        Game1.spriteBatch.End();
                    }

                    Game1.uiMode = false;
                    Game1.uiModeCount = 0;
                    Game1.nonUIRenderTarget = null;
                }
                catch (Exception innerEx)
                {
                    this.Monitor.Log($"Could not recover game draw state: {innerEx.GetLogSummary()}", LogLevel.Error);
                }
            }
            Context.IsInDrawLoop = false;
        }
#endif

#nullable disable
        /// <summary>Replicate the game's draw logic with some changes for SMAPI.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        /// <remarks>This implementation is identical to <see cref="Game1._draw"/>, except for try..catch around menu draw code, private field references replaced by wrappers, and added events.</remarks>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "LocalVariableHidesMember", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "MergeIntoPattern", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantBaseQualifier", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantCast", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantExplicitNullableCreation", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "MergeIntoPattern", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidImplicitNetFieldCast", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "Deliberate to minimize chance of errors when copying event calls into new versions of this code.")]
#if SMAPI_FOR_MOBILE
        private void DrawImpl(GameTime gameTime, RenderTarget2D target_screen)
        {
            showingHealthBar = false;
            if ((this.NewDayTaskField.GetValue() != null) || this.isLocalMultiplayerNewDayActive)
            {
                base.GraphicsDevice.Clear(bgColor);
            }
            else
            {
                Matrix? nullable;
                if (target_screen != null)
                {
                    SetRenderTarget(target_screen);
                }

                if (this.IsSaving)
                {
                    base.GraphicsDevice.Clear(bgColor);
                    PushUIMode();
                    IClickableMenu activeClickableMenu = Game1.activeClickableMenu;
                    if (activeClickableMenu != null)
                    {
                        nullable = null;
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                        activeClickableMenu.draw(spriteBatch);
                        spriteBatch.End();
                    }

                    if (overlayMenu != null)
                    {
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        overlayMenu.draw(spriteBatch);
                        spriteBatch.End();
                    }

                    PopUIMode();
                }
                else
                {
                    base.GraphicsDevice.Clear(bgColor);
                    if (((Game1.activeClickableMenu != null) && options.showMenuBackground) && (Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !this.takingMapScreenshot))
                    {
                        PushUIMode();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        Game1.activeClickableMenu.drawBackground(spriteBatch);
                        for (IClickableMenu menu2 = Game1.activeClickableMenu; menu2 != null; menu2 = menu2.GetChildMenu())
                        {
                            menu2.draw(spriteBatch);
                        }

                        if (specialCurrencyDisplay != null)
                        {
                            specialCurrencyDisplay.Draw(spriteBatch);
                        }

                        spriteBatch.End();
                        this.drawOverlays(spriteBatch);
                        PopUIMode();
                    }
                    else
                    {
                        if (emergencyLoading)
                        {
                            if (!SeenConcernedApeLogo)
                            {
                                PushUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (logoFadeTimer < 0x1388)
                                {
                                    spriteBatch.Draw(staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
                                }

                                if (logoFadeTimer > 0x1194)
                                {
                                    float num = Math.Min((float)1f, (float)(((float)(logoFadeTimer - 0x1194)) / 500f));
                                    spriteBatch.Draw(staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * num);
                                }

                                spriteBatch.Draw(titleButtonsTexture, new Vector2((float)(Game1.viewport.Width / 2), (float)((Game1.viewport.Height / 2) - 90)), new Rectangle(0xab + ((((logoFadeTimer / 100) % 2) == 0) ? 0x6f : 0), 0x137, 0x6f, 60), Color.White * ((logoFadeTimer < 500) ? (((float)logoFadeTimer) / 500f) : ((logoFadeTimer > 0x1194) ? (1f - (((float)(logoFadeTimer - 0x1194)) / 500f)) : 1f)), 0f, Vector2.Zero, (float)3f, SpriteEffects.None, 0.2f);
                                spriteBatch.Draw(titleButtonsTexture, new Vector2((float)((Game1.viewport.Width / 2) - 0x105), (float)((Game1.viewport.Height / 2) - 0x66)), new Rectangle((((logoFadeTimer / 100) % 2) == 0) ? 0x55 : 0, 0x132, 0x55, 0x45), Color.White * ((logoFadeTimer < 500) ? (((float)logoFadeTimer) / 500f) : ((logoFadeTimer > 0x1194) ? (1f - (((float)(logoFadeTimer - 0x1194)) / 500f)) : 1f)), 0f, Vector2.Zero, (float)3f, SpriteEffects.None, 0.2f);
                                spriteBatch.End();
                                PopUIMode();
                            }

                            logoFadeTimer -= gameTime.ElapsedGameTime.Milliseconds;
                        }

                        if (Game1.gameMode == 11)
                        {
                            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                            spriteBatch.DrawString(dialogueFont, content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                            spriteBatch.DrawString(dialogueFont, content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 0xff, 0));
                            spriteBatch.DrawString(dialogueFont, parseText(errorMessage, dialogueFont, graphics.GraphicsDevice.Viewport.Width, 1f), new Vector2(16f, 48f), Color.White);
                            spriteBatch.End();
                        }
                        else if (currentMinigame != null)
                        {
                            currentMinigame.draw(spriteBatch);
                            if ((globalFade && !menuUp) && (!nameSelectUp || messagePause))
                            {
                                PushUIMode();
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - fadeToBlackAlpha) : fadeToBlackAlpha));
                                spriteBatch.End();
                                PopUIMode();
                            }

                            PushUIMode();
                            this.drawOverlays(spriteBatch);
                            PopUIMode();
                            SetRenderTarget(target_screen);
                        }
                        else if (showingEndOfNightStuff)
                        {
                            PushUIMode();
                            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                            if (Game1.activeClickableMenu != null)
                            {
                                for (IClickableMenu menu3 = Game1.activeClickableMenu; menu3 != null; menu3 = menu3.GetChildMenu())
                                {
                                    menu3.draw(spriteBatch);
                                }
                            }

                            spriteBatch.End();
                            this.drawOverlays(spriteBatch);
                            PopUIMode();
                        }
                        else if ((Game1.gameMode == 6) || ((Game1.gameMode == 3) && (currentLocation == null)))
                        {
                            PushUIMode();
                            base.GraphicsDevice.Clear(bgColor);
                            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                            string str = "";
                            for (int i = 0; i < ((gameTime.TotalGameTime.TotalMilliseconds % 999.0) / 333.0); i++)
                            {
                                str = str + ".";
                            }

                            string text1 = content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.3688");
                            string s = text1 + str;
                            string str3 = text1 + "... ";
                            int width = SpriteText.getWidthOfString(str3, 0xf423f);
                            int height = 0x40;
                            int x = 0x40;
                            int y = graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - height;
                            int num6 = 20;
                            x = (xEdge > 0) ? (0x33 + xEdge) : (0x33 + num6);
                            y = (uiViewport.Height - 0x3f) - num6;
                            SpriteText.drawString(spriteBatch, s, x, y, 0xf423f, width, height, 1f, 0.88f, false, 0, str3, -1, SpriteText.ScrollTextAlignment.Left);
                            spriteBatch.End();
                            this.drawOverlays(spriteBatch);
                            byte gameMode = Game1.gameMode;
                            PopUIMode();
                        }
                        else
                        {
                            if (Game1.gameMode == 0)
                            {
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            }
                            else
                            {
                                if (((Game1.gameMode == 3) && (dayOfMonth == 0)) && newDay)
                                {
                                    base.Draw(gameTime);
                                    return;
                                }

                                if (drawLighting)
                                {
                                    Color ambientLight;
                                    SetRenderTarget(lightmap);
                                    base.GraphicsDevice.Clear(Color.White * 0f);
                                    Matrix identity = Matrix.Identity;
                                    if (this.useUnscaledLighting)
                                    {
                                        identity = Matrix.CreateScale(options.zoomLevel);
                                    }

                                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, new Matrix?(identity));
                                    if (currentLocation.Name.StartsWith("UndergroundMine") && (currentLocation is MineShaft))
                                    {
                                        ambientLight = (currentLocation as MineShaft).getLightingColor(gameTime);
                                    }
                                    else if (!Game1.ambientLight.Equals(Color.White) && (!IsRainingHere(null) || (currentLocation.isOutdoors == null)))
                                    {
                                        ambientLight = Game1.ambientLight;
                                    }
                                    else
                                    {
                                        ambientLight = outdoorLight;
                                    }

                                    float num8 = 1f;
                                    if (player.hasBuff(0x1a))
                                    {
                                        if (ambientLight == Color.White)
                                        {
                                            ambientLight = new Color(0.75f, 0.75f, 0.75f);
                                        }
                                        else
                                        {
                                            ambientLight.R = (byte)Utility.Lerp((float)ambientLight.R, 255f, 0.5f);
                                            ambientLight.G = (byte)Utility.Lerp((float)ambientLight.G, 255f, 0.5f);
                                            ambientLight.B = (byte)Utility.Lerp((float)ambientLight.B, 255f, 0.5f);
                                        }

                                        num8 = 0.33f;
                                    }

                                    spriteBatch.Draw(staminaRect, lightmap.Bounds, ambientLight);
                                    foreach (LightSource source in currentLightSources)
                                    {
                                        if ((!IsRainingHere(null) && !isDarkOut()) || (source.lightContext.Value != LightSource.LightContext.WindowLight))
                                        {
                                            if ((source.PlayerID != 0) && (source.PlayerID != player.UniqueMultiplayerID))
                                            {
                                                Farmer farmer = getFarmerMaybeOffline(source.PlayerID);
                                                if (((farmer == null) || ((farmer.currentLocation != null) && (farmer.currentLocation.Name != currentLocation.Name))) || (farmer.hidden != null))
                                                {
                                                    continue;
                                                }
                                            }

                                            if (Utility.isOnScreen((Vector2)source.position, (int)((source.radius * 64f) * 4f)))
                                            {
                                                spriteBatch.Draw(source.lightTexture, GlobalToLocal(Game1.viewport, (Vector2)source.position) / ((float)(options.lightingQuality / 2)), new Rectangle?(source.lightTexture.Bounds), source.color.Value * num8, 0f, new Vector2((float)(source.lightTexture.Bounds.Width / 2), (float)(source.lightTexture.Bounds.Height / 2)), (float)(source.radius / ((float)(options.lightingQuality / 2))), SpriteEffects.None, 0.9f);
                                            }
                                        }
                                    }

                                    spriteBatch.End();
                                    SetRenderTarget(target_screen);
                                }

                                base.GraphicsDevice.Clear(bgColor);
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (background != null)
                                {
                                    background.draw(spriteBatch);
                                }

                                currentLocation.drawBackground(spriteBatch);
                                mapDisplayDevice.BeginScene(spriteBatch);
                                currentLocation.Map.GetLayer("Back").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                currentLocation.drawWater(spriteBatch);
                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                currentLocation.drawFloorDecorations(spriteBatch);
                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                this._farmerShadows.Clear();
                                if (((currentLocation.currentEvent != null) && !currentLocation.currentEvent.isFestival) && (currentLocation.currentEvent.farmerActors.Count > 0))
                                {
                                    foreach (Farmer farmer2 in currentLocation.currentEvent.farmerActors)
                                    {
                                        if ((farmer2.IsLocalPlayer && displayFarmer) || (farmer2.hidden == null))
                                        {
                                            this._farmerShadows.Add(farmer2);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (Farmer farmer3 in currentLocation.farmers)
                                    {
                                        if ((farmer3.IsLocalPlayer && displayFarmer) || (farmer3.hidden == null))
                                        {
                                            this._farmerShadows.Add(farmer3);
                                        }
                                    }
                                }

                                if (!currentLocation.shouldHideCharacters())
                                {
                                    if (CurrentEvent == null)
                                    {
                                        foreach (NPC npc in currentLocation.characters)
                                        {
                                            if (((npc.swimming == null) && !npc.HideShadow) && (!npc.IsInvisible && !this.checkCharacterTilesForShadowDrawFlag(npc)))
                                            {
                                                spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, (npc.GetShadowOffset() + npc.Position) + new Vector2(((float)(npc.GetSpriteWidthForPositioning() * 4)) / 2f, (float)(npc.GetBoundingBox().Height + (npc.IsMonster ? 0 : 12)))), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), Math.Max((float)0f, (float)((4f + (((float)npc.yJumpOffset) / 40f)) * npc.scale)), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc.getStandingY()) / 10000f)) - 1E-06f);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (NPC npc2 in CurrentEvent.actors)
                                        {
                                            if ((((CurrentEvent == null) || !CurrentEvent.ShouldHideCharacter(npc2)) && ((npc2.swimming == null) && !npc2.HideShadow)) && !this.checkCharacterTilesForShadowDrawFlag(npc2))
                                            {
                                                spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, (npc2.GetShadowOffset() + npc2.Position) + new Vector2(((float)(npc2.GetSpriteWidthForPositioning() * 4)) / 2f, (float)(npc2.GetBoundingBox().Height + (npc2.IsMonster ? 0 : ((npc2.Sprite.SpriteHeight <= 0x10) ? -4 : 12))))), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), (float)(Math.Max((float)0f, (float)(4f + (((float)npc2.yJumpOffset) / 40f))) * npc2.scale), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc2.getStandingY()) / 10000f)) - 1E-06f);
                                            }
                                        }
                                    }

                                    foreach (Farmer farmer4 in this._farmerShadows)
                                    {
                                        if (((!multiplayer.isDisconnecting(farmer4.UniqueMultiplayerID) && (farmer4.swimming == null)) && (!farmer4.isRidingHorse() && !farmer4.IsSitting())) && ((currentLocation == null) || !this.checkCharacterTilesForShadowDrawFlag(farmer4)))
                                        {
                                            spriteBatch.Draw(shadowTexture, GlobalToLocal((farmer4.GetShadowOffset() + farmer4.Position) + new Vector2(32f, 24f)), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), (float)(4f - (((farmer4.running || farmer4.UsingTool) && (farmer4.FarmerSprite.currentAnimationIndex > 1)) ? (Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmer4.FarmerSprite.CurrentFrame]) * 0.5f) : 0f)), SpriteEffects.None, 0f);
                                        }
                                    }
                                }

                                Layer layer = currentLocation.Map.GetLayer("Buildings");
                                layer.Draw(mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                mapDisplayDevice.EndScene();
                                if ((currentLocation != null) && (currentLocation.tapToMove.targetNPC != null))
                                {
                                    spriteBatch.Draw(mouseCursors, GlobalToLocal(Game1.viewport, currentLocation.tapToMove.targetNPC.Position + new Vector2((((float)(currentLocation.tapToMove.targetNPC.Sprite.SpriteWidth * 4)) / 2f) - 32f, (float)((currentLocation.tapToMove.targetNPC.GetBoundingBox().Height + (currentLocation.tapToMove.targetNPC.IsMonster ? 0 : 12)) - 0x20))), new Rectangle(0xc2, 0x184, 0x10, 0x10), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 0.58f);
                                }

                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (!currentLocation.shouldHideCharacters())
                                {
                                    if (CurrentEvent == null)
                                    {
                                        foreach (NPC npc3 in currentLocation.characters)
                                        {
                                            if (((npc3.swimming == null) && !npc3.HideShadow) && ((npc3.isInvisible == null) && this.checkCharacterTilesForShadowDrawFlag(npc3)))
                                            {
                                                spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, (npc3.GetShadowOffset() + npc3.Position) + new Vector2(((float)(npc3.GetSpriteWidthForPositioning() * 4)) / 2f, (float)(npc3.GetBoundingBox().Height + (npc3.IsMonster ? 0 : 12)))), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), Math.Max((float)0f, (float)((4f + (((float)npc3.yJumpOffset) / 40f)) * npc3.scale)), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc3.getStandingY()) / 10000f)) - 1E-06f);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (NPC npc4 in CurrentEvent.actors)
                                        {
                                            if ((((CurrentEvent == null) || !CurrentEvent.ShouldHideCharacter(npc4)) && ((npc4.swimming == null) && !npc4.HideShadow)) && this.checkCharacterTilesForShadowDrawFlag(npc4))
                                            {
                                                spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, (npc4.GetShadowOffset() + npc4.Position) + new Vector2(((float)(npc4.GetSpriteWidthForPositioning() * 4)) / 2f, (float)(npc4.GetBoundingBox().Height + (npc4.IsMonster ? 0 : 12)))), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), Math.Max((float)0f, (float)((4f + (((float)npc4.yJumpOffset) / 40f)) * npc4.scale)), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc4.getStandingY()) / 10000f)) - 1E-06f);
                                            }
                                        }
                                    }

                                    foreach (Farmer farmer5 in this._farmerShadows)
                                    {
                                        float layerDepth = Math.Max((float)0.0001f, (float)(farmer5.getDrawLayer() + 0.00011f)) - 0.0001f;
                                        if ((((farmer5.swimming == null) && !farmer5.isRidingHorse()) && (!farmer5.IsSitting() && (currentLocation != null))) && this.checkCharacterTilesForShadowDrawFlag(farmer5))
                                        {
                                            spriteBatch.Draw(shadowTexture, GlobalToLocal((farmer5.GetShadowOffset() + farmer5.Position) + new Vector2(32f, 24f)), new Rectangle?(shadowTexture.Bounds), Color.White, 0f, new Vector2((float)shadowTexture.Bounds.Center.X, (float)shadowTexture.Bounds.Center.Y), (float)(4f - (((farmer5.running || farmer5.UsingTool) && (farmer5.FarmerSprite.currentAnimationIndex > 1)) ? (Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmer5.FarmerSprite.CurrentFrame]) * 0.5f) : 0f)), SpriteEffects.None, layerDepth);
                                        }
                                    }
                                }

                                if ((eventUp || killScreen) && (!killScreen && (currentLocation.currentEvent != null)))
                                {
                                    currentLocation.currentEvent.draw(spriteBatch);
                                }

                                if (((player.currentUpgrade != null) && (player.currentUpgrade.daysLeftTillUpgradeDone <= 3)) && currentLocation.Name.Equals("Farm"))
                                {
                                    spriteBatch.Draw(player.currentUpgrade.workerTexture, GlobalToLocal(Game1.viewport, player.currentUpgrade.positionOfCarpenter), new Rectangle?(player.currentUpgrade.getSourceRectangle()), Color.White, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, (player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                                }

                                currentLocation.draw(spriteBatch);
                                foreach (KeyValuePair<Vector2, int> pair in crabPotOverlayTiles)
                                {
                                    Vector2 key = pair.Key;
                                    Tile tile = layer.Tiles[(int)key.X, (int)key.Y];
                                    if (tile != null)
                                    {
                                        Vector2 vector2 = GlobalToLocal(Game1.viewport, key * 64f);
                                        Location location = new Location((int)vector2.X, (int)vector2.Y);
                                        mapDisplayDevice.DrawTile(tile, location, ((key.Y * 64f) - 1f) / 10000f);
                                    }
                                }

                                if (eventUp && (currentLocation.currentEvent != null))
                                {
                                    string messageToScreen = currentLocation.currentEvent.messageToScreen;
                                }

                                if (((player.ActiveObject == null) && (player.UsingTool || pickingTool)) && ((player.CurrentTool != null) && (!player.CurrentTool.Name.Equals("Seeds") || pickingTool)))
                                {
                                    drawTool(player);
                                }

                                if (currentLocation.Name.Equals("Farm"))
                                {
                                    this.drawFarmBuildings();
                                }

                                if (tvStation >= 0)
                                {
                                    spriteBatch.Draw(tvStationTexture, GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)), new Rectangle(tvStation * 0x18, 0, 0x18, 15), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 1E-08f);
                                }

                                if (panMode)
                                {
                                    spriteBatch.Draw(fadeToBlackRect, new Rectangle((((int)Math.Floor((double)(((double)(getOldMouseX() + Game1.viewport.X)) / 64.0))) * 0x40) - Game1.viewport.X, (((int)Math.Floor((double)(((double)(getOldMouseY() + Game1.viewport.Y)) / 64.0))) * 0x40) - Game1.viewport.Y, 0x40, 0x40), Color.Lime * 0.75f);
                                    foreach (Warp warp in currentLocation.warps)
                                    {
                                        spriteBatch.Draw(fadeToBlackRect, new Rectangle((warp.X * 0x40) - Game1.viewport.X, (warp.Y * 0x40) - Game1.viewport.Y, 0x40, 0x40), Color.Red * 0.75f);
                                    }
                                }

                                mapDisplayDevice.BeginScene(spriteBatch);
                                currentLocation.Map.GetLayer("Front").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                mapDisplayDevice.EndScene();
                                currentLocation.drawAboveFrontLayer(spriteBatch);
                                if ((((currentLocation.tapToMove.targetNPC == null) && (displayHUD || eventUp)) && (((currentBillboard == 0) && (Game1.gameMode == 3)) && (!freezeControls && !panMode))) && !HostPaused)
                                {
                                    this.DrawTapToMoveTargetMethod.Invoke();
                                }

                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (currentLocation.Map.GetLayer("AlwaysFront") != null)
                                {
                                    mapDisplayDevice.BeginScene(spriteBatch);
                                    currentLocation.Map.GetLayer("AlwaysFront").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                    mapDisplayDevice.EndScene();
                                }

                                if (((toolHold > 400f) && (player.CurrentTool.UpgradeLevel >= 1)) && player.canReleaseTool)
                                {
                                    Color white = Color.White;
                                    switch ((((int)(toolHold / 600f)) + 2))
                                    {
                                        case 1:
                                            white = Tool.copperColor;
                                            break;

                                        case 2:
                                            white = Tool.steelColor;
                                            break;

                                        case 3:
                                            white = Tool.goldColor;
                                            break;

                                        case 4:
                                            white = Tool.iridiumColor;
                                            break;
                                    }

                                    spriteBatch.Draw(littleEffect, new Rectangle(((int)player.getLocalPosition(Game1.viewport).X) - 2, (((int)player.getLocalPosition(Game1.viewport).Y) - (player.CurrentTool.Name.Equals("Watering Can") ? 0 : 0x40)) - 2, ((int)((toolHold % 600f) * 0.08f)) + 4, 12), Color.Black);
                                    spriteBatch.Draw(littleEffect, new Rectangle((int)player.getLocalPosition(Game1.viewport).X, ((int)player.getLocalPosition(Game1.viewport).Y) - (player.CurrentTool.Name.Equals("Watering Can") ? 0 : 0x40), (int)((toolHold % 600f) * 0.08f), 8), white);
                                }

                                if (!IsFakedBlackScreen())
                                {
                                    this.drawWeather(gameTime, target_screen);
                                }

                                if (Game1.farmEvent != null)
                                {
                                    Game1.farmEvent.draw(spriteBatch);
                                }

                                if ((currentLocation.LightLevel > 0f) && (timeOfDay < 0x7d0))
                                {
                                    spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * currentLocation.LightLevel);
                                }

                                if (screenGlow)
                                {
                                    spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, screenGlowColor * screenGlowAlpha);
                                }

                                currentLocation.drawAboveAlwaysFrontLayer(spriteBatch);
                                if (((player.CurrentTool != null) && (player.CurrentTool is FishingRod)) && (((player.CurrentTool as FishingRod).isTimingCast || ((player.CurrentTool as FishingRod).castingChosenCountdown > 0f)) || ((player.CurrentTool as FishingRod).fishCaught || (player.CurrentTool as FishingRod).showingTreasure)))
                                {
                                    player.CurrentTool.draw(spriteBatch);
                                }

                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (eventUp && (currentLocation.currentEvent != null))
                                {
                                    foreach (NPC npc5 in currentLocation.currentEvent.actors)
                                    {
                                        if (npc5.isEmoting)
                                        {
                                            Vector2 position = npc5.getLocalPosition(Game1.viewport);
                                            if (npc5.NeedsBirdieEmoteHack())
                                            {
                                                position.X += 64f;
                                            }

                                            position.Y -= 140f;
                                            if (npc5.Age == 2)
                                            {
                                                position.Y += 32f;
                                            }
                                            else if (npc5.Gender == 1)
                                            {
                                                position.Y += 10f;
                                            }

                                            spriteBatch.Draw(emoteSpriteSheet, position, new Rectangle((npc5.CurrentEmoteIndex * 0x10) % emoteSpriteSheet.Width, ((npc5.CurrentEmoteIndex * 0x10) / emoteSpriteSheet.Width) * 0x10, 0x10, 0x10), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, ((float)npc5.getStandingY()) / 10000f);
                                        }
                                    }
                                }

                                spriteBatch.End();
                                if (drawLighting && !IsFakedBlackScreen())
                                {
                                    nullable = null;
                                    spriteBatch.Begin(SpriteSortMode.Deferred, this.lightingBlend, SamplerState.LinearClamp, null, null, null, nullable);
                                    Viewport viewport = base.GraphicsDevice.Viewport;
                                    viewport.Bounds = (target_screen != null) ? target_screen.Bounds : base.GraphicsDevice.PresentationParameters.Bounds;
                                    base.GraphicsDevice.Viewport = viewport;
                                    float scale = options.lightingQuality / 2;
                                    if (this.useUnscaledLighting)
                                    {
                                        scale /= options.zoomLevel;
                                    }

                                    spriteBatch.Draw(lightmap, Vector2.Zero, new Rectangle?(lightmap.Bounds), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                                    if ((IsRainingHere(null) && (currentLocation.isOutdoors != null)) && !(currentLocation is Desert))
                                    {
                                        spriteBatch.Draw(staminaRect, viewport.Bounds, Color.OrangeRed * 0.45f);
                                    }

                                    spriteBatch.End();
                                }

                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (drawGrid)
                                {
                                    int x = -Game1.viewport.X % 0x40;
                                    float num13 = -Game1.viewport.Y % 0x40;
                                    for (int i = x; i < graphics.GraphicsDevice.Viewport.Width; i += 0x40)
                                    {
                                        spriteBatch.Draw(staminaRect, new Rectangle(i, (int)num13, 1, graphics.GraphicsDevice.Viewport.Height), Color.Red * 0.5f);
                                    }

                                    for (float j = num13; j < graphics.GraphicsDevice.Viewport.Height; j += 64f)
                                    {
                                        spriteBatch.Draw(staminaRect, new Rectangle(x, (int)j, graphics.GraphicsDevice.Viewport.Width, 1), Color.Red * 0.5f);
                                    }
                                }

                                if (ShouldShowOnscreenUsernames() && (currentLocation != null))
                                {
                                    currentLocation.DrawFarmerUsernames(spriteBatch);
                                }

                                if ((currentBillboard != 0) && !this.takingMapScreenshot)
                                {
                                    this.drawBillboard();
                                }

                                if (((!eventUp && (Game1.farmEvent == null)) && ((currentBillboard == 0) && (Game1.gameMode == 3))) && (!this.takingMapScreenshot && isOutdoorMapSmallerThanViewport()))
                                {
                                    spriteBatch.Draw(fadeToBlackRect, new Rectangle(0, 0, -Game1.viewport.X, graphics.GraphicsDevice.Viewport.Height), Color.Black);
                                    spriteBatch.Draw(fadeToBlackRect, new Rectangle(-Game1.viewport.X + (currentLocation.map.Layers[0].LayerWidth * 0x40), 0, graphics.GraphicsDevice.Viewport.Width - (-Game1.viewport.X + (currentLocation.map.Layers[0].LayerWidth * 0x40)), graphics.GraphicsDevice.Viewport.Height), Color.Black);
                                    spriteBatch.Draw(fadeToBlackRect, new Rectangle(0, 0, graphics.GraphicsDevice.Viewport.Width, -Game1.viewport.Y), Color.Black);
                                    spriteBatch.Draw(fadeToBlackRect, new Rectangle(0, -Game1.viewport.Y + (currentLocation.map.Layers[0].LayerHeight * 0x40), graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height - (-Game1.viewport.Y + (currentLocation.map.Layers[0].LayerHeight * 0x40))), Color.Black);
                                }

                                spriteBatch.End();
                                PushUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                if (((displayHUD || eventUp) && ((currentBillboard == 0) && (Game1.gameMode == 3))) && ((!freezeControls && !panMode) && (!HostPaused && !this.takingMapScreenshot)))
                                {
                                    this.drawHUD();
                                    if (!this.takingMapScreenshot)
                                    {
                                        this.DrawGreenPlacementBoundsMethod.Invoke();
                                    }
                                }
                                else if (Game1.activeClickableMenu == null)
                                {
                                    FarmEvent farmEvent = Game1.farmEvent;
                                }

                                if ((hudMessages.Count > 0) && !this.takingMapScreenshot)
                                {
                                    for (int i = hudMessages.Count - 1; i >= 0; i--)
                                    {
                                        hudMessages[i].draw(spriteBatch, i);
                                    }
                                }

                                spriteBatch.End();
                                PopUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            }

                            if (Game1.farmEvent != null)
                            {
                                Game1.farmEvent.draw(spriteBatch);
                                spriteBatch.End();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            }

                            PushUIMode();
                            if (((dialogueUp && !nameSelectUp) && !messagePause) && (((Game1.activeClickableMenu == null) || !(Game1.activeClickableMenu is DialogueBox)) && !this.takingMapScreenshot))
                            {
                                this.drawDialogueBox();
                            }

                            if (progressBar && !this.takingMapScreenshot)
                            {
                                spriteBatch.Draw(fadeToBlackRect, new Rectangle((graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - dialogueWidth) / 2, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 0x80, dialogueWidth, 0x20), Color.LightGray);
                                spriteBatch.Draw(staminaRect, new Rectangle((graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - dialogueWidth) / 2, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 0x80, (int)((pauseAccumulator / pauseTime) * dialogueWidth), 0x20), Color.DimGray);
                            }

                            spriteBatch.End();
                            PopUIMode();
                            nullable = null;
                            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            if ((eventUp && (currentLocation != null)) && (currentLocation.currentEvent != null))
                            {
                                currentLocation.currentEvent.drawAfterMap(spriteBatch);
                            }

                            if (((!IsFakedBlackScreen() && IsRainingHere(null)) && ((currentLocation != null) && (currentLocation.isOutdoors != null))) && !(currentLocation is Desert))
                            {
                                spriteBatch.Draw(staminaRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Blue * 0.2f);
                            }

                            if (((fadeToBlack || globalFade) && !menuUp) && ((!nameSelectUp || messagePause) && !this.takingMapScreenshot))
                            {
                                spriteBatch.End();
                                PushUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - fadeToBlackAlpha) : fadeToBlackAlpha));
                                spriteBatch.End();
                                PopUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            }
                            else if ((flashAlpha > 0f) && !this.takingMapScreenshot)
                            {
                                if (options.screenFlash)
                                {
                                    spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.White * Math.Min(1f, flashAlpha));
                                }

                                flashAlpha -= 0.1f;
                            }

                            if ((messagePause || globalFade) && (dialogueUp && !this.takingMapScreenshot))
                            {
                                this.drawDialogueBox();
                            }

                            if (!this.takingMapScreenshot)
                            {
                                List<TemporaryAnimatedSprite>.Enumerator enumerator7;
                                using (enumerator7 = screenOverlayTempSprites.GetEnumerator())
                                {
                                    while (enumerator7.MoveNext())
                                    {
                                        enumerator7.Current.draw(spriteBatch, true, 0, 0, 1f);
                                    }
                                }

                                spriteBatch.End();
                                PushUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                using (enumerator7 = uiOverlayTempSprites.GetEnumerator())
                                {
                                    while (enumerator7.MoveNext())
                                    {
                                        enumerator7.Current.draw(spriteBatch, true, 0, 0, 1f);
                                    }
                                }

                                spriteBatch.End();
                                PopUIMode();
                                nullable = null;
                                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                            }

                            if (debugMode)
                            {
                                StringBuilder text = this.DebugStringBuilderField.GetValue();
                                text.Clear();
                                if (panMode)
                                {
                                    text.Append((int)((getOldMouseX() + Game1.viewport.X) / 0x40));
                                    text.Append(",");
                                    text.Append((int)((getOldMouseY() + Game1.viewport.Y) / 0x40));
                                }
                                else
                                {
                                    text.Append("player: ");
                                    text.Append((int)(player.getStandingX() / 0x40));
                                    text.Append(", ");
                                    text.Append((int)(player.getStandingY() / 0x40));
                                }

                                text.Append(" mouseTransparency: ");
                                text.Append(mouseCursorTransparency);
                                text.Append(" mousePosition: ");
                                text.Append(getMouseX());
                                text.Append(",");
                                text.Append(getMouseY());
                                text.Append(Environment.NewLine);
                                text.Append(" mouseWorldPosition: ");
                                text.Append((int)(getMouseX() + Game1.viewport.X));
                                text.Append(",");
                                text.Append((int)(getMouseY() + Game1.viewport.Y));
                                text.Append("  debugOutput: ");
                                text.Append(debugOutput);
                                spriteBatch.DrawString(smallFont, text, new Vector2((float)base.GraphicsDevice.Viewport.GetTitleSafeArea().X, (float)(base.GraphicsDevice.Viewport.GetTitleSafeArea().Y + (smallFont.LineSpacing * 8))), Color.Red, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, 0.9999999f);
                            }

                            spriteBatch.End();
                            PushUIMode();
                            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                            if (showKeyHelp && !this.takingMapScreenshot)
                            {
                                spriteBatch.DrawString(smallFont, keyHelpString, new Vector2(64f, ((Game1.viewport.Height - 0x40) - (dialogueUp ? (0xc0 + (isQuestion ? (questionChoices.Count * 0x40) : 0)) : 0)) - smallFont.MeasureString(keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, 0.9999999f);
                            }

                            if ((Game1.activeClickableMenu != null) && !this.takingMapScreenshot)
                            {
                                for (IClickableMenu menu4 = Game1.activeClickableMenu; menu4 != null; menu4 = menu4.GetChildMenu())
                                {
                                    menu4.draw(spriteBatch);
                                }
                            }
                            else if (Game1.farmEvent != null)
                            {
                                Game1.farmEvent.drawAboveEverything(spriteBatch);
                            }

                            if (specialCurrencyDisplay != null)
                            {
                                specialCurrencyDisplay.Draw(spriteBatch);
                            }

                            if ((emoteMenu != null) && !this.takingMapScreenshot)
                            {
                                emoteMenu.draw(spriteBatch);
                            }

                            if (HostPaused && !this.takingMapScreenshot)
                            {
                                string s = content.LoadString(@"Strings\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                                SpriteText.drawStringWithScrollBackground(spriteBatch, s, 0x60, 0x20, "", 1f, -1, SpriteText.ScrollTextAlignment.Left);
                            }

                            spriteBatch.End();
                            this.drawOverlays(spriteBatch);
                            PopUIMode();
                        }
                    }
                }
            }
        }

#else
        private void DrawImpl(GameTime gameTime, RenderTarget2D target_screen)
        {
            var events = this.Events;

            Game1.showingHealthBar = false;
            if (Game1._newDayTask != null || this.isLocalMultiplayerNewDayActive)
            {
                base.GraphicsDevice.Clear(Game1.bgColor);
                return;
            }
            if (target_screen != null)
            {
                Game1.SetRenderTarget(target_screen);
            }
            if (this.IsSaving)
            {
                base.GraphicsDevice.Clear(Game1.bgColor);
                Game1.PushUIMode();
                IClickableMenu menu = Game1.activeClickableMenu;
                if (menu != null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                    events.Rendering.RaiseEmpty();
                    try
                    {
                        events.RenderingActiveMenu.RaiseEmpty();
                        menu.draw(Game1.spriteBatch);
                        events.RenderedActiveMenu.RaiseEmpty();
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"The {activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                        activeClickableMenu.exitThisMenu();
                    }
                    events.Rendered.RaiseEmpty();
                    Game1.spriteBatch.End();
                }
                if (Game1.overlayMenu != null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                    Game1.overlayMenu.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                }
                Game1.PopUIMode();
                return;
            }
            base.GraphicsDevice.Clear(Game1.bgColor);
            if (Game1.activeClickableMenu != null && Game1.options.showMenuBackground && Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !this.takingMapScreenshot)
            {
                Game1.PushUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

                events.Rendering.RaiseEmpty();
                IClickableMenu curMenu = null;
                try
                {
                    Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                    events.RenderingActiveMenu.RaiseEmpty();
                    for (curMenu = Game1.activeClickableMenu; curMenu != null; curMenu = curMenu.GetChildMenu())
                    {
                        curMenu.draw(Game1.spriteBatch);
                    }
                    events.RenderedActiveMenu.RaiseEmpty();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The {curMenu.GetMenuChainLabel()} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                    Game1.activeClickableMenu.exitThisMenu();
                }
                events.Rendered.RaiseEmpty();
                if (Game1.specialCurrencyDisplay != null)
                {
                    Game1.specialCurrencyDisplay.Draw(Game1.spriteBatch);
                }
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                Game1.PopUIMode();
                return;
            }
            if (Game1.gameMode == 11)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                events.Rendering.RaiseEmpty();
                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 255, 0));
                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.parseText(Game1.errorMessage, Game1.dialogueFont, Game1.graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
                events.Rendered.RaiseEmpty();
                Game1.spriteBatch.End();
                return;
            }
            if (Game1.currentMinigame != null)
            {
                if (events.Rendering.HasListeners)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    events.Rendering.RaiseEmpty();
                    Game1.spriteBatch.End();
                }

                Game1.currentMinigame.draw(Game1.spriteBatch);
                if (Game1.globalFade && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause))
                {
                    Game1.PushUIMode();
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - Game1.fadeToBlackAlpha) : Game1.fadeToBlackAlpha));
                    Game1.spriteBatch.End();
                    Game1.PopUIMode();
                }
                Game1.PushUIMode();
                this.drawOverlays(Game1.spriteBatch);
                Game1.PopUIMode();
                if (events.Rendered.HasListeners)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    events.Rendered.RaiseEmpty();
                    Game1.spriteBatch.End();
                }
                Game1.SetRenderTarget(target_screen);
                return;
            }
            if (Game1.showingEndOfNightStuff)
            {
                Game1.PushUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                events.Rendering.RaiseEmpty();
                if (Game1.activeClickableMenu != null)
                {
                    IClickableMenu curMenu = null;
                    try
                    {
                        events.RenderingActiveMenu.RaiseEmpty();
                        for (curMenu = Game1.activeClickableMenu; curMenu != null; curMenu = curMenu.GetChildMenu())
                        {
                            curMenu.draw(Game1.spriteBatch);
                        }
                        events.RenderedActiveMenu.RaiseEmpty();
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"The {curMenu.GetMenuChainLabel()} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                        Game1.activeClickableMenu.exitThisMenu();
                    }
                }
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                Game1.PopUIMode();
                return;
            }
            if (Game1.gameMode == 6 || (Game1.gameMode == 3 && Game1.currentLocation == null))
            {
                Game1.PushUIMode();
                base.GraphicsDevice.Clear(Game1.bgColor);
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                events.Rendering.RaiseEmpty();
                string addOn = "";
                for (int i = 0; (double)i < gameTime.TotalGameTime.TotalMilliseconds % 999.0 / 333.0; i++)
                {
                    addOn += ".";
                }
                string text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3688");
                string msg = text + addOn;
                string largestMessage = text + "... ";
                int msgw = SpriteText.getWidthOfString(largestMessage);
                int msgh = 64;
                int msgx = 64;
                int msgy = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - msgh;
                SpriteText.drawString(Game1.spriteBatch, msg, msgx, msgy, 999999, msgw, msgh, 1f, 0.88f, junimoText: false, 0, largestMessage);
                events.Rendered.RaiseEmpty();
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                Game1.PopUIMode();
                return;
            }

            byte batchOpens = 0; // used for rendering event
            if (Game1.gameMode == 0)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                if (++batchOpens == 1)
                    events.Rendering.RaiseEmpty();
            }
            else
            {
                if (Game1.gameMode == 3 && Game1.dayOfMonth == 0 && Game1.newDay)
                {
                    //base.Draw(gameTime);
                    return;
                }
                if (Game1.drawLighting)
                {
                    Game1.SetRenderTarget(Game1.lightmap);
                    base.GraphicsDevice.Clear(Color.White * 0f);
                    Matrix lighting_matrix = Matrix.Identity;
                    if (this.useUnscaledLighting)
                    {
                        lighting_matrix = Matrix.CreateScale(Game1.options.zoomLevel);
                    }
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, lighting_matrix);
                    if (++batchOpens == 1)
                        events.Rendering.RaiseEmpty();
                    Color lighting = ((Game1.currentLocation.Name.StartsWith("UndergroundMine") && Game1.currentLocation is MineShaft) ? (Game1.currentLocation as MineShaft).getLightingColor(gameTime) : ((Game1.ambientLight.Equals(Color.White) || (Game1.IsRainingHere() && (bool)Game1.currentLocation.isOutdoors)) ? Game1.outdoorLight : Game1.ambientLight));
                    float light_multiplier = 1f;
                    if (Game1.player.hasBuff(26))
                    {
                        if (lighting == Color.White)
                        {
                            lighting = new Color(0.75f, 0.75f, 0.75f);
                        }
                        else
                        {
                            lighting.R = (byte)Utility.Lerp((int)lighting.R, 255f, 0.5f);
                            lighting.G = (byte)Utility.Lerp((int)lighting.G, 255f, 0.5f);
                            lighting.B = (byte)Utility.Lerp((int)lighting.B, 255f, 0.5f);
                        }
                        light_multiplier = 0.33f;
                    }
                    Game1.spriteBatch.Draw(Game1.staminaRect, Game1.lightmap.Bounds, lighting);
                    foreach (LightSource lightSource in Game1.currentLightSources)
                    {
                        if ((Game1.IsRainingHere() || Game1.isDarkOut()) && lightSource.lightContext.Value == LightSource.LightContext.WindowLight)
                        {
                            continue;
                        }
                        if (lightSource.PlayerID != 0L && lightSource.PlayerID != Game1.player.UniqueMultiplayerID)
                        {
                            Farmer farmer = Game1.getFarmerMaybeOffline(lightSource.PlayerID);
                            if (farmer == null || (farmer.currentLocation != null && farmer.currentLocation.Name != Game1.currentLocation.Name) || (bool)farmer.hidden)
                            {
                                continue;
                            }
                        }
                        if (Utility.isOnScreen(lightSource.position, (int)((float)lightSource.radius * 64f * 4f)))
                        {
                            Game1.spriteBatch.Draw(lightSource.lightTexture, Game1.GlobalToLocal(Game1.viewport, lightSource.position) / (Game1.options.lightingQuality / 2), lightSource.lightTexture.Bounds, lightSource.color.Value * light_multiplier, 0f, new Vector2(lightSource.lightTexture.Bounds.Width / 2, lightSource.lightTexture.Bounds.Height / 2), (float)lightSource.radius / (float)(Game1.options.lightingQuality / 2), SpriteEffects.None, 0.9f);
                        }
                    }
                    Game1.spriteBatch.End();
                    Game1.SetRenderTarget(target_screen);
                }
                base.GraphicsDevice.Clear(Game1.bgColor);
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                if (++batchOpens == 1)
                    events.Rendering.RaiseEmpty();
                events.RenderingWorld.RaiseEmpty();
                if (Game1.background != null)
                {
                    Game1.background.draw(Game1.spriteBatch);
                }
                Game1.currentLocation.drawBackground(Game1.spriteBatch);
                Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                Game1.currentLocation.Map.GetLayer("Back").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                Game1.currentLocation.drawWater(Game1.spriteBatch);
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                Game1.currentLocation.drawFloorDecorations(Game1.spriteBatch);
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                this._farmerShadows.Clear();
                if (Game1.currentLocation.currentEvent != null && !Game1.currentLocation.currentEvent.isFestival && Game1.currentLocation.currentEvent.farmerActors.Count > 0)
                {
                    foreach (Farmer f in Game1.currentLocation.currentEvent.farmerActors)
                    {
                        if ((f.IsLocalPlayer && Game1.displayFarmer) || !f.hidden)
                        {
                            this._farmerShadows.Add(f);
                        }
                    }
                }
                else
                {
                    foreach (Farmer f2 in Game1.currentLocation.farmers)
                    {
                        if ((f2.IsLocalPlayer && Game1.displayFarmer) || !f2.hidden)
                        {
                            this._farmerShadows.Add(f2);
                        }
                    }
                }
                if (!Game1.currentLocation.shouldHideCharacters())
                {
                    if (Game1.CurrentEvent == null)
                    {
                        foreach (NPC k in Game1.currentLocation.characters)
                        {
                            if (!k.swimming && !k.HideShadow && !k.IsInvisible && !this.checkCharacterTilesForShadowDrawFlag(k))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, k.GetShadowOffset() + k.Position + new Vector2((float)(k.GetSpriteWidthForPositioning() * 4) / 2f, k.GetBoundingBox().Height + ((!k.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Max(0f, (4f + (float)k.yJumpOffset / 40f) * (float)k.scale), SpriteEffects.None, Math.Max(0f, (float)k.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    else
                    {
                        foreach (NPC l in Game1.CurrentEvent.actors)
                        {
                            if ((Game1.CurrentEvent == null || !Game1.CurrentEvent.ShouldHideCharacter(l)) && !l.swimming && !l.HideShadow && !this.checkCharacterTilesForShadowDrawFlag(l))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, l.GetShadowOffset() + l.Position + new Vector2((float)(l.GetSpriteWidthForPositioning() * 4) / 2f, l.GetBoundingBox().Height + ((!l.IsMonster) ? ((l.Sprite.SpriteHeight <= 16) ? (-4) : 12) : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Max(0f, 4f + (float)l.yJumpOffset / 40f) * (float)l.scale, SpriteEffects.None, Math.Max(0f, (float)l.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    foreach (Farmer f3 in this._farmerShadows)
                    {
                        if (!Game1.multiplayer.isDisconnecting(f3.UniqueMultiplayerID) && !f3.swimming && !f3.isRidingHorse() && !f3.IsSitting() && (Game1.currentLocation == null || !this.checkCharacterTilesForShadowDrawFlag(f3)))
                        {
                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(f3.GetShadowOffset() + f3.Position + new Vector2(32f, 24f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f - (((f3.running || f3.UsingTool) && f3.FarmerSprite.currentAnimationIndex > 1) ? ((float)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[f3.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, 0f);
                        }
                    }
                }
                Layer building_layer = Game1.currentLocation.Map.GetLayer("Buildings");
                building_layer.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                Game1.mapDisplayDevice.EndScene();
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                if (!Game1.currentLocation.shouldHideCharacters())
                {
                    if (Game1.CurrentEvent == null)
                    {
                        foreach (NPC m in Game1.currentLocation.characters)
                        {
                            if (!m.swimming && !m.HideShadow && !m.isInvisible && this.checkCharacterTilesForShadowDrawFlag(m))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, m.GetShadowOffset() + m.Position + new Vector2((float)(m.GetSpriteWidthForPositioning() * 4) / 2f, m.GetBoundingBox().Height + ((!m.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Max(0f, (4f + (float)m.yJumpOffset / 40f) * (float)m.scale), SpriteEffects.None, Math.Max(0f, (float)m.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    else
                    {
                        foreach (NPC n in Game1.CurrentEvent.actors)
                        {
                            if ((Game1.CurrentEvent == null || !Game1.CurrentEvent.ShouldHideCharacter(n)) && !n.swimming && !n.HideShadow && this.checkCharacterTilesForShadowDrawFlag(n))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, n.GetShadowOffset() + n.Position + new Vector2((float)(n.GetSpriteWidthForPositioning() * 4) / 2f, n.GetBoundingBox().Height + ((!n.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Max(0f, (4f + (float)n.yJumpOffset / 40f) * (float)n.scale), SpriteEffects.None, Math.Max(0f, (float)n.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    foreach (Farmer f4 in this._farmerShadows)
                    {
                        float draw_layer = Math.Max(0.0001f, f4.getDrawLayer() + 0.00011f) - 0.0001f;
                        if (!f4.swimming && !f4.isRidingHorse() && !f4.IsSitting() && Game1.currentLocation != null && this.checkCharacterTilesForShadowDrawFlag(f4))
                        {
                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(f4.GetShadowOffset() + f4.Position + new Vector2(32f, 24f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f - (((f4.running || f4.UsingTool) && f4.FarmerSprite.currentAnimationIndex > 1) ? ((float)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[f4.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, draw_layer);
                        }
                    }
                }
                if ((Game1.eventUp || Game1.killScreen) && !Game1.killScreen && Game1.currentLocation.currentEvent != null)
                {
                    Game1.currentLocation.currentEvent.draw(Game1.spriteBatch);
                }
                if (Game1.player.currentUpgrade != null && Game1.player.currentUpgrade.daysLeftTillUpgradeDone <= 3 && Game1.currentLocation.Name.Equals("Farm"))
                {
                    Game1.spriteBatch.Draw(Game1.player.currentUpgrade.workerTexture, Game1.GlobalToLocal(Game1.viewport, Game1.player.currentUpgrade.positionOfCarpenter), Game1.player.currentUpgrade.getSourceRectangle(), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, (Game1.player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                }
                Game1.currentLocation.draw(Game1.spriteBatch);
                foreach (Vector2 tile_position in Game1.crabPotOverlayTiles.Keys)
                {
                    Tile tile = building_layer.Tiles[(int)tile_position.X, (int)tile_position.Y];
                    if (tile != null)
                    {
                        Vector2 vector_draw_position = Game1.GlobalToLocal(Game1.viewport, tile_position * 64f);
                        Location draw_location = new((int)vector_draw_position.X, (int)vector_draw_position.Y);
                        Game1.mapDisplayDevice.DrawTile(tile, draw_location, (tile_position.Y * 64f - 1f) / 10000f);
                    }
                }
                if (Game1.eventUp && Game1.currentLocation.currentEvent != null)
                {
                    _ = Game1.currentLocation.currentEvent.messageToScreen;
                }
                if (Game1.player.ActiveObject == null && (Game1.player.UsingTool || Game1.pickingTool) && Game1.player.CurrentTool != null && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool))
                {
                    Game1.drawTool(Game1.player);
                }
                if (Game1.currentLocation.Name.Equals("Farm"))
                {
                    this.drawFarmBuildings();
                }
                if (Game1.tvStation >= 0)
                {
                    Game1.spriteBatch.Draw(Game1.tvStationTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)), new Microsoft.Xna.Framework.Rectangle(Game1.tvStation * 24, 0, 24, 15), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
                }
                if (Game1.panMode)
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)Math.Floor((double)(Game1.getOldMouseX() + Game1.viewport.X) / 64.0) * 64 - Game1.viewport.X, (int)Math.Floor((double)(Game1.getOldMouseY() + Game1.viewport.Y) / 64.0) * 64 - Game1.viewport.Y, 64, 64), Color.Lime * 0.75f);
                    foreach (Warp w in Game1.currentLocation.warps)
                    {
                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(w.X * 64 - Game1.viewport.X, w.Y * 64 - Game1.viewport.Y, 64, 64), Color.Red * 0.75f);
                    }
                }
                Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                Game1.currentLocation.Map.GetLayer("Front").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                Game1.mapDisplayDevice.EndScene();
                Game1.currentLocation.drawAboveFrontLayer(Game1.spriteBatch);
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                if (Game1.currentLocation.Map.GetLayer("AlwaysFront") != null)
                {
                    Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                    Game1.currentLocation.Map.GetLayer("AlwaysFront").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                    Game1.mapDisplayDevice.EndScene();
                }
                if (Game1.toolHold > 400f && Game1.player.CurrentTool.UpgradeLevel >= 1 && Game1.player.canReleaseTool)
                {
                    Color barColor = Color.White;
                    switch ((int)(Game1.toolHold / 600f) + 2)
                    {
                        case 1:
                            barColor = Tool.copperColor;
                            break;
                        case 2:
                            barColor = Tool.steelColor;
                            break;
                        case 3:
                            barColor = Tool.goldColor;
                            break;
                        case 4:
                            barColor = Tool.iridiumColor;
                            break;
                    }
                    Game1.spriteBatch.Draw(Game1.littleEffect, new Microsoft.Xna.Framework.Rectangle((int)Game1.player.getLocalPosition(Game1.viewport).X - 2, (int)Game1.player.getLocalPosition(Game1.viewport).Y - ((!Game1.player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0) - 2, (int)(Game1.toolHold % 600f * 0.08f) + 4, 12), Color.Black);
                    Game1.spriteBatch.Draw(Game1.littleEffect, new Microsoft.Xna.Framework.Rectangle((int)Game1.player.getLocalPosition(Game1.viewport).X, (int)Game1.player.getLocalPosition(Game1.viewport).Y - ((!Game1.player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0), (int)(Game1.toolHold % 600f * 0.08f), 8), barColor);
                }
                if (!Game1.IsFakedBlackScreen())
                {
                    this.drawWeather(gameTime, target_screen);
                }
                if (Game1.farmEvent != null)
                {
                    Game1.farmEvent.draw(Game1.spriteBatch);
                }
                if (Game1.currentLocation.LightLevel > 0f && Game1.timeOfDay < 2000)
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * Game1.currentLocation.LightLevel);
                }
                if (Game1.screenGlow)
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Game1.screenGlowColor * Game1.screenGlowAlpha);
                }
                Game1.currentLocation.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is FishingRod && ((Game1.player.CurrentTool as FishingRod).isTimingCast || (Game1.player.CurrentTool as FishingRod).castingChosenCountdown > 0f || (Game1.player.CurrentTool as FishingRod).fishCaught || (Game1.player.CurrentTool as FishingRod).showingTreasure))
                {
                    Game1.player.CurrentTool.draw(Game1.spriteBatch);
                }
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                if (Game1.eventUp && Game1.currentLocation.currentEvent != null)
                {
                    foreach (NPC n2 in Game1.currentLocation.currentEvent.actors)
                    {
                        if (n2.isEmoting)
                        {
                            Vector2 emotePosition = n2.getLocalPosition(Game1.viewport);
                            if (n2.NeedsBirdieEmoteHack())
                            {
                                emotePosition.X += 64f;
                            }
                            emotePosition.Y -= 140f;
                            if (n2.Age == 2)
                            {
                                emotePosition.Y += 32f;
                            }
                            else if (n2.Gender == 1)
                            {
                                emotePosition.Y += 10f;
                            }
                            Game1.spriteBatch.Draw(Game1.emoteSpriteSheet, emotePosition, new Microsoft.Xna.Framework.Rectangle(n2.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, n2.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)n2.getStandingY() / 10000f);
                        }
                    }
                }
                Game1.spriteBatch.End();
                if (Game1.drawLighting && !Game1.IsFakedBlackScreen())
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, this.lightingBlend, SamplerState.LinearClamp);
                    Viewport vp = base.GraphicsDevice.Viewport;
                    vp.Bounds = target_screen?.Bounds ?? base.GraphicsDevice.PresentationParameters.Bounds;
                    base.GraphicsDevice.Viewport = vp;
                    float render_zoom = Game1.options.lightingQuality / 2;
                    if (this.useUnscaledLighting)
                    {
                        render_zoom /= Game1.options.zoomLevel;
                    }
                    Game1.spriteBatch.Draw(Game1.lightmap, Vector2.Zero, Game1.lightmap.Bounds, Color.White, 0f, Vector2.Zero, render_zoom, SpriteEffects.None, 1f);
                    if (Game1.IsRainingHere() && (bool)Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
                    {
                        Game1.spriteBatch.Draw(Game1.staminaRect, vp.Bounds, Color.OrangeRed * 0.45f);
                    }
                    Game1.spriteBatch.End();
                }
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                events.RenderedWorld.RaiseEmpty();
                if (Game1.drawGrid)
                {
                    int startingX = -Game1.viewport.X % 64;
                    float startingY = -Game1.viewport.Y % 64;
                    for (int x = startingX; x < Game1.graphics.GraphicsDevice.Viewport.Width; x += 64)
                    {
                        Game1.spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(x, (int)startingY, 1, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Red * 0.5f);
                    }
                    for (float y = startingY; y < (float)Game1.graphics.GraphicsDevice.Viewport.Height; y += 64f)
                    {
                        Game1.spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(startingX, (int)y, Game1.graphics.GraphicsDevice.Viewport.Width, 1), Color.Red * 0.5f);
                    }
                }
                if (Game1.ShouldShowOnscreenUsernames() && Game1.currentLocation != null)
                {
                    Game1.currentLocation.DrawFarmerUsernames(Game1.spriteBatch);
                }
                if (Game1.currentBillboard != 0 && !this.takingMapScreenshot)
                {
                    this.drawBillboard();
                }
                if (!Game1.eventUp && Game1.farmEvent == null && Game1.currentBillboard == 0 && Game1.gameMode == 3 && !this.takingMapScreenshot && Game1.isOutdoorMapSmallerThanViewport())
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, -Game1.viewport.X, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64, 0, Game1.graphics.GraphicsDevice.Viewport.Width - (-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64), Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, -Game1.viewport.Y), Color.Black);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, -Game1.viewport.Y + Game1.currentLocation.map.Layers[0].LayerHeight * 64, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height - (-Game1.viewport.Y + Game1.currentLocation.map.Layers[0].LayerHeight * 64)), Color.Black);
                }
                Game1.spriteBatch.End();
                Game1.PushUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                if ((Game1.displayHUD || Game1.eventUp) && Game1.currentBillboard == 0 && Game1.gameMode == 3 && !Game1.freezeControls && !Game1.panMode && !Game1.HostPaused && !this.takingMapScreenshot)
                {
                    events.RenderingHud.RaiseEmpty();
                    this.drawHUD();
                    events.RenderedHud.RaiseEmpty();
                }
                else if (Game1.activeClickableMenu == null)
                {
                    _ = Game1.farmEvent;
                }
                if (Game1.hudMessages.Count > 0 && !this.takingMapScreenshot)
                {
                    for (int j = Game1.hudMessages.Count - 1; j >= 0; j--)
                    {
                        Game1.hudMessages[j].draw(Game1.spriteBatch, j);
                    }
                }
                Game1.spriteBatch.End();
                Game1.PopUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            if (Game1.farmEvent != null)
            {
                Game1.farmEvent.draw(Game1.spriteBatch);
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            Game1.PushUIMode();
            if (Game1.dialogueUp && !Game1.nameSelectUp && !Game1.messagePause && (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is DialogueBox)) && !this.takingMapScreenshot)
            {
                this.drawDialogueBox();
            }
            if (Game1.progressBar && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, Game1.dialogueWidth, 32), Color.LightGray);
                Game1.spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, (int)(Game1.pauseAccumulator / Game1.pauseTime * (float)Game1.dialogueWidth), 32), Color.DimGray);
            }
            Game1.spriteBatch.End();
            Game1.PopUIMode();
            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (Game1.eventUp && Game1.currentLocation != null && Game1.currentLocation.currentEvent != null)
            {
                Game1.currentLocation.currentEvent.drawAfterMap(Game1.spriteBatch);
            }
            if (!Game1.IsFakedBlackScreen() && Game1.IsRainingHere() && Game1.currentLocation != null && (bool)Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
            {
                Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Blue * 0.2f);
            }
            if ((Game1.fadeToBlack || Game1.globalFade) && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause) && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.End();
                Game1.PushUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - Game1.fadeToBlackAlpha) : Game1.fadeToBlackAlpha));
                Game1.spriteBatch.End();
                Game1.PopUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            else if (Game1.flashAlpha > 0f && !this.takingMapScreenshot)
            {
                if (Game1.options.screenFlash)
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.White * Math.Min(1f, Game1.flashAlpha));
                }
                Game1.flashAlpha -= 0.1f;
            }
            if ((Game1.messagePause || Game1.globalFade) && Game1.dialogueUp && !this.takingMapScreenshot)
            {
                this.drawDialogueBox();
            }
            if (!this.takingMapScreenshot)
            {
                foreach (TemporaryAnimatedSprite screenOverlayTempSprite in Game1.screenOverlayTempSprites)
                {
                    screenOverlayTempSprite.draw(Game1.spriteBatch, localPosition: true);
                }
                Game1.spriteBatch.End();
                Game1.PushUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                foreach (TemporaryAnimatedSprite uiOverlayTempSprite in Game1.uiOverlayTempSprites)
                {
                    uiOverlayTempSprite.draw(Game1.spriteBatch, localPosition: true);
                }
                Game1.spriteBatch.End();
                Game1.PopUIMode();
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            if (Game1.debugMode)
            {
                StringBuilder sb = Game1._debugStringBuilder;
                sb.Clear();
                if (Game1.panMode)
                {
                    sb.Append((Game1.getOldMouseX() + Game1.viewport.X) / 64);
                    sb.Append(",");
                    sb.Append((Game1.getOldMouseY() + Game1.viewport.Y) / 64);
                }
                else
                {
                    sb.Append("player: ");
                    sb.Append(Game1.player.getStandingX() / 64);
                    sb.Append(", ");
                    sb.Append(Game1.player.getStandingY() / 64);
                }
                sb.Append(" mouseTransparency: ");
                sb.Append(Game1.mouseCursorTransparency);
                sb.Append(" mousePosition: ");
                sb.Append(Game1.getMouseX());
                sb.Append(",");
                sb.Append(Game1.getMouseY());
                sb.Append(Environment.NewLine);
                sb.Append(" mouseWorldPosition: ");
                sb.Append(Game1.getMouseX() + Game1.viewport.X);
                sb.Append(",");
                sb.Append(Game1.getMouseY() + Game1.viewport.Y);
                sb.Append("  debugOutput: ");
                sb.Append(Game1.debugOutput);
                Game1.spriteBatch.DrawString(Game1.smallFont, sb, new Vector2(base.GraphicsDevice.Viewport.GetTitleSafeArea().X, base.GraphicsDevice.Viewport.GetTitleSafeArea().Y + Game1.smallFont.LineSpacing * 8), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
            }
            Game1.spriteBatch.End();
            Game1.PushUIMode();
            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (Game1.showKeyHelp && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.DrawString(Game1.smallFont, Game1.keyHelpString, new Vector2(64f, (float)(Game1.viewport.Height - 64 - (Game1.dialogueUp ? (192 + (Game1.isQuestion ? (Game1.questionChoices.Count * 64) : 0)) : 0)) - Game1.smallFont.MeasureString(Game1.keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
            }
            if (Game1.activeClickableMenu != null && !this.takingMapScreenshot)
            {
                IClickableMenu curMenu = null;
                try
                {
                    events.RenderingActiveMenu.RaiseEmpty();
                    for (curMenu = Game1.activeClickableMenu; curMenu != null; curMenu = curMenu.GetChildMenu())
                    {
                        curMenu.draw(Game1.spriteBatch);
                    }
                    events.RenderedActiveMenu.RaiseEmpty();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The {curMenu.GetMenuChainLabel()} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                    Game1.activeClickableMenu.exitThisMenu();
                }
            }
            else if (Game1.farmEvent != null)
            {
                Game1.farmEvent.drawAboveEverything(Game1.spriteBatch);
            }
            if (Game1.specialCurrencyDisplay != null)
            {
                Game1.specialCurrencyDisplay.Draw(Game1.spriteBatch);
            }
            if (Game1.emoteMenu != null && !this.takingMapScreenshot)
            {
                Game1.emoteMenu.draw(Game1.spriteBatch);
            }
            if (Game1.HostPaused && !this.takingMapScreenshot)
            {
                string msg2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                SpriteText.drawStringWithScrollBackground(Game1.spriteBatch, msg2, 96, 32);
            }
            events.Rendered.RaiseEmpty();
            Game1.spriteBatch.End();
            this.drawOverlays(Game1.spriteBatch);
            Game1.PopUIMode();
        }
#endif
#nullable enable
    }
}
