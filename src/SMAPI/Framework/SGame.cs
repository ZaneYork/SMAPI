using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;

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
        private readonly Countdown DrawCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        private readonly IReflectedField<bool> DrawActiveClickableMenuField;
        private readonly IReflectedField<string> SpriteBatchBeginNextIDField;
        private readonly IReflectedField<bool> DrawHudField;
        private readonly IReflectedField<List<Farmer>> FarmerShadowsField;
        private readonly IReflectedField<StringBuilder> DebugStringBuilderField;
        private readonly IReflectedField<BlendState> LightingBlendField;

        private readonly IReflectedMethod SpriteBatchBeginMethod;
        private readonly IReflectedMethod _spriteBatchBeginMethod;
        private readonly IReflectedMethod _spriteBatchEndMethod;
        private readonly IReflectedMethod DrawLoadingDotDotDotMethod;
        private readonly IReflectedMethod CheckToReloadGameLocationAfterDrawFailMethod;
        private readonly IReflectedMethod DrawTapToMoveTargetMethod;
        private readonly IReflectedMethod DrawDayTimeMoneyBoxMethod;
        private readonly IReflectedMethod DrawAfterMapMethod;
        private readonly IReflectedMethod DrawToolbarMethod;
        private readonly IReflectedMethod DrawVirtualJoypadMethod;
        private readonly IReflectedMethod DrawMenuMouseCursorMethod;
        private readonly IReflectedMethod DrawFadeToBlackFullScreenRectMethod;
        private readonly IReflectedMethod DrawChatBoxMethod;
        private readonly IReflectedMethod DrawDialogueBoxForPinchZoomMethod;
        private readonly IReflectedMethod DrawUnscaledActiveClickableMenuForPinchZoomMethod;
        private readonly IReflectedMethod DrawNativeScaledActiveClickableMenuForPinchZoomMethod;

        // ReSharper disable once InconsistentNaming
        private readonly IReflectedMethod DrawHUDMessagesMethod;

        // ReSharper disable once InconsistentNaming
        private readonly IReflectedMethod DrawTutorialUIMethod;
        private readonly IReflectedMethod DrawGreenPlacementBoundsMethod;

        /*********
        ** Accessors
        *********/
        /// <summary>Manages input visible to the game.</summary>
        public static SInputState Input => (SInputState)Game1.input;

        /// <summary>The game's core multiplayer utility.</summary>
        public static SMultiplayer Multiplayer => (SMultiplayer)Game1.multiplayer;

        /// <summary>The game background task which initializes a new day.</summary>
        public static Task NewDayTask => Game1._newDayTask;

        /// <summary>Construct a content manager to read game content files.</summary>
        /// <remarks>This must be static because the game accesses it before the <see cref="SGame"/> constructor is called.</remarks>
        public static Func<IServiceProvider, string, LocalizedContentManager> CreateContentManagerImpl;

        /// <summary>Raised after the game finishes loading its initial content.</summary>
        public event Action OnGameContentLoaded;

        /// <summary>Raised before the game exits.</summary>
        public event Action OnGameExiting;

        /// <summary>Raised when the game is updating its state (roughly 60 times per second).</summary>
        public event Action<GameTime, Action> OnGameUpdating;

#if SMAPI_FOR_MOBILE
        public static SGame instance;

        public bool IsGameSuspended;

        public bool IsAfterInitialize = false;
#endif

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        public SGame(Monitor monitor, Reflector reflection, EventManager eventManager, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately)
        {
            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // hook into game
            Game1.input = new SInputState();
            Game1.multiplayer = multiplayer;
            Game1.hooks = modHooks;
            Game1.locations = new ObservableCollection<GameLocation>();

#if SMAPI_FOR_MOBILE
            // init observables
            SGame.instance = this;
            this.DrawActiveClickableMenuField = this.Reflection.GetField<bool>(this, "_drawActiveClickableMenu");
            this.SpriteBatchBeginNextIDField = this.Reflection.GetField<string>(typeof(Game1), "_spriteBatchBeginNextID");
            this.DrawHudField = this.Reflection.GetField<bool>(this, "_drawHUD");
            this.FarmerShadowsField = this.Reflection.GetField<List<Farmer>>(this, "_farmerShadows");
            this.DebugStringBuilderField = this.Reflection.GetField<StringBuilder>(typeof(Game1), "_debugStringBuilder");
            this.LightingBlendField = this.Reflection.GetField<BlendState>(this, "lightingBlend");
            this.SpriteBatchBeginMethod = this.Reflection.GetMethod(this, "SpriteBatchBegin", new[] {typeof(float)});
            this._spriteBatchBeginMethod = this.Reflection.GetMethod(this, "_spriteBatchBegin", new[] {typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix)});
            this._spriteBatchEndMethod = this.Reflection.GetMethod(this, "_spriteBatchEnd", new Type[] { });
            this.DrawLoadingDotDotDotMethod = this.Reflection.GetMethod(this, "DrawLoadingDotDotDot", new[] {typeof(GameTime)});
            this.CheckToReloadGameLocationAfterDrawFailMethod = this.Reflection.GetMethod(this, "CheckToReloadGameLocationAfterDrawFail", new[] {typeof(string), typeof(Exception)});
            this.DrawTapToMoveTargetMethod = this.Reflection.GetMethod(this, "DrawTapToMoveTarget", new Type[] { });
            this.DrawDayTimeMoneyBoxMethod = this.Reflection.GetMethod(this, "DrawDayTimeMoneyBox", new Type[] { });
            this.DrawAfterMapMethod = this.Reflection.GetMethod(this, "DrawAfterMap", new Type[] { });
            this.DrawToolbarMethod = this.Reflection.GetMethod(this, "DrawToolbar", new Type[] { });
            this.DrawVirtualJoypadMethod = this.Reflection.GetMethod(this, "DrawVirtualJoypad", new Type[] { });
            this.DrawMenuMouseCursorMethod = this.Reflection.GetMethod(this, "DrawMenuMouseCursor", new Type[] { });
            this.DrawFadeToBlackFullScreenRectMethod = this.Reflection.GetMethod(this, "DrawFadeToBlackFullScreenRect", new Type[] { });
            this.DrawChatBoxMethod = this.Reflection.GetMethod(this, "DrawChatBox", new Type[] { });
            this.DrawDialogueBoxForPinchZoomMethod = this.Reflection.GetMethod(this, "DrawDialogueBoxForPinchZoom", new Type[] { });
            this.DrawUnscaledActiveClickableMenuForPinchZoomMethod = this.Reflection.GetMethod(this, "DrawUnscaledActiveClickableMenuForPinchZoom", new Type[] { });
            this.DrawNativeScaledActiveClickableMenuForPinchZoomMethod = this.Reflection.GetMethod(this, "DrawNativeScaledActiveClickableMenuForPinchZoom", new Type[] { });
            this.DrawHUDMessagesMethod = this.Reflection.GetMethod(this, "DrawHUDMessages", new Type[] { });
            this.DrawTutorialUIMethod = this.Reflection.GetMethod(this, "DrawTutorialUI", new Type[] { });
            this.DrawGreenPlacementBoundsMethod = this.Reflection.GetMethod(this, "DrawGreenPlacementBounds", new Type[] { });
