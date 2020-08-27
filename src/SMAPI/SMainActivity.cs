#if SMAPI_FOR_MOBILE
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Google.Android.Vending.Licensing;
using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework;
using StardewValley;
using System.Reflection;
using Android.Content.Res;
using Java.Interop;
using System.Linq;
using System.IO;
using File = Java.IO.File;
using Microsoft.AppCenter;
using Newtonsoft.Json;
using Microsoft.AppCenter.Crashes;
using Android.Content;
using Java.Lang;
using Exception = System.Exception;
using Thread = System.Threading.Thread;

namespace StardewModdingAPI
{
    [Activity(Label = "SMAPI Stardew Valley", Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Splash", MainLauncher = true, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
#if !ANDROID_TARGET_GOOGLE
    public class SMainActivity: MainActivity
#else
    public class SMainActivity : MainActivity, ILicenseCheckerCallback, IJavaObject, IDisposable, IJavaPeerable
#endif
    {
        internal SCore core;
        private LicenseChecker _licenseChecker;
#if ANDROID_TARGET_GOOGLE
        private ServerManagedPolicyExtended _serverManagedPolicyExtended;
#endif

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
                    SMainActivity.ErrorDetected = true;
                    SAlertDialogUtil.AlertMessage(System.IO.File.ReadAllText(errorLog.AbsolutePath), "Crash Detected", callback: (type =>
                    {
                        SMainActivity.ErrorDetected = false;
                    }));
                }
                Type[] services = {typeof(Microsoft.AppCenter.Analytics.Analytics), typeof(Microsoft.AppCenter.Crashes.Crashes)};
                AppCenter.Start(Constants.MicrosoftAppSecret, services);
                AppCenter.SetUserId(Constants.ApiVersion.ToString());
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
                Game1 game1 = (Game1) typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this);
                if (game1 != null)
                {
                    game1.Exit();
                }

                if (SMainActivity.ErrorDetected)
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        SMainActivity.Instance.RunOnUiThread(() => this.OnCreatePartTwo());
                    }).Start();
                    return;
                }
                new SGameConsole();

                Program.Main(null);
                string modPath = null;
                if (System.IO.File.Exists(Constants.ApiUserConfigPath))
                {
                    var settings = JsonConvert.DeserializeObject<Framework.Models.SConfig>(System.IO.File.ReadAllText(Constants.ApiUserConfigPath));
                    modPath = settings.ModsPath;
                    Constants.HarmonyEnabled = !settings.DisableMonoMod;
                }

                if (string.IsNullOrWhiteSpace(modPath))
                {
                    modPath = "StardewValley/Mods";
                }

                this.core = new SCore(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, modPath), false);
                this.core.RunInteractively();
                typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, this.core.Game);

                this.SetContentView((View) this.core.Game.Services.GetService(typeof(View)));

                this.CheckUsingServerManagedPolicy();
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
                    callback: type =>
                    {
                        Crashes.TrackError(ex);
                        this.Finish();
                    });
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
#if ANDROID_TARGET_GOOGLE
            this._serverManagedPolicyExtended = new ServerManagedPolicyExtended(this, new AESObfuscator(new byte[15]
            {
                46,
                65,
                30,
                128,
                103,
                57,
                74,
                64,
                51,
                88,
                95,
                45,
                77,
                117,
                36
            }, this.PackageName, Settings.Secure.GetString(this.ContentResolver, "android_id")));
            this._licenseChecker = new LicenseChecker(this, this._serverManagedPolicyExtended, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
            this._licenseChecker.CheckAccess(this);
#endif
        }

#if ANDROID_TARGET_GOOGLE
        public new void Allow(PolicyResponse response)
        {
            typeof(MainActivity).GetMethod("CheckToDownloadExpansion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
        }

        public new void DontAllow(PolicyResponse response)
        {
            switch (response)
            {
                case PolicyResponse.Retry:
                    typeof(MainActivity).GetMethod("WaitThenCheckForValidLicence")?.Invoke(this, null);
                    break;
                case PolicyResponse.Licensed:
                    typeof(MainActivity).GetMethod("CheckToDownloadExpansion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
                    break;
            }
        }
#endif
    }
}
#endif
