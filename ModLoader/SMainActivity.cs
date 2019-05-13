using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using StardewModdingAPI;
using StardewModdingAPI.Framework;
using StardewValley;
using Constants = ModLoader.Common.Constants;

namespace ModLoader
{
    [Activity(Label = "Stardew Valley", Icon = "@drawable/icon", Theme = "@style/Theme.Splash", MainLauncher = false, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
    public class SMainActivity : MainActivity
    {

        protected override void OnCreate(Bundle bundle)
        {
            instance = this;
            base.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            PowerManager.WakeLock wakeLock = ((PowerManager)this.GetSystemService("power")).NewWakeLock(WakeLockFlags.Full, "StardewWakeLock");
            wakeLock.Acquire();
            typeof(MainActivity).GetField("_wakeLock", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, wakeLock);
            base.OnCreate(bundle);
            this.OnCreatePartTwo();
        }

        private void OnCreatePartTwo()
        {
            typeof(MainActivity).GetMethod("SetZoomScaleAndMenuButtonScale", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
            typeof(MainActivity).GetMethod("SetSavesPath", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
            this.SetPaddingForMenus();
            new GameConsole();
            SCore core = new SCore(Constants.ModPath, false);
            core.RunInteractively();
            Game1 game1 = StardewValley.Program.gamePtr;
            typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, game1);
            this.SetContentView((View)game1.Services.GetService(typeof(View)));
            typeof(MainActivity).GetMethod("CheckUsingServerManagedPolicy", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
        }
    }
}
