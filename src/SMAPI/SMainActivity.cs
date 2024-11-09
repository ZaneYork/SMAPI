#if SMAPI_FOR_MOBILE
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Framework;
using StardewValley;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Android.Content;
using Android.Support.V4.Provider;
using Java.IO;
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

        private Action _callback;

        private static bool ErrorDetected;
        private static bool Migrating = false;

        protected override void OnCreate(Bundle bundle)
        {
            MainActivity.instance = this;
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P) this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            FarmMigrationPatch.Apply();

            SMainActivity.Instance = this;
            try
            {
                File errorLog = this.FilesDir.ListFiles().FirstOrDefault(f => f.IsDirectory && f.Name == "error")?.ListFiles().FirstOrDefault(f => f.Name.EndsWith(".dat"));
                if (errorLog != null)
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
                if (!this.CheckSMAPIMigration()) return;

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

                if (string.IsNullOrWhiteSpace(modPath)) modPath = "Mods";

                this.core = new SCore(Path.Combine(EarlyConstants.StardewValleyBasePath, modPath), false, false);
                this.core.RunInteractively();

                typeof(MainActivity).Assembly.GetType("StardewValley.Mobile.MobileDisplay")?.GetMethod("SetupDisplaySettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, Array.Empty<object>());
                typeof(MainActivity).GetMethod("SetZoomScaleAndMenuButtonScale", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, Array.Empty<object>());
                this.SetPaddingForMenus();

                this.SetContentView((View)this.core.Game.Services.GetService(typeof(View)));
                GameRunner.instance = this.core.Game;
                typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, this.core.Game.gamePtr);
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

        private void ShowMigrationPicker()
        {
            Intent intent = new Intent("android.intent.action.OPEN_DOCUMENT_TREE");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            this.StartActivityForResult(intent, 1235);
        }

        public bool CheckSMAPIMigration()
        {
            string storagePath = this.GetExternalFilesDir(null).AbsolutePath;
            if (SMainActivity.Migrating) return false;

            if (Directory.Exists(storagePath + "/smapi-internal"))
                return true;
            SMainActivity.Migrating = true;
            SAlertDialogUtil.AlertMessage($"SMAPI needs to locate StardewValley folder's content to continue", "Confirm",
                callback: type => { this.ShowMigrationPicker(); });
            return false;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != 1235)
                return;
            this.RunOnUiThread((Action)(() =>
            {
                if (resultCode == Result.Ok)
                    this.CopySMAPIData(data.Data);
                else
                    this.CheckSMAPIMigration();
            }));
        }

        private void CopySMAPIData(Android.Net.Uri folderUri)
        {
            if (!folderUri.LastPathSegment.EndsWith(":StardewValley"))
                this.CheckSMAPIMigration();
            else
            {
                Android.Content.Context context = Application.Context;
                string storagePath = context.GetExternalFilesDir(null).AbsolutePath;
                this.Window.SetFlags(WindowManagerFlags.NotTouchable, WindowManagerFlags.NotTouchable);
                Action ContinueGame = () =>
                {
                    this.Window.ClearFlags(WindowManagerFlags.NotTouchable);
                    SAlertDialogUtil.AlertMessage($"SMAPI migration is finished, please restart game to continue", "Confirm",
                        callback: type => { this.Finish(); });

                };
                Task.Run(() =>
                {
                    try
                    {
                        DocumentFile source = DocumentFile.FromTreeUri(context, folderUri);
                        if (!source.Exists())
                            return;
                        if (source.IsDirectory)
                        {
                            if (!Directory.Exists(storagePath))
                                Directory.CreateDirectory(storagePath);
                            foreach (DocumentFile listFile in source.ListFiles())
                            {
                                string destPath;
                                if (listFile.IsDirectory)
                                {
                                    if (listFile.Name.StartsWith("smapi-internal") || listFile.Name.StartsWith("ErrorLogs") || listFile.Name.StartsWith("Mods"))
                                        destPath = Path.Combine(storagePath, listFile.Name);
                                    else
                                        destPath = Path.Combine(storagePath, "/Saves", listFile.Name);

                                    if (!System.IO.File.Exists(destPath)) SMainActivity.DirectoryCopy(listFile, destPath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string exMessage = ex.Message;
                    }

                    this.RunOnUiThread(ContinueGame);
                });
            }
        }

        private static void DirectoryCopy(DocumentFile source, string dest)
        {
            if (!source.Exists())
                return;
            if (source.IsDirectory)
            {
                if (!Directory.Exists(dest))
                    Directory.CreateDirectory(dest);
                foreach (DocumentFile listFile in source.ListFiles())
                {
                    string str = Path.Combine(dest, listFile.Name);
                    if (!System.IO.File.Exists(str))
                        SMainActivity.DirectoryCopy(listFile, str);
                }
            }
            else
            {
                Stream stream = MainActivity.instance.ContentResolver.OpenInputStream(source.Uri);
                FileOutputStream fileOutputStream = new FileOutputStream(dest);
                byte[] numArray = new byte[1024];
                int len;
                while ((len = stream.Read((Span<byte>)numArray)) > 0)
                    fileOutputStream.Write(numArray, 0, len);
                stream.Close();
                fileOutputStream.Close();
            }
        }

        public new void PromptForPermissionsIfNecessary(Action callback = null)
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

        public new void CheckAppPermissions()
        {
            this.LogPermissions();
            if (this.HasPermissions)
                this.OnCreatePartTwo();
            else
                this.PromptForPermissionsWithReasonFirst();
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
                    if (this.PackageManager.CheckPermission(requiredPermissions[index], this.PackageName) != Permission.Granted)
                        stringList.Add(requiredPermissions[index]);

                return stringList.ToArray();
            }
        }

        public new void PromptForPermissions()
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
                    for (int index = 0; index < grantResults.Length; ++index)
                        if (grantResults[index] == Permission.Granted)
                            ++num;
                        else if (grantResults[index] == Permission.Denied)
                        {
                            this.PromptForPermissions();
                            return;
                        }

                if (num != permissions.Length)
                    return;
                if (this._callback != null)
                {
                    this._callback();
                    this._callback = null;
                }
                else
                    this.OnCreatePartTwo();
            }
        }
    }
}
#endif
