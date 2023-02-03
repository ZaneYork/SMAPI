#if SMAPI_FOR_MOBILE
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework;
using StardewValley;
using System.Reflection;
using Java.Interop;
using System.Linq;
using File = Java.IO.File;
using Newtonsoft.Json;
using Android.Content;
using Android.Util;
using Java.Lang;
using Java.Util;
using Exception = System.Exception;
using Object = Java.Lang.Object;
using Thread = System.Threading.Thread;

namespace StardewModdingAPI
{
    [Activity(Label = "SMAPI Stardew Valley", Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Splash", MainLauncher = true, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
    public class SMainActivity : MainActivity
    {
        internal SCore core;

        public static SMainActivity Instance;

        private static bool ErrorDetected;

        public new bool HasPermissions
        {
            get
            {
                return this.PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.INTERNET", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.READ_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.VIBRATE", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.WAKE_LOCK", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("android.permission.WRITE_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted
                       && this.PackageManager.CheckPermission("com.android.vending.CHECK_LICENSE", this.PackageName) == Permission.Granted;
            }
        }

        private string[] requiredPermissions => new string[8]
        {
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.ACCESS_WIFI_STATE",
            "android.permission.INTERNET",
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.VIBRATE",
            "android.permission.WAKE_LOCK",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "com.android.vending.CHECK_LICENSE"
        };

        private string[] DeniedPermissionsArray
        {
            get
            {
                List<string> list = new List<string>();
                for (int i = 0; i < this.requiredPermissions.Length; i++)
                {
                    if (ContextCompat.CheckSelfPermission(this, this.requiredPermissions[i]) != 0)
                    {
                        list.Add(this.requiredPermissions[i]);
                    }
                }

                return list.ToArray();
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            MainActivity.instance = this;
            base.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            SMainActivity.Instance = this;
            try
            {
                File errorLog = this.FilesDir.ListFiles().FirstOrDefault(f => f.IsDirectory && f.Name == "error")?.ListFiles().FirstOrDefault(f => f.Name.EndsWith(".dat"));
                if (errorLog != null)
                {
                    try
                    {
                        Handler handler = new Handler((msg) => throw new RuntimeException());
                        SAlertDialogUtil.ShowDialog(System.IO.File.ReadAllText(errorLog.AbsolutePath), "Crash Detected", null, null, callback: (type =>
                        {
                            errorLog.Delete();
                            handler.SendEmptyMessage(0);
                        }));
                        try
                        {
                            Looper.Prepare();
                        }
                        catch (Exception)
                        {
                        }

                        Looper.Loop();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch
            {
                // ignored
            }

            base.OnCreate(bundle);
            this.CheckAppPermissions();
        }

        public void OnCreatePartTwo(int retry = 0)
        {
            try
            {
                Game1 game1 = (Game1)typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this);
                if (game1 != null)
                {
                    // game1.Exit();
                }

                new SGameConsole();

                Program.Main(null);
                string modPath = null;
                if (System.IO.File.Exists(Constants.ApiUserConfigPath))
                {
                    var settings = JsonConvert.DeserializeObject<Framework.Models.SConfig>(System.IO.File.ReadAllText(Constants.ApiUserConfigPath));
                    modPath = settings.ModsPath;
                    Constants.HarmonyEnabled = !settings.DisableMonoMod;
                    Constants.RewriteMissing = settings.RewriteMissing;
                }

                if (string.IsNullOrWhiteSpace(modPath))
                {
                    modPath = "Mods";
                }

                this.core = new SCore(System.IO.Path.Combine(EarlyConstants.StardewValleyBasePath, modPath), false, false);
                this.core.RunInteractively();
                typeof(MainActivity).GetMethod("SetZoomScaleAndMenuButtonScale", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, Array.Empty<object>());
                this.SetPaddingForMenus();
                typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, this.core.Game.gamePtr);

                this.SetContentView((View)this.core.Game.Services.GetService(typeof(View)));
                this.core.Game.Run();
            }
            catch when (retry < 3)
            {
                void RetryStart()
                {
                    Thread.Sleep(100);
                    SMainActivity.Instance.OnCreatePartTwo(retry + 1);
                }

                new Thread(RetryStart).Start();
            }
            catch (Exception ex)
            {
                SAlertDialogUtil.AlertMessage($"SMAPI failed to initialize: {ex}",
                    callback: type => { this.Finish(); });
            }
        }

        public new void CheckAppPermissions()
        {
            if (!this.HasPermissions)
                this.PromptForPermissions();
            else
                this.OnCreatePartTwo();
        }

        public new void PromptForPermissions()
        {
            ActivityCompat.RequestPermissions(this, this.DeniedPermissionsArray, 0);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            try
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
            catch (ActivityNotFoundException)
            {
            }

            if (this.HasPermissions)
                this.OnCreatePartTwo();
        }


        private void CheckUsingServerManagedPolicy()
        {
        }
    }
}
#endif
