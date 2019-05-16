using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using Google.Android.Vending.Licensing;
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
            this.CheckUsingServerManagedPolicy();
        }
        private void CheckUsingServerManagedPolicy()
        {
            string packageName = Constants.GamePackageName;
            string deviceId = Settings.Secure.GetString(this.ContentResolver, "android_id");
            AESObfuscator obfuscator = new AESObfuscator(new byte[] { 0x2e, 0x41, 30, 0x80, 0x67, 0x39, 0x4a, 0x40, 0x33, 0x58, 0x5f, 0x2d, 0x4d, 0x75, 0x24 }, packageName, deviceId);
            ServerManagedPolicy policy = new ServerManagedPolicy(this, obfuscator);
            LicenseChecker licenseChecker = new LicenseChecker(this, policy, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
            typeof(MainActivity).GetField("_licenseChecker", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, licenseChecker);
            licenseChecker.CheckAccess(this);
        }
    }
}
