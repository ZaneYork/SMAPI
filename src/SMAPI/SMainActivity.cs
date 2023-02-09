#if SMAPI_FOR_MOBILE
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework;
using StardewValley;
using System.Reflection;
using System.Linq;
using File = Java.IO.File;
using Newtonsoft.Json;
using Java.Lang;
using Java.Util;
using Exception = System.Exception;
using Thread = System.Threading.Thread;

namespace StardewModdingAPI
{
    [Activity(Label = "SMAPI Stardew Valley", Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Splash", MainLauncher = true, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
    public class SMainActivity : MainActivity
    {
        internal SCore core;

        public static SMainActivity Instance;

        private System.Action _callback;

        private static bool ErrorDetected;

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

        public void PromptForPermissionsIfNecessary(System.Action callback = null)
        {
            if (this.HasPermissions)
            {
                if (callback == null)
                    return;
                callback();
            }
            else
            {
                this._callback = callback;
                this.PromptForPermissionsWithReasonFirst();
            }
        }

        private void PromptForPermissionsWithReasonFirst() => this.PromptForPermissions();

        public void CheckAppPermissions()
        {
            this.LogPermissions();
            if (this.HasPermissions)
            {
                this.OnCreatePartTwo();
            }
            else
            {
                this.PromptForPermissionsWithReasonFirst();
            }
        }

        private string[] requiredPermissions => new string[4]
        {
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.ACCESS_WIFI_STATE",
            "android.permission.INTERNET",
            "android.permission.VIBRATE"
        };

        private string[] deniedPermissionsArray
        {
            get
            {
                List<string> stringList = new List<string>();
                string[] requiredPermissions = this.requiredPermissions;
                for (int index = 0; index < requiredPermissions.Length; ++index)
                {
                    if (this.PackageManager.CheckPermission(requiredPermissions[index], this.PackageName) != Permission.Granted)
                        stringList.Add(requiredPermissions[index]);
                }

                return stringList.ToArray();
            }
        }

        public void PromptForPermissions()
        {
            string[] permissionsArray = this.deniedPermissionsArray;
            if (permissionsArray.Length == 0)
                return;
            this.RequestPermissions(permissionsArray, 0);
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            Permission[] grantResults)
        {
            if (permissions.Length == 0)
            {
            }
            else
            {
                string languageCode = Locale.Default.Language.Substring(0, 2);
                int num = 0;
                if (requestCode == 0)
                {
                    for (int index = 0; index < grantResults.Length; ++index)
                    {
                        if (grantResults[index] == Permission.Granted)
                            ++num;
                        else if (grantResults[index] == Permission.Denied)
                        {
                            this.PromptForPermissions();
                            return;
                        }
                    }
                }

                if (num != permissions.Length)
                    return;
                if (this._callback != null)
                {
                    this._callback();
                    this._callback = (System.Action)null;
                }
                else
                {
                    this.OnCreatePartTwo();
                }
            }
        }
    }
}
#endif