#endif
            // init SMAPI
            this.Monitor = monitor;
            this.Events = eventManager;
            this.Reflection = reflection;
            this.ExitGameImmediately = exitGameImmediately;
        }

        /// <summary>Get the observable location list.</summary>
        public ObservableCollection<GameLocation> GetObservableLocations()
        {
            return (ObservableCollection<GameLocation>)Game1.locations;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Load content when the game is launched.</summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            this.OnGameContentLoaded?.Invoke();
        }

        /// <summary>Perform cleanup logic when the game exits.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event args.</param>
        /// <remarks>This overrides the logic in <see cref="Game1.exitEvent"/> to let SMAPI clean up before exit.</remarks>
        protected override void OnExiting(object sender, EventArgs args)
        {
            this.OnGameExiting?.Invoke();
        }

        /// <summary>Construct a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        protected override LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (SGame.CreateContentManagerImpl == null)
                throw new InvalidOperationException($"The {nameof(SGame)}.{nameof(SGame.CreateContentManagerImpl)} must be set.");

            return SGame.CreateContentManagerImpl(serviceProvider, rootDirectory);
        }

        /// <summary>The method called when the game is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            this.OnGameUpdating?.Invoke(gameTime, () => base.Update(gameTime));
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
#if SMAPI_FOR_MOBILE
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen, RenderTarget2D toBuffer = null)
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

                this.DrawImpl(gameTime, target_screen, toBuffer);
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
                this.Monitor.Log($"An error occured in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }

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
#endif

        /// <summary>Replicate the game's draw logic with some changes for SMAPI.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        /// <remarks>This implementation is identical to <see cref="Game1.Draw"/>, except for try..catch around menu draw code, private field references replaced by wrappers, and added events.</remarks>
//        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
//        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidImplicitNetFieldCast", Justification = "copied from game code as-is")]
#if SMAPI_FOR_MOBILE
        private void DrawImpl(GameTime gameTime, RenderTarget2D target_screen, RenderTarget2D toBuffer = null)
        {
            var events = this.Events;
            if (Game1.skipNextDrawCall)
            {
                Game1.skipNextDrawCall = false;
            }
            else
            {
                this.DrawHudField.SetValue(false);
                this.DrawActiveClickableMenuField.SetValue(false);
                Game1.showingHealthBar = false;
                if (Game1._newDayTask != null)
                {
                    if (!Game1.showInterDayScroll)
                        return;
                    this.DrawSavingDotDotDot();
                }
                else
                {
                    if (target_screen != null && toBuffer == null)
                    {
                        this.GraphicsDevice.SetRenderTarget(target_screen);
                    }

                    if (this.IsSaving)
                    {
                        this.GraphicsDevice.Clear(Game1.bgColor);
                        this.renderScreenBuffer(BlendState.Opaque, toBuffer);
                        if (Game1.activeClickableMenu != null)
                        {
                            if (Game1.IsActiveClickableMenuNativeScaled)
                            {
                                Game1.BackupViewportAndZoom(divideByZoom: true);
                                Game1.SetSpriteBatchBeginNextID("A1");
                                this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                events.Rendering.RaiseEmpty();
                                try
                                {
                                    events.RenderingActiveMenu.RaiseEmpty();
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    events.RenderedActiveMenu.RaiseEmpty();
                                }
                                catch (Exception ex)
                                {
                                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                    Game1.activeClickableMenu.exitThisMenu();
                                }

                                this._spriteBatchEndMethod.Invoke();
                                Game1.RestoreViewportAndZoom();
                            }
                            else
                            {
                                Game1.BackupViewportAndZoom();
                                Game1.SetSpriteBatchBeginNextID("A2");
                                this.SpriteBatchBeginMethod.Invoke(1f);
                                events.Rendering.RaiseEmpty();
                                try
                                {
                                    events.RenderingActiveMenu.RaiseEmpty();
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    events.RenderedActiveMenu.RaiseEmpty();
                                }
                                catch (Exception ex)
                                {
                                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                    Game1.activeClickableMenu.exitThisMenu();
                                }

                                events.Rendered.RaiseEmpty();
                                this._spriteBatchEndMethod.Invoke();
                                Game1.RestoreViewportAndZoom();
                            }
                        }

                        if (Game1.overlayMenu == null)
                            return;
                        Game1.BackupViewportAndZoom();
                        Game1.SetSpriteBatchBeginNextID("B");
                        this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        Game1.overlayMenu.draw(Game1.spriteBatch);
                        this._spriteBatchEndMethod.Invoke();
                        Game1.RestoreViewportAndZoom();
                    }
                    else
                    {
                        this.GraphicsDevice.Clear(Game1.bgColor);
                        if (Game1.activeClickableMenu != null && Game1.options.showMenuBackground && Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !this.takingMapScreenshot)
                        {
                            Matrix scale = Matrix.CreateScale(1f);
                            Game1.SetSpriteBatchBeginNextID("C");
                            this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, scale);
                            events.Rendering.RaiseEmpty();
                            try
                            {
                                Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                                events.RenderingActiveMenu.RaiseEmpty();
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                events.RenderedActiveMenu.RaiseEmpty();
                            }
                            catch (Exception ex)
                            {
                                this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                Game1.activeClickableMenu.exitThisMenu();
                            }

                            events.Rendered.RaiseEmpty();
                            this._spriteBatchEndMethod.Invoke();
                            this.drawOverlays(Game1.spriteBatch);
                            this.renderScreenBufferTargetScreen(target_screen);
                            if (Game1.overlayMenu == null)
                                return;
                            Game1.SetSpriteBatchBeginNextID("D");
                            this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                            Game1.overlayMenu.draw(Game1.spriteBatch);
                            this._spriteBatchEndMethod.Invoke();
                        }
                        else
                        {
                            if (Game1.emergencyLoading)
                            {
                                if (!Game1.SeenConcernedApeLogo)
                                {
                                    Game1.SetSpriteBatchBeginNextID("E");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (Game1.logoFadeTimer < 5000)
                                    {
                                        Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
                                    }

                                    if (Game1.logoFadeTimer > 4500)
                                    {
                                        float scale = Math.Min(1f, (Game1.logoFadeTimer - 4500) / 500f);
                                        Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * scale);
                                    }

                                    Game1.spriteBatch.Draw(
                                        Game1.titleButtonsTexture,
                                        new Vector2(Game1.viewport.Width / 2, Game1.viewport.Height / 2 - 90),
                                        new Rectangle(171 + (Game1.logoFadeTimer / 100 % 2 == 0 ? 111 : 0), 311, 111, 60),
                                        Color.White * (Game1.logoFadeTimer < 500 ? Game1.logoFadeTimer / 500f : Game1.logoFadeTimer > 4500 ? 1f - (Game1.logoFadeTimer - 4500) / 500f : 1f),
                                        0f,
                                        Vector2.Zero,
                                        3f,
                                        SpriteEffects.None,
                                        0.2f);
                                    Game1.spriteBatch.Draw(
                                        Game1.titleButtonsTexture,
                                        new Vector2(Game1.viewport.Width / 2 - 261, Game1.viewport.Height / 2 - 102),
                                        new Rectangle(Game1.logoFadeTimer / 100 % 2 == 0 ? 85 : 0, 306, 85, 69),
                                        Color.White * (Game1.logoFadeTimer < 500 ? Game1.logoFadeTimer / 500f : Game1.logoFadeTimer > 4500 ? 1f - (Game1.logoFadeTimer - 4500) / 500f : 1f),
                                        0f,
                                        Vector2.Zero,
                                        3f,
                                        SpriteEffects.None,
                                        0.2f);
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                Game1.logoFadeTimer -= gameTime.ElapsedGameTime.Milliseconds;
                            }

                            if (Game1.gameMode == Game1.errorLogMode)
                            {
                                Game1.SetSpriteBatchBeginNextID("F");
                                this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                events.Rendering.RaiseEmpty();
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 255, 0));
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.parseText(Game1.errorMessage, Game1.dialogueFont, Game1.graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
                                events.Rendered.RaiseEmpty();
                                this._spriteBatchEndMethod.Invoke();
                            }
                            else if (Game1.currentMinigame != null)
                            {
                                Game1.currentMinigame.draw(Game1.spriteBatch);
                                if (Game1.globalFade && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause))
                                {
                                    Game1.SetSpriteBatchBeginNextID("G");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds,
                                        Color.Black * (Game1.gameMode == Game1.titleScreenGameMode ? 1f - Game1.fadeToBlackAlpha : Game1.fadeToBlackAlpha));
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                this.drawOverlays(Game1.spriteBatch);
                                this.renderScreenBufferTargetScreen(target_screen);
                                if (Game1.currentMinigame is FishingGame && Game1.activeClickableMenu != null)
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-A");
                                    this.SpriteBatchBeginMethod.Invoke(1f);
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    this._spriteBatchEndMethod.Invoke();
                                    this.drawOverlays(Game1.spriteBatch);
                                }
                                else if (Game1.currentMinigame is FantasyBoardGame && Game1.activeClickableMenu != null)
                                {
                                    if (Game1.IsActiveClickableMenuNativeScaled)
                                    {
                                        Game1.BackupViewportAndZoom(true);
                                        Game1.SetSpriteBatchBeginNextID("A1");
                                        this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        this._spriteBatchEndMethod.Invoke();
                                        Game1.RestoreViewportAndZoom();
                                    }
                                    else
                                    {
                                        Game1.BackupViewportAndZoom();
                                        Game1.SetSpriteBatchBeginNextID("A2");
                                        this.SpriteBatchBeginMethod.Invoke(1f);
                                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        this._spriteBatchEndMethod.Invoke();
                                        Game1.RestoreViewportAndZoom();
                                    }
                                }

                                this.DrawVirtualJoypadMethod.Invoke();
                            }
                            else if (Game1.showingEndOfNightStuff)
                            {
                                this.renderScreenBuffer(BlendState.Opaque);
                                Game1.BackupViewportAndZoom(divideByZoom: true);
                                Game1.SetSpriteBatchBeginNextID("A-B");
                                this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                events.Rendering.RaiseEmpty();
                                if (Game1.activeClickableMenu != null)
                                {
                                    try
                                    {
                                        events.RenderingActiveMenu.RaiseEmpty();
                                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        events.RenderedActiveMenu.RaiseEmpty();
                                    }
                                    catch (Exception ex)
                                    {
                                        this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                        Game1.activeClickableMenu.exitThisMenu();
                                    }
                                }

                                events.Rendered.RaiseEmpty();
                                this._spriteBatchEndMethod.Invoke();
                                this.drawOverlays(Game1.spriteBatch);
                                Game1.RestoreViewportAndZoom();
                            }
                            else if (Game1.gameMode == Game1.loadingMode || Game1.gameMode == Game1.playingGameMode && Game1.currentLocation == null)
                            {
                                this.SpriteBatchBeginMethod.Invoke(1f);
                                events.Rendering.RaiseEmpty();
                                this._spriteBatchEndMethod.Invoke();
                                this.DrawLoadingDotDotDotMethod.Invoke(gameTime);
                                this.SpriteBatchBeginMethod.Invoke(1f);
                                events.Rendered.RaiseEmpty();
                                this._spriteBatchEndMethod.Invoke();
                                this.drawOverlays(Game1.spriteBatch);
                                this.renderScreenBufferTargetScreen(target_screen);
                                if (Game1.overlayMenu != null)
                                {
                                    Game1.SetSpriteBatchBeginNextID("H");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    Game1.overlayMenu.draw(Game1.spriteBatch);
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                //base.Draw(gameTime);
                            }
                            else
                            {
                                Rectangle rectangle;
                                byte batchOpens = 0;
                                if (Game1.gameMode == Game1.titleScreenGameMode)
                                {
                                    Game1.SetSpriteBatchBeginNextID("I");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (!Game1.drawGame)
                                {
                                    Game1.SetSpriteBatchBeginNextID("J");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (Game1.drawGame)
                                {
                                    if (Game1.drawLighting && Game1.currentLocation != null)
                                    {
                                        this.GraphicsDevice.SetRenderTarget(Game1.lightmap);
                                        this.GraphicsDevice.Clear(Color.White * 0f);
                                        Game1.SetSpriteBatchBeginNextID("K");
                                        this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, new Matrix?());
                                        if (++batchOpens == 1)
                                            events.Rendering.RaiseEmpty();
                                        Color color1 = !Game1.currentLocation.Name.StartsWith("UndergroundMine") || !(Game1.currentLocation is MineShaft)
                                            ? Game1.ambientLight.Equals(Color.White) || RainManager.Instance.isRaining && (bool) Game1.currentLocation.isOutdoors ? Game1.outdoorLight :
                                            Game1.ambientLight
                                            : ((MineShaft) Game1.currentLocation).getLightingColor(gameTime);
                                        Game1.spriteBatch.Draw(Game1.staminaRect, Game1.lightmap.Bounds, color1);
                                        foreach (LightSource currentLightSource in Game1.currentLightSources)
                                        {
                                            if (!RainManager.Instance.isRaining && !Game1.isDarkOut() || currentLightSource.lightContext.Value != LightSource.LightContext.WindowLight)
                                            {
                                                if (currentLightSource.PlayerID != 0L && currentLightSource.PlayerID != Game1.player.UniqueMultiplayerID)
                                                {
                                                    Farmer farmerMaybeOffline = Game1.getFarmerMaybeOffline(currentLightSource.PlayerID);
                                                    if (farmerMaybeOffline == null || farmerMaybeOffline.currentLocation != null && farmerMaybeOffline.currentLocation.Name != Game1.currentLocation.Name || farmerMaybeOffline.hidden)
                                                        continue;
                                                }
                                            }

                                            if (Utility.isOnScreen(currentLightSource.position, (int) (currentLightSource.radius * 64.0 * 4.0)))
                                            {
                                                Texture2D lightTexture = currentLightSource.lightTexture;
                                                Vector2 position = Game1.GlobalToLocal(Game1.viewport, currentLightSource.position) / (Game1.options.lightingQuality / 2);
                                                Rectangle? sourceRectangle = currentLightSource.lightTexture.Bounds;
                                                Color color = currentLightSource.color;
                                                Rectangle bounds = currentLightSource.lightTexture.Bounds;
                                                double x = bounds.Center.X;
                                                bounds = currentLightSource.lightTexture.Bounds;
                                                double y = bounds.Center.Y;
                                                Vector2 origin = new Vector2((float) x, (float) y);
                                                double num = (double) currentLightSource.radius / (Game1.options.lightingQuality / 2);
                                                Game1.spriteBatch.Draw(lightTexture, position, sourceRectangle, color, 0.0f, origin, (float) num, SpriteEffects.None, 0.9f);
                                            }
                                        }

                                        this._spriteBatchEndMethod.Invoke();
                                        this.GraphicsDevice.SetRenderTarget(target_screen);
                                    }

                                    if (Game1.bloomDay && Game1.bloom != null)
                                    {
                                        Game1.bloom.BeginDraw();
                                    }

                                    this.GraphicsDevice.Clear(Game1.bgColor);
                                    Game1.SetSpriteBatchBeginNextID("L");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                    events.RenderingWorld.RaiseEmpty();
                                    this.SpriteBatchBeginNextIDField.SetValue("L1");
                                    if (Game1.background != null)
                                    {
                                        Game1.background.draw(Game1.spriteBatch);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L2");
                                    Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                    this.SpriteBatchBeginNextIDField.SetValue("L3");
                                    try
                                    {
                                        if (Game1.currentLocation != null)
                                        {
                                            Game1.currentLocation.Map.GetLayer("Back").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                            this.SpriteBatchBeginNextIDField.SetValue("L4");
                                        }
                                    }
                                    catch (KeyNotFoundException exception)
                                    {
                                        this.CheckToReloadGameLocationAfterDrawFailMethod.Invoke("Back", exception);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L5");
                                    if (Game1.currentLocation != null)
                                    {
                                        Game1.currentLocation.drawWater(Game1.spriteBatch);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L6");
                                    this.FarmerShadowsField.GetValue().Clear();
                                    this.SpriteBatchBeginNextIDField.SetValue("L7");
                                    if (Game1.currentLocation != null && Game1.currentLocation.currentEvent != null && !Game1.currentLocation.currentEvent.isFestival && Game1.currentLocation.currentEvent.farmerActors.Count > 0)
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("L8");
                                        foreach (Farmer farmerActor in Game1.currentLocation.currentEvent.farmerActors)
                                        {
                                            if (farmerActor.IsLocalPlayer && Game1.displayFarmer || !farmerActor.hidden)
                                            {
                                                this.FarmerShadowsField.GetValue().Add(farmerActor);
                                            }
                                        }

                                        this.SpriteBatchBeginNextIDField.SetValue("L9");
                                    }
                                    else
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("L10");
                                        if (Game1.currentLocation != null)
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("L11");
                                            foreach (Farmer farmer in Game1.currentLocation.farmers)
                                            {
                                                if (farmer.IsLocalPlayer && Game1.displayFarmer || !farmer.hidden)
                                                {
                                                    this.FarmerShadowsField.GetValue().Add(farmer);
                                                }
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("L12");
                                        }
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L13");
                                    if (Game1.currentLocation != null && !Game1.currentLocation.shouldHideCharacters())
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("L14");
                                        if (Game1.CurrentEvent == null)
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("L15");
                                            foreach (NPC character in Game1.currentLocation.characters)
                                            {
                                                try
                                                {
                                                    if (!character.swimming)
                                                    {
                                                        if (!character.HideShadow)
                                                        {
                                                            if (!character.IsInvisible)
                                                            {
                                                                if (!Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character.getTileLocation()))
                                                                    Game1.spriteBatch.Draw(
                                                                        Game1.shadowTexture,
                                                                        Game1.GlobalToLocal(Game1.viewport, character.Position + new Vector2(character.Sprite.SpriteWidth * 4 / 2f, character.GetBoundingBox().Height + (character.IsMonster ? 0 : 12))),
                                                                        Game1.shadowTexture.Bounds,
                                                                        Color.White,
                                                                        0.0f,
                                                                        new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                                                                        (float) (4.0 + character.yJumpOffset / 40.0) * (float) character.scale,
                                                                        SpriteEffects.None,
                                                                        Math.Max(0.0f, character.getStandingY() / 10000f) - 1E-06f);
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Dictionary<string, string> dictionary1 = new Dictionary<string, string>();
                                                    if (character != null)
                                                    {
                                                        dictionary1["name"] = character.name;
                                                        dictionary1["Sprite"] = (character.Sprite != null).ToString();
                                                        Dictionary<string, string> dictionary2 = dictionary1;
                                                        character.GetBoundingBox();
                                                        bool flag = true;
                                                        string str1 = flag.ToString();
                                                        dictionary2["BoundingBox"] = str1;
                                                        Dictionary<string, string> dictionary3 = dictionary1;
                                                        flag = true;
                                                        string str2 = flag.ToString();
                                                        dictionary3["shadowTexture.Bounds"] = str2;
                                                        Dictionary<string, string> dictionary4 = dictionary1;
                                                        flag = Game1.currentLocation != null;
                                                        string str3 = flag.ToString();
                                                        dictionary4["currentLocation"] = str3;
                                                    }

                                                    Dictionary<string, string> dictionary5 = dictionary1;
                                                    Microsoft.AppCenter.Crashes.ErrorAttachmentLog[] errorAttachmentLogArray = Array.Empty<Microsoft.AppCenter.Crashes.ErrorAttachmentLog>();
                                                    Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, dictionary5, errorAttachmentLogArray);
                                                }
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("L16");
                                        }
                                        else
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("L17");
                                            foreach (NPC actor in Game1.CurrentEvent.actors)
                                            {
                                                if (!actor.swimming && !actor.HideShadow && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, actor.Position + new Vector2(actor.Sprite.SpriteWidth * 4 / 2f, actor.GetBoundingBox().Height + (!actor.IsMonster ? actor.Sprite.SpriteHeight <= 16 ? -4 : 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + actor.yJumpOffset / 40f) * (float) actor.scale, SpriteEffects.None, Math.Max(0f, actor.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("L18");
                                        }

                                        this.SpriteBatchBeginNextIDField.SetValue("L19");
                                        foreach (Farmer farmerShadow in this.FarmerShadowsField.GetValue())
                                        {
                                            if (!Game1.multiplayer.isDisconnecting(farmerShadow.UniqueMultiplayerID) &&
                                                !farmerShadow.swimming &&
                                                !farmerShadow.isRidingHorse() &&
                                                (Game1.currentLocation == null || !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation())))
                                            {
                                                Game1.spriteBatch.Draw(
                                                    Game1.shadowTexture,
                                                    Game1.GlobalToLocal(farmerShadow.Position + new Vector2(32f, 24f)),
                                                    Game1.shadowTexture.Bounds,
                                                    Color.White,
                                                    0.0f,
                                                    new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (float) (4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5)),
                                                    SpriteEffects.None,
                                                    0.0f);
                                            }
                                        }

                                        this.SpriteBatchBeginNextIDField.SetValue("L20");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L21");
                                    try
                                    {
                                        if (Game1.currentLocation != null)
                                        {
                                            Game1.currentLocation.Map.GetLayer("Buildings").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        }
                                    }
                                    catch (KeyNotFoundException exception2)
                                    {
                                        this.CheckToReloadGameLocationAfterDrawFailMethod.Invoke("Buildings", exception2);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L22");
                                    Game1.mapDisplayDevice.EndScene();
                                    this.SpriteBatchBeginNextIDField.SetValue("L23");
                                    if (Game1.currentLocation != null && Game1.currentLocation.tapToMove.targetNPC != null)
                                    {
                                        Game1.spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.currentLocation.tapToMove.targetNPC.Position + new Vector2(Game1.currentLocation.tapToMove.targetNPC.Sprite.SpriteWidth * 4 / 2f - 32f, Game1.currentLocation.tapToMove.targetNPC.GetBoundingBox().Height + (!Game1.currentLocation.tapToMove.targetNPC.IsMonster ? 12 : 0) - 32)), new Rectangle(194, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.58f);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("L24");
                                    this._spriteBatchEndMethod.Invoke();
                                    this.SpriteBatchBeginNextIDField.SetValue("L25");
                                    Game1.SetSpriteBatchBeginNextID("M");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    this.SpriteBatchBeginNextIDField.SetValue("M1");
                                    if (Game1.currentLocation != null && !Game1.currentLocation.shouldHideCharacters())
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M2");
                                        if (Game1.CurrentEvent == null)
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("M3");
                                            foreach (NPC character2 in Game1.currentLocation.characters)
                                            {
                                                if (!character2.swimming && !character2.HideShadow && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character2.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(
                                                        Game1.shadowTexture,
                                                        Game1.GlobalToLocal(Game1.viewport, character2.Position + new Vector2(character2.Sprite.SpriteWidth * 4 / 2f, character2.GetBoundingBox().Height + (!character2.IsMonster ? 12 : 0))),
                                                        Game1.shadowTexture.Bounds,
                                                        Color.White,
                                                        0f,
                                                        new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                                                        (4f + character2.yJumpOffset / 40f) * (float) character2.scale, SpriteEffects.None,
                                                        Math.Max(0f, character2.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("M4");
                                        }
                                        else
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("M5");
                                            foreach (NPC actor2 in Game1.CurrentEvent.actors)
                                            {
                                                if (!actor2.swimming && !actor2.HideShadow && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor2.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(
                                                        Game1.shadowTexture,
                                                        Game1.GlobalToLocal(Game1.viewport, actor2.Position + new Vector2(actor2.Sprite.SpriteWidth * 4 / 2f, actor2.GetBoundingBox().Height + (!actor2.IsMonster ? 12 : 0))),
                                                        Game1.shadowTexture.Bounds,
                                                        Color.White,
                                                        0f,
                                                        new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                                                        (4f + actor2.yJumpOffset / 40f) * (float) actor2.scale,
                                                        SpriteEffects.None,
                                                        Math.Max(0f, actor2.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("M6");
                                        }

                                        foreach (Farmer farmerShadow in this.FarmerShadowsField.GetValue())
                                        {
                                            this.SpriteBatchBeginNextIDField.SetValue("M7");
                                            float layerDepth = System.Math.Max(0.0001f, farmerShadow.getDrawLayer() + 0.00011f) - 0.0001f;
                                            if (!farmerShadow.swimming && !farmerShadow.isRidingHorse() && Game1.currentLocation != null && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation()))
                                            {
                                                Game1.spriteBatch.Draw(
                                                    Game1.shadowTexture,
                                                    Game1.GlobalToLocal(farmerShadow.Position + new Vector2(32f, 24f)),
                                                    Game1.shadowTexture.Bounds,
                                                    Color.White,
                                                    0.0f,
                                                    new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                                                    (float) (4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5)),
                                                    SpriteEffects.None,
                                                    layerDepth);
                                            }

                                            this.SpriteBatchBeginNextIDField.SetValue("M8");
                                        }
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M9");
                                    if ((Game1.eventUp || Game1.killScreen) && !Game1.killScreen && Game1.currentLocation?.currentEvent != null)
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M10");
                                        Game1.currentLocation.currentEvent.draw(Game1.spriteBatch);
                                        this.SpriteBatchBeginNextIDField.SetValue("M11");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M12");
                                    if (Game1.currentLocation != null && Game1.player.currentUpgrade != null && Game1.player.currentUpgrade.daysLeftTillUpgradeDone <= 3 && Game1.currentLocation.Name.Equals("Farm"))
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M13");
                                        Game1.spriteBatch.Draw(
                                            Game1.player.currentUpgrade.workerTexture,
                                            Game1.GlobalToLocal(Game1.viewport, Game1.player.currentUpgrade.positionOfCarpenter),
                                            Game1.player.currentUpgrade.getSourceRectangle(),
                                            Color.White,
                                            0f,
                                            Vector2.Zero,
                                            1f,
                                            SpriteEffects.None,
                                            (Game1.player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                                        this.SpriteBatchBeginNextIDField.SetValue("M14");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M15");
                                    Game1.currentLocation?.draw(Game1.spriteBatch);
                                    foreach (Vector2 key in Game1.crabPotOverlayTiles.Keys)
                                    {
                                        Tile tile = Game1.currentLocation.Map.GetLayer("Buildings").Tiles[(int) key.X, (int) key.Y];
                                        if (tile != null)
                                        {
                                            Vector2 local = Game1.GlobalToLocal(Game1.viewport, key * 64f);
                                            Location location = new Location((int) local.X, (int) local.Y);
                                            Game1.mapDisplayDevice.DrawTile(tile, location, (float) (((double) key.Y * 64.0 - 1.0) / 10000.0));
                                        }
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M16");
                                    if (Game1.player.ActiveObject == null && (Game1.player.UsingTool || Game1.pickingTool) && Game1.player.CurrentTool != null && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool))
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M17");
                                        Game1.drawTool(Game1.player);
                                        this.SpriteBatchBeginNextIDField.SetValue("M18");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M19");
                                    if (Game1.currentLocation != null && Game1.currentLocation.Name.Equals("Farm"))
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M20");
                                        this.drawFarmBuildings();
                                        this.SpriteBatchBeginNextIDField.SetValue("M21");
                                    }

                                    if (Game1.tvStation >= 0)
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M22");
                                        Game1.spriteBatch.Draw(
                                            Game1.tvStationTexture,
                                            Game1.GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)),
                                            new Rectangle(Game1.tvStation * 24, 0, 24, 15),
                                            Color.White,
                                            0f,
                                            Vector2.Zero,
                                            4f,
                                            SpriteEffects.None,
                                            1E-08f);
                                        this.SpriteBatchBeginNextIDField.SetValue("M23");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M24");
                                    if (Game1.panMode)
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M25");
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle((int) Math.Floor((Game1.getOldMouseX() + Game1.viewport.X) / 64.0) * 64 - Game1.viewport.X, (int) Math.Floor((Game1.getOldMouseY() + Game1.viewport.Y) / 64.0) * 64 - Game1.viewport.Y, 64, 64), Color.Lime * 0.75f);
                                        this.SpriteBatchBeginNextIDField.SetValue("M26");
                                        foreach (Warp warp in Game1.currentLocation?.warps)
                                        {
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(warp.X * 64 - Game1.viewport.X, warp.Y * 64 - Game1.viewport.Y, 64, 64), Color.Red * 0.75f);
                                        }

                                        this.SpriteBatchBeginNextIDField.SetValue("M27");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M28");
                                    Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                    this.SpriteBatchBeginNextIDField.SetValue("M29");
                                    try
                                    {
                                        Game1.currentLocation?.Map.GetLayer("Front").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        this.SpriteBatchBeginNextIDField.SetValue("M30");
                                    }
                                    catch (KeyNotFoundException exception3)
                                    {
                                        this.CheckToReloadGameLocationAfterDrawFailMethod.Invoke("Front", exception3);
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M31");
                                    Game1.mapDisplayDevice.EndScene();
                                    this.SpriteBatchBeginNextIDField.SetValue("M32");
                                    Game1.currentLocation?.drawAboveFrontLayer(Game1.spriteBatch);
                                    this.SpriteBatchBeginNextIDField.SetValue("M33");
                                    if (Game1.currentLocation != null &&
                                        Game1.currentLocation.tapToMove.targetNPC == null &&
                                        (Game1.displayHUD || Game1.eventUp) &&
                                        Game1.currentBillboard == 0 &&
                                        Game1.gameMode == Game1.playingGameMode &&
                                        !Game1.freezeControls &&
                                        !Game1.panMode &&
                                        !Game1.HostPaused)
                                    {
                                        this.SpriteBatchBeginNextIDField.SetValue("M34");
                                        this.DrawTapToMoveTargetMethod.Invoke();
                                        this.SpriteBatchBeginNextIDField.SetValue("M35");
                                    }

                                    this.SpriteBatchBeginNextIDField.SetValue("M36");
                                    this._spriteBatchEndMethod.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("N");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (Game1.currentLocation != null &&
                                        Game1.displayFarmer &&
                                        Game1.player.ActiveObject != null &&
                                        (bool) Game1.player.ActiveObject.bigCraftable &&
                                        this.checkBigCraftableBoundariesForFrontLayer() &&
                                        Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)
                                    {
                                        Game1.drawPlayerHeldObject(Game1.player);
                                    }
                                    else if (Game1.displayFarmer && Game1.player.ActiveObject != null)
                                    {
                                        if (Game1.currentLocation != null && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int) Game1.player.Position.X, (int) Game1.player.Position.Y - 38), Game1.viewport.Size) == null || Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int) Game1.player.Position.X, (int) Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways"))
                                        {
                                            Layer layer1 = Game1.currentLocation.Map.GetLayer("Front");
                                            rectangle = Game1.player.GetBoundingBox();
                                            Location mapDisplayLocation1 = new Location(rectangle.Right, (int) Game1.player.Position.Y - 38);
                                            Size size1 = Game1.viewport.Size;
                                            if (layer1.PickTile(mapDisplayLocation1, size1) != null)
                                            {
                                                Layer layer2 = Game1.currentLocation.Map.GetLayer("Front");
                                                rectangle = Game1.player.GetBoundingBox();
                                                Location mapDisplayLocation2 = new Location(rectangle.Right, (int) Game1.player.Position.Y - 38);
                                                Size size2 = Game1.viewport.Size;
                                                if (layer2.PickTile(mapDisplayLocation2, size2).TileIndexProperties.ContainsKey("FrontAlways"))
                                                    goto label_183;
                                            }
                                            else
                                                goto label_183;
                                        }

                                        Game1.drawPlayerHeldObject(Game1.player);
                                    }

                                    label_183:
                                    if (Game1.currentLocation != null
                                        && (Game1.player.UsingTool || Game1.pickingTool)
                                        && Game1.player.CurrentTool != null
                                        && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool)
                                        && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), (int) Game1.player.Position.Y - 38), Game1.viewport.Size) != null && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)
                                        Game1.drawTool(Game1.player);
                                    if (Game1.currentLocation != null && Game1.currentLocation.Map.GetLayer("AlwaysFront") != null)
                                    {
                                        Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                        try
                                        {
                                            Game1.currentLocation.Map.GetLayer("AlwaysFront").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        }
                                        catch (KeyNotFoundException exception4)
                                        {
                                            this.CheckToReloadGameLocationAfterDrawFailMethod.Invoke("AlwaysFront", exception4);
                                        }

                                        Game1.mapDisplayDevice.EndScene();
                                    }

                                    if (Game1.toolHold > 400f && Game1.player.CurrentTool.UpgradeLevel >= 1 && Game1.player.canReleaseTool)
                                    {
                                        Color color = Color.White;
                                        switch ((int) ((double) Game1.toolHold / 600.0) + 2)
                                        {
                                            case 1:
                                                color = Tool.copperColor;
                                                break;
                                            case 2:
                                                color = Tool.steelColor;
                                                break;
                                            case 3:
                                                color = Tool.goldColor;
                                                break;
                                            case 4:
                                                color = Tool.iridiumColor;
                                                break;
                                        }

                                        Game1.spriteBatch.Draw(Game1.littleEffect, new Rectangle((int) Game1.player.getLocalPosition(Game1.viewport).X - 2, (int) Game1.player.getLocalPosition(Game1.viewport).Y - (!Game1.player.CurrentTool.Name.Equals("Watering Can") ? 64 : 0) - 2, (int) (Game1.toolHold % 600f * 0.08f) + 4, 12), Color.Black);
                                        Game1.spriteBatch.Draw(Game1.littleEffect, new Rectangle((int) Game1.player.getLocalPosition(Game1.viewport).X, (int) Game1.player.getLocalPosition(Game1.viewport).Y - (!Game1.player.CurrentTool.Name.Equals("Watering Can") ? 64 : 0), (int) (Game1.toolHold % 600f * 0.08f), 8), color);
                                    }

                                    this.drawWeather(gameTime, target_screen);
                                    if (Game1.farmEvent != null)
                                    {
                                        Game1.farmEvent.draw(Game1.spriteBatch);
                                    }

                                    if (Game1.currentLocation != null && Game1.currentLocation.LightLevel > 0f && Game1.timeOfDay < 2000)
                                    {
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * Game1.currentLocation.LightLevel);
                                    }

                                    if (Game1.screenGlow)
                                    {
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Game1.screenGlowColor * Game1.screenGlowAlpha);
                                    }

                                    Game1.currentLocation?.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                                    if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is FishingRod && ((Game1.player.CurrentTool as FishingRod).isTimingCast || (Game1.player.CurrentTool as FishingRod).castingChosenCountdown > 0f || (Game1.player.CurrentTool as FishingRod).fishCaught || (Game1.player.CurrentTool as FishingRod).showingTreasure))
                                    {
                                        Game1.player.CurrentTool.draw(Game1.spriteBatch);
                                    }

                                    this._spriteBatchEndMethod.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("O");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (Game1.eventUp && Game1.currentLocation != null && Game1.currentLocation.currentEvent != null)
                                    {
                                        Game1.currentLocation.currentEvent.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                                        foreach (NPC actor in Game1.currentLocation.currentEvent.actors)
                                        {
                                            if (actor.isEmoting)
                                            {
                                                Vector2 localPosition = actor.getLocalPosition(Game1.viewport);
                                                localPosition.Y -= 140f;
                                                if (actor.Age == 2)
                                                {
                                                    localPosition.Y += 32f;
                                                }
                                                else if (actor.Gender == 1)
                                                {
                                                    localPosition.Y += 10f;
                                                }

                                                Game1.spriteBatch.Draw(Game1.emoteSpriteSheet, localPosition, new Rectangle(actor.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, actor.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, actor.getStandingY() / 10000f);
                                            }
                                        }
                                    }

                                    this._spriteBatchEndMethod.Invoke();
                                    if (Game1.drawLighting)
                                    {
                                        Game1.SetSpriteBatchBeginNextID("P");
                                        this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, this.LightingBlendField.GetValue(), SamplerState.LinearClamp, null, null, null, new Matrix?());
                                        Game1.spriteBatch.Draw(Game1.lightmap, Vector2.Zero, Game1.lightmap.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.lightingQuality / 2, SpriteEffects.None, 1f);
                                        if (RainManager.Instance.isRaining && Game1.currentLocation != null && (bool) Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
                                        {
                                            Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.OrangeRed * 0.45f);
                                        }

                                        this._spriteBatchEndMethod.Invoke();
                                    }

                                    Game1.SetSpriteBatchBeginNextID("Q");
                                    this._spriteBatchBeginMethod.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    events.RenderedWorld.RaiseEmpty();
                                    if (Game1.drawGrid)
                                    {
                                        int num = -Game1.viewport.X % 64;
                                        float num2 = -Game1.viewport.Y % 64;
                                        int num3 = num;
                                        while (true)
                                        {
                                            int num4 = num3;
                                            int width = Game1.graphics.GraphicsDevice.Viewport.Width;
                                            if (num4 < width)
                                            {
                                                int x = num3;
                                                int y = (int) num2;
                                                int height = Game1.graphics.GraphicsDevice.Viewport.Height;
                                                Rectangle destinationRectangle = new Rectangle(x, y, 1, height);
                                                Color color = Color.Red * 0.5f;
                                                Game1.spriteBatch.Draw(Game1.staminaRect, destinationRectangle, color);
                                                num3 += 64;
                                            }
                                            else
                                                break;
                                        }

                                        float num5 = num2;
                                        while (true)
                                        {
                                            double num4 = num5;
                                            double height = Game1.graphics.GraphicsDevice.Viewport.Height;
                                            if (num4 < height)
                                            {
                                                int x = num;
                                                int y = (int) num5;
                                                int width = Game1.graphics.GraphicsDevice.Viewport.Width;
                                                Rectangle destinationRectangle = new Rectangle(x, y, width, 1);
                                                Color color = Color.Red * 0.5f;
                                                Game1.spriteBatch.Draw(Game1.staminaRect, destinationRectangle, color);
                                                num5 += 64f;
                                            }
                                            else
                                                break;
                                        }
                                    }

                                    if (Game1.currentBillboard != 0 && !this.takingMapScreenshot)
                                        this.drawBillboard();
                                    if ((Game1.displayHUD || Game1.eventUp)
                                        && Game1.currentBillboard == 0
                                        && Game1.gameMode == Game1.playingGameMode
                                        && !Game1.freezeControls
                                        && !Game1.panMode
                                        && !Game1.HostPaused)
                                    {
                                        if (Game1.currentLocation != null
                                            && !Game1.eventUp
                                            && Game1.farmEvent == null
//                                            && Game1.currentBillboard == 0
//                                            && Game1.gameMode == Game1.playingGameMode
                                            && !this.takingMapScreenshot
                                            && Game1.isOutdoorMapSmallerThanViewport())
                                        {
                                            int width1 = -Math.Min(Game1.viewport.X, 4096);
                                            int height1 = Game1.graphics.GraphicsDevice.Viewport.Height;
                                            Rectangle destinationRectangle1 = new Rectangle(0, 0, width1, height1);
                                            Color black1 = Color.Black;
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, destinationRectangle1, black1);
                                            int x = -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64;
                                            int width2 = Math.Min(4096, Game1.graphics.GraphicsDevice.Viewport.Width - (-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64));
                                            int height2 = Game1.graphics.GraphicsDevice.Viewport.Height;
                                            Rectangle destinationRectangle2 = new Rectangle(x, 0, width2, height2);
                                            Color black2 = Color.Black;
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, destinationRectangle2, black2);
                                        }

                                        this.DrawHudField.SetValue(false);
                                        if ((Game1.displayHUD || Game1.eventUp) && Game1.currentBillboard == 0 && Game1.gameMode == 3 && !Game1.freezeControls && !Game1.panMode && !Game1.HostPaused && !this.takingMapScreenshot) this.DrawHudField.SetValue(true);
                                        this.DrawGreenPlacementBoundsMethod.Invoke();
                                    }
                                }

                                if (Game1.farmEvent != null)
                                {
                                    Game1.farmEvent.draw(Game1.spriteBatch);
                                }

                                if (Game1.dialogueUp && !Game1.nameSelectUp && !Game1.messagePause && (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is DialogueBox)))
                                {
                                    this.drawDialogueBox();
                                }

                                if (Game1.progressBar && !this.takingMapScreenshot)
                                {
                                    int x1 = (Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2;
                                    rectangle = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
                                    int y1 = rectangle.Bottom - 128;
                                    Rectangle destinationRectangle1 = new Rectangle(x1, y1, Game1.dialogueWidth, 32);
                                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, destinationRectangle1, Color.LightGray);
                                    int x2 = (Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2;
                                    rectangle = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
                                    int y2 = rectangle.Bottom - 128;
                                    int width = (int) (Game1.pauseAccumulator / (double) Game1.pauseTime * Game1.dialogueWidth);
                                    Rectangle destinationRectangle2 = new Rectangle(x2, y2, width, 32);
                                    Game1.spriteBatch.Draw(Game1.staminaRect, destinationRectangle2, Color.DimGray);
                                }

                                if (RainManager.Instance.isRaining && Game1.currentLocation != null && (bool) Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
                                {
                                    Rectangle bounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
                                    Color color = Color.Blue * 0.2f;
                                    Game1.spriteBatch.Draw(Game1.staminaRect, bounds, color);
                                }

                                if ((Game1.messagePause || Game1.globalFade) && Game1.dialogueUp && !this.takingMapScreenshot)
                                {
                                    this.drawDialogueBox();
                                }

                                if (!this.takingMapScreenshot)
                                {
                                    foreach (TemporaryAnimatedSprite overlayTempSprite in Game1.screenOverlayTempSprites)
                                    {
                                        overlayTempSprite.draw(Game1.spriteBatch, localPosition: true);
                                    }
                                }

                                if (Game1.debugMode)
                                {
                                    StringBuilder debugStringBuilder = this.DebugStringBuilderField.GetValue();
                                    debugStringBuilder.Clear();
                                    if (Game1.panMode)
                                    {
                                        debugStringBuilder.Append((Game1.getOldMouseX() + Game1.viewport.X) / 64);
                                        debugStringBuilder.Append(",");
                                        debugStringBuilder.Append((Game1.getOldMouseY() + Game1.viewport.Y) / 64);
                                    }
                                    else
                                    {
                                        debugStringBuilder.Append("player: ");
                                        debugStringBuilder.Append(Game1.player.getStandingX() / 64);
                                        debugStringBuilder.Append(", ");
                                        debugStringBuilder.Append(Game1.player.getStandingY() / 64);
                                    }

                                    debugStringBuilder.Append(" mouseTransparency: ");
                                    debugStringBuilder.Append(Game1.mouseCursorTransparency);
                                    debugStringBuilder.Append(" mousePosition: ");
                                    debugStringBuilder.Append(Game1.getMouseX());
                                    debugStringBuilder.Append(",");
                                    debugStringBuilder.Append(Game1.getMouseY());
                                    debugStringBuilder.Append(Environment.NewLine);
                                    debugStringBuilder.Append(" mouseWorldPosition: ");
                                    debugStringBuilder.Append(Game1.getMouseX() + Game1.viewport.X);
                                    debugStringBuilder.Append(",");
                                    debugStringBuilder.Append(Game1.getMouseY() + Game1.viewport.Y);
                                    debugStringBuilder.Append("debugOutput: ");
                                    debugStringBuilder.Append(Game1.debugOutput);
                                    Game1.spriteBatch.DrawString(Game1.smallFont, debugStringBuilder, new Vector2(this.GraphicsDevice.Viewport.GetTitleSafeArea().X, this.GraphicsDevice.Viewport.GetTitleSafeArea().Y + Game1.smallFont.LineSpacing * 8), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.09999999f);
                                }

                                if (Game1.showKeyHelp && !this.takingMapScreenshot)
                                {
                                    Game1.spriteBatch.DrawString(Game1.smallFont, Game1.keyHelpString, new Vector2(64f, Game1.viewport.Height - 64 - (Game1.dialogueUp ? 192 + (Game1.isQuestion ? Game1.questionChoices.Count * 64 : 0) : 0) - Game1.smallFont.MeasureString(Game1.keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
                                }

                                if (Game1.activeClickableMenu != null)
                                {
                                    this.DrawActiveClickableMenuField.SetValue(true);
                                    if (Game1.activeClickableMenu is CarpenterMenu)
                                    {
                                        ((CarpenterMenu) Game1.activeClickableMenu).DrawPlacementSquares(Game1.spriteBatch);
                                    }
                                    else if (Game1.activeClickableMenu is MuseumMenu)
                                    {
                                        ((MuseumMenu) Game1.activeClickableMenu).DrawPlacementGrid(Game1.spriteBatch);
                                    }

                                    if (!Game1.IsActiveClickableMenuUnscaled && !Game1.IsActiveClickableMenuNativeScaled)
                                    {
                                        try
                                        {

                                            events.RenderingActiveMenu.RaiseEmpty();
                                            Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                            events.RenderedActiveMenu.RaiseEmpty();
                                        }
                                        catch (Exception ex)
                                        {
                                            this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                            Game1.activeClickableMenu.exitThisMenu();
                                        }
                                    }

                                }
                                else if (Game1.farmEvent != null)
                                {
                                    Game1.farmEvent.drawAboveEverything(Game1.spriteBatch);
                                }

                                if (Game1.emoteMenu != null && !this.takingMapScreenshot)
                                    Game1.emoteMenu.draw(Game1.spriteBatch);
                                if (Game1.HostPaused)
                                {
                                    string s = Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                                    SpriteText.drawStringWithScrollCenteredAt(Game1.spriteBatch, s, 96, 32);
                                }

                                this._spriteBatchEndMethod.Invoke();
                                this.drawOverlays(Game1.spriteBatch, false);
                                this.renderScreenBuffer(BlendState.Opaque, toBuffer);
                                if (this.DrawHudField.GetValue())
                                {
                                    this.DrawDayTimeMoneyBoxMethod.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("A-C");
                                    this.SpriteBatchBeginMethod.Invoke(1f);
                                    events.RenderingHud.RaiseEmpty();
                                    this.DrawHUD();
                                    events.RenderedHud.RaiseEmpty();
                                    if (Game1.currentLocation != null && !(Game1.activeClickableMenu is GameMenu) && !(Game1.activeClickableMenu is QuestLog))
                                        Game1.currentLocation.drawAboveAlwaysFrontLayerText(Game1.spriteBatch);

                                    this.DrawAfterMapMethod.Invoke();
                                    this._spriteBatchEndMethod.Invoke();
                                    if (TutorialManager.Instance != null)
                                    {
                                        Game1.SetSpriteBatchBeginNextID("A-D");
                                        this.SpriteBatchBeginMethod.Invoke(Game1.options.zoomLevel);
                                        TutorialManager.Instance.draw(Game1.spriteBatch);
                                        this._spriteBatchEndMethod.Invoke();
                                    }

                                    this.DrawToolbarMethod.Invoke();
                                    this.DrawMenuMouseCursorMethod.Invoke();
                                }

                                if (this.DrawHudField.GetValue() || Game1.player.CanMove) this.DrawVirtualJoypadMethod.Invoke();
                                this.DrawFadeToBlackFullScreenRectMethod.Invoke();
                                Game1.SetSpriteBatchBeginNextID("A-E");
                                this.SpriteBatchBeginMethod.Invoke(1f);
                                this.DrawChatBoxMethod.Invoke();
                                this._spriteBatchEndMethod.Invoke();
                                if (this.DrawActiveClickableMenuField.GetValue())
                                {
                                    try
                                    {
                                        if (Game1.activeClickableMenu is DialogueBox)
                                        {
                                            Game1.BackupViewportAndZoom(true);
                                            this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                            events.RenderingActiveMenu.RaiseEmpty();
                                            this._spriteBatchEndMethod.Invoke();
                                            Game1.RestoreViewportAndZoom();

                                            this.DrawDialogueBoxForPinchZoomMethod.Invoke();

                                            Game1.BackupViewportAndZoom(true);
                                            this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                            events.RenderedActiveMenu.RaiseEmpty();
                                            this._spriteBatchEndMethod.Invoke();
                                            Game1.RestoreViewportAndZoom();
                                        }
                                        if (Game1.IsActiveClickableMenuUnscaled && !(Game1.activeClickableMenu is DialogueBox))
                                        {
                                                Game1.BackupViewportAndZoom();
                                                this.SpriteBatchBeginMethod.Invoke(1f);
                                                events.RenderingActiveMenu.RaiseEmpty();
                                                this._spriteBatchEndMethod.Invoke();
                                                Game1.RestoreViewportAndZoom();

                                                this.DrawUnscaledActiveClickableMenuForPinchZoomMethod.Invoke();

                                                Game1.BackupViewportAndZoom();
                                                this.SpriteBatchBeginMethod.Invoke(1f);
                                                events.RenderedActiveMenu.RaiseEmpty();
                                                this._spriteBatchEndMethod.Invoke();
                                                Game1.RestoreViewportAndZoom();
                                        }
                                        if (Game1.IsActiveClickableMenuNativeScaled)
                                        {
                                            Game1.BackupViewportAndZoom(true);
                                            this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                            events.RenderingActiveMenu.RaiseEmpty();
                                            this._spriteBatchEndMethod.Invoke();
                                            Game1.RestoreViewportAndZoom();

                                            this.DrawNativeScaledActiveClickableMenuForPinchZoomMethod.Invoke();

                                            Game1.BackupViewportAndZoom(true);
                                            this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                            events.RenderedActiveMenu.RaiseEmpty();
                                            this._spriteBatchEndMethod.Invoke();
                                            Game1.RestoreViewportAndZoom();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                        Game1.activeClickableMenu.exitThisMenu();
                                    }

                                    if (Game1.IsActiveClickableMenuNativeScaled)
                                        this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                    else
                                        this.SpriteBatchBeginMethod.Invoke(Game1.options.zoomLevel);
                                    events.Rendered.RaiseEmpty();
                                    this._spriteBatchEndMethod.Invoke();
                                }
                                else
                                {
                                    this.SpriteBatchBeginMethod.Invoke(Game1.options.zoomLevel);
                                    events.Rendered.RaiseEmpty();
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                if (this.DrawHudField.GetValue() && Game1.hudMessages.Count > 0 && (!Game1.eventUp || Game1.isFestival()))
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-F");
                                    this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                    this.DrawHUDMessagesMethod.Invoke();
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                if (Game1.CurrentEvent != null && Game1.CurrentEvent.skippable && !Game1.CurrentEvent.skipped && (Game1.activeClickableMenu == null || Game1.activeClickableMenu != null && !(Game1.activeClickableMenu is MenuWithInventory)))
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-G");
                                    this.SpriteBatchBeginMethod.Invoke(Game1.NativeZoomLevel);
                                    Game1.CurrentEvent.DrawSkipButton(Game1.spriteBatch);
                                    this._spriteBatchEndMethod.Invoke();
                                }

                                this.DrawTutorialUIMethod.Invoke();
                            }
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
            if (Game1._newDayTask != null)
            {
                base.GraphicsDevice.Clear(Game1.bgColor);
                return;
            }
            if (target_screen != null)
            {
                base.GraphicsDevice.SetRenderTarget(target_screen);
            }
            if (this.IsSaving)
            {
                base.GraphicsDevice.Clear(Game1.bgColor);
                IClickableMenu menu = Game1.activeClickableMenu;
                if (menu != null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
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
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.overlayMenu.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                }
                this.renderScreenBuffer(target_screen);
                return;
            }
            base.GraphicsDevice.Clear(Game1.bgColor);
            if (Game1.activeClickableMenu != null && Game1.options.showMenuBackground && Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                events.Rendering.RaiseEmpty();
                try
                {
                    Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                    events.RenderingActiveMenu.RaiseEmpty();
                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                    events.RenderedActiveMenu.RaiseEmpty();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                    Game1.activeClickableMenu.exitThisMenu();
                }
                events.Rendered.RaiseEmpty();
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                if (target_screen != null)
                {
                    base.GraphicsDevice.SetRenderTarget(null);
                    base.GraphicsDevice.Clear(Game1.bgColor);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                    Game1.spriteBatch.Draw(target_screen, Vector2.Zero, target_screen.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                    Game1.spriteBatch.End();
                }
                if (Game1.overlayMenu != null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.overlayMenu.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                }
                return;
            }
            if (Game1.gameMode == 11)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
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
                bool batchEnded = false;

                if (events.Rendering.HasListeners())
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    events.Rendering.RaiseEmpty();
                    Game1.spriteBatch.End();
                }

                Game1.currentMinigame.draw(Game1.spriteBatch);
                if (Game1.globalFade && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause))
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - Game1.fadeToBlackAlpha) : Game1.fadeToBlackAlpha));
                    Game1.spriteBatch.End();
                }
                this.drawOverlays(Game1.spriteBatch);
                if (target_screen != null)
                {
                    base.GraphicsDevice.SetRenderTarget(null);
                    base.GraphicsDevice.Clear(Game1.bgColor);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                    Game1.spriteBatch.Draw(target_screen, Vector2.Zero, target_screen.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                    events.Rendered.RaiseEmpty();
                    batchEnded = true;
                    Game1.spriteBatch.End();
                }
                else
                {
                    if (!batchEnded && events.Rendered.HasListeners())
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                    }
                }
                return;
            }
            if (Game1.showingEndOfNightStuff)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                events.Rendering.RaiseEmpty();
                if (Game1.activeClickableMenu != null)
                {
                    try
                    {
                        events.RenderingActiveMenu.RaiseEmpty();
                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                        events.RenderedActiveMenu.RaiseEmpty();
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                        Game1.activeClickableMenu.exitThisMenu();
                    }
                }
                events.Rendered.RaiseEmpty();
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                if (target_screen != null)
                {
                    base.GraphicsDevice.SetRenderTarget(null);
                    base.GraphicsDevice.Clear(Game1.bgColor);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                    Game1.spriteBatch.Draw(target_screen, Vector2.Zero, target_screen.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                    Game1.spriteBatch.End();
                }
                return;
            }
            if (Game1.gameMode == 6 || (Game1.gameMode == 3 && Game1.currentLocation == null))
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                events.Rendering.RaiseEmpty();
                string addOn = "";
                for (int i = 0; (double)i < gameTime.TotalGameTime.TotalMilliseconds % 999.0 / 333.0; i++)
                {
                    addOn += ".";
                }
                string str = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3688");
                string msg = str + addOn;
                string largestMessage = str + "... ";
                int msgw = SpriteText.getWidthOfString(largestMessage);
                int msgh = 64;
                int msgx = 64;
                int msgy = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - msgh;
                SpriteText.drawString(Game1.spriteBatch, msg, msgx, msgy, 999999, msgw, msgh, 1f, 0.88f, junimoText: false, 0, largestMessage);
                events.Rendered.RaiseEmpty();
                Game1.spriteBatch.End();
                this.drawOverlays(Game1.spriteBatch);
                if (target_screen != null)
                {
                    base.GraphicsDevice.SetRenderTarget(null);
                    base.GraphicsDevice.Clear(Game1.bgColor);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                    Game1.spriteBatch.Draw(target_screen, Vector2.Zero, target_screen.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                    Game1.spriteBatch.End();
                }
                if (Game1.overlayMenu != null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.overlayMenu.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                }
                //base.Draw(gameTime);
                return;
            }
            byte batchOpens = 0; // used for rendering event
            if (Game1.gameMode == 0)
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (++batchOpens == 1)
                    events.Rendering.RaiseEmpty();
            }
            else
            {
                if (Game1.drawLighting)
                {
                    base.GraphicsDevice.SetRenderTarget(Game1.lightmap);
                    base.GraphicsDevice.Clear(Color.White * 0f);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                    if (++batchOpens == 1)
                        events.Rendering.RaiseEmpty();
                    Color lighting = (Game1.currentLocation.Name.StartsWith("UndergroundMine") && Game1.currentLocation is MineShaft) ? (Game1.currentLocation as MineShaft).getLightingColor(gameTime) : ((Game1.ambientLight.Equals(Color.White) || (Game1.isRaining && (bool)Game1.currentLocation.isOutdoors)) ? Game1.outdoorLight : Game1.ambientLight);
                    Game1.spriteBatch.Draw(Game1.staminaRect, Game1.lightmap.Bounds, lighting);
                    foreach (LightSource lightSource in Game1.currentLightSources)
                    {
                        if ((Game1.isRaining || Game1.isDarkOut()) && lightSource.lightContext.Value == LightSource.LightContext.WindowLight)
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
                            Game1.spriteBatch.Draw(lightSource.lightTexture, Game1.GlobalToLocal(Game1.viewport, lightSource.position) / (Game1.options.lightingQuality / 2), lightSource.lightTexture.Bounds, lightSource.color, 0f, new Vector2(lightSource.lightTexture.Bounds.Center.X, lightSource.lightTexture.Bounds.Center.Y), (float)lightSource.radius / (float)(Game1.options.lightingQuality / 2), SpriteEffects.None, 0.9f);
                        }
                    }
                    Game1.spriteBatch.End();
                    base.GraphicsDevice.SetRenderTarget(target_screen);
                }
                if (Game1.bloomDay && Game1.bloom != null)
                {
                    Game1.bloom.BeginDraw();
                }
                base.GraphicsDevice.Clear(Game1.bgColor);
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (++batchOpens == 1)
                    events.Rendering.RaiseEmpty();
                events.RenderingWorld.RaiseEmpty();
                if (Game1.background != null)
                {
                    Game1.background.draw(Game1.spriteBatch);
                }
                Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                Game1.currentLocation.Map.GetLayer("Back").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                Game1.currentLocation.drawWater(Game1.spriteBatch);
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
                            if (!k.swimming && !k.HideShadow && !k.IsInvisible && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(k.getTileLocation()))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, k.Position + new Vector2((float)(k.Sprite.SpriteWidth * 4) / 2f, k.GetBoundingBox().Height + ((!k.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)k.yJumpOffset / 40f) * (float)k.scale, SpriteEffects.None, Math.Max(0f, (float)k.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    else
                    {
                        foreach (NPC l in Game1.CurrentEvent.actors)
                        {
                            if (!l.swimming && !l.HideShadow && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(l.getTileLocation()))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, l.Position + new Vector2((float)(l.Sprite.SpriteWidth * 4) / 2f, l.GetBoundingBox().Height + ((!l.IsMonster) ? ((l.Sprite.SpriteHeight <= 16) ? (-4) : 12) : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)l.yJumpOffset / 40f) * (float)l.scale, SpriteEffects.None, Math.Max(0f, (float)l.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    foreach (Farmer f3 in this._farmerShadows)
                    {
                        if (!Game1.multiplayer.isDisconnecting(f3.UniqueMultiplayerID) && !f3.swimming && !f3.isRidingHorse() && (Game1.currentLocation == null || !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(f3.getTileLocation())))
                        {
                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(f3.Position + new Vector2(32f, 24f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f - (((f3.running || f3.UsingTool) && f3.FarmerSprite.currentAnimationIndex > 1) ? ((float)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[f3.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, 0f);
                        }
                    }
                }
                Layer building_layer = Game1.currentLocation.Map.GetLayer("Buildings");
                building_layer.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                Game1.mapDisplayDevice.EndScene();
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (!Game1.currentLocation.shouldHideCharacters())
                {
                    if (Game1.CurrentEvent == null)
                    {
                        foreach (NPC n in Game1.currentLocation.characters)
                        {
                            if (!n.swimming && !n.HideShadow && !n.isInvisible && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(n.getTileLocation()))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, n.Position + new Vector2((float)(n.Sprite.SpriteWidth * 4) / 2f, n.GetBoundingBox().Height + ((!n.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)n.yJumpOffset / 40f) * (float)n.scale, SpriteEffects.None, Math.Max(0f, (float)n.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    else
                    {
                        foreach (NPC n2 in Game1.CurrentEvent.actors)
                        {
                            if (!n2.swimming && !n2.HideShadow && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(n2.getTileLocation()))
                            {
                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, n2.Position + new Vector2((float)(n2.Sprite.SpriteWidth * 4) / 2f, n2.GetBoundingBox().Height + ((!n2.IsMonster) ? 12 : 0))), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)n2.yJumpOffset / 40f) * (float)n2.scale, SpriteEffects.None, Math.Max(0f, (float)n2.getStandingY() / 10000f) - 1E-06f);
                            }
                        }
                    }
                    foreach (Farmer f4 in this._farmerShadows)
                    {
                        float draw_layer = Math.Max(0.0001f, f4.getDrawLayer() + 0.00011f) - 0.0001f;
                        if (!f4.swimming && !f4.isRidingHorse() && Game1.currentLocation != null && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(f4.getTileLocation()))
                        {
                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(f4.Position + new Vector2(32f, 24f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f - (((f4.running || f4.UsingTool) && f4.FarmerSprite.currentAnimationIndex > 1) ? ((float)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[f4.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, draw_layer);
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
                        Location draw_location = new Location((int)vector_draw_position.X, (int)vector_draw_position.Y);
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
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (Game1.displayFarmer && Game1.player.ActiveObject != null && (bool)Game1.player.ActiveObject.bigCraftable && this.checkBigCraftableBoundariesForFrontLayer() && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)
                {
                    Game1.drawPlayerHeldObject(Game1.player);
                }
                else if (Game1.displayFarmer && Game1.player.ActiveObject != null && ((Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways")) || (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, (int)Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways"))))
                {
                    Game1.drawPlayerHeldObject(Game1.player);
                }
                if ((Game1.player.UsingTool || Game1.pickingTool) && Game1.player.CurrentTool != null && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool) && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)
                {
                    Game1.drawTool(Game1.player);
                }
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
                this.drawWeather(gameTime, target_screen);
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
                Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (Game1.eventUp && Game1.currentLocation.currentEvent != null)
                {
                    foreach (NPC m in Game1.currentLocation.currentEvent.actors)
                    {
                        if (m.isEmoting)
                        {
                            Vector2 emotePosition = m.getLocalPosition(Game1.viewport);
                            emotePosition.Y -= 140f;
                            if (m.Age == 2)
                            {
                                emotePosition.Y += 32f;
                            }
                            else if (m.Gender == 1)
                            {
                                emotePosition.Y += 10f;
                            }
                            Game1.spriteBatch.Draw(Game1.emoteSpriteSheet, emotePosition, new Microsoft.Xna.Framework.Rectangle(m.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, m.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)m.getStandingY() / 10000f);
                        }
                    }
                }
                Game1.spriteBatch.End();
                if (Game1.drawLighting)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, this.lightingBlend, SamplerState.LinearClamp, null, null);
                    Game1.spriteBatch.Draw(Game1.lightmap, Vector2.Zero, Game1.lightmap.Bounds, Color.White, 0f, Vector2.Zero, Game1.options.lightingQuality / 2, SpriteEffects.None, 1f);
                    if (Game1.isRaining && (bool)Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
                    {
                        Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.OrangeRed * 0.45f);
                    }
                    Game1.spriteBatch.End();
                }
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
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
                if (Game1.currentBillboard != 0 && !this.takingMapScreenshot)
                {
                    this.drawBillboard();
                }
                if (!Game1.eventUp && Game1.farmEvent == null && Game1.currentBillboard == 0 && Game1.gameMode == 3 && !this.takingMapScreenshot && Game1.isOutdoorMapSmallerThanViewport())
                {
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, -Math.Min(Game1.viewport.X, 4096), Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64, 0, Math.Min(4096, Game1.graphics.GraphicsDevice.Viewport.Width - (-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64)), Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                }
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
            }
            if (Game1.farmEvent != null)
            {
                Game1.farmEvent.draw(Game1.spriteBatch);
            }
            if (Game1.dialogueUp && !Game1.nameSelectUp && !Game1.messagePause && (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is DialogueBox)) && !this.takingMapScreenshot)
            {
                this.drawDialogueBox();
            }
            if (Game1.progressBar && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, Game1.dialogueWidth, 32), Color.LightGray);
                Game1.spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, (int)(Game1.pauseAccumulator / Game1.pauseTime * (float)Game1.dialogueWidth), 32), Color.DimGray);
            }
            if (Game1.eventUp && Game1.currentLocation != null && Game1.currentLocation.currentEvent != null)
            {
                Game1.currentLocation.currentEvent.drawAfterMap(Game1.spriteBatch);
            }
            if (Game1.isRaining && Game1.currentLocation != null && (bool)Game1.currentLocation.isOutdoors && !(Game1.currentLocation is Desert))
            {
                Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Blue * 0.2f);
            }
            if ((Game1.fadeToBlack || Game1.globalFade) && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause) && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - Game1.fadeToBlackAlpha) : Game1.fadeToBlackAlpha));
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
            if (Game1.showKeyHelp && !this.takingMapScreenshot)
            {
                Game1.spriteBatch.DrawString(Game1.smallFont, Game1.keyHelpString, new Vector2(64f, (float)(Game1.viewport.Height - 64 - (Game1.dialogueUp ? (192 + (Game1.isQuestion ? (Game1.questionChoices.Count * 64) : 0)) : 0)) - Game1.smallFont.MeasureString(Game1.keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
            }
            if (Game1.activeClickableMenu != null && !this.takingMapScreenshot)
            {
                try
                {
                    events.RenderingActiveMenu.RaiseEmpty();
                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                    events.RenderedActiveMenu.RaiseEmpty();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                    Game1.activeClickableMenu.exitThisMenu();
                }
            }
            else if (Game1.farmEvent != null)
            {
                Game1.farmEvent.drawAboveEverything(Game1.spriteBatch);
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
            this.renderScreenBuffer(target_screen);
        }
#endif
    }
}
