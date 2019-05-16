using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Com.Pgyersdk.Update;
using DllRewrite;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Xna.Framework;
using ModLoader.Common;
using ModLoader.Helper;
using Mono.Cecil;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialisation;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

namespace ModLoader
{
    [Activity(Label = "@string/ApplicationName"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    public class Activity1 : AndroidGameActivity
    {
        private string[] requiredPermissions => new[] { "android.permission.ACCESS_NETWORK_STATE", "android.permission.ACCESS_WIFI_STATE", "android.permission.INTERNET", "android.permission.READ_EXTERNAL_STORAGE", "android.permission.VIBRATE", "android.permission.WAKE_LOCK", "android.permission.WRITE_EXTERNAL_STORAGE", "com.android.vending.CHECK_LICENSE" };
        private string[] deniedPermissionsArray
        {
            get
            {
                List<string> list = new List<string>();
                string[] requiredPermissions = this.requiredPermissions;
                for (int i = 0; i < requiredPermissions.Length; i++)
                {
                    if (this.PackageManager.CheckPermission(requiredPermissions[i], this.PackageName) != Permission.Granted)
                    {
                        list.Add(requiredPermissions[i]);
                    }
                }
                return list.ToArray();
            }
        }
        public bool HasPermissions => ((((this.PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", this.PackageName) == Permission.Granted) && (this.PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", this.PackageName) == Permission.Granted)) && ((this.PackageManager.CheckPermission("android.permission.INTERNET", this.PackageName) == Permission.Granted) && (this.PackageManager.CheckPermission("android.permission.READ_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted))) && (((this.PackageManager.CheckPermission("android.permission.VIBRATE", this.PackageName) == Permission.Granted) && (this.PackageManager.CheckPermission("android.permission.WAKE_LOCK", this.PackageName) == Permission.Granted)) && ((this.PackageManager.CheckPermission("android.permission.WRITE_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted) && (this.PackageManager.CheckPermission("com.android.vending.CHECK_LICENSE", this.PackageName) == Permission.Granted))));

        public void PromptForPermissions()
        {
            ActivityCompat.RequestPermissions(this, this.deniedPermissionsArray, 0);
        }

        private readonly Mutex _working = new Mutex(false);

        public static Activity1 Instance { get; private set; }
        private HttpClient _httpClient = new HttpClient();

        private static Dictionary<int, Action> MessageHandler = new Dictionary<int, Action>();
        private readonly Handler _handler = new Handler(message =>
        {
            if (MessageHandler.ContainsKey(message.What))
            {
                MessageHandler[message.What]();
            }
        });

        public void InvokeActivityThread(int what, Action action)
        {
            Message msg = new Message();
            msg.What = what;
            MessageHandler[what] = action;
            this._handler.SendMessage(msg);
        }

        protected override void OnCreate(Bundle bundle)
        {
            Instance = this;
            Type[] services = { typeof(Analytics), typeof(Crashes) };
            AppCenter.Start("b8eaba94-d276-4c97-9953-0c91e7357e21", services);
            base.OnCreate(bundle);
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            this.SetContentView(Resource.Layout.layout_main);
            if (!this.HasPermissions)
                this.PromptForPermissions();
            while (!this.HasPermissions)
            {
                Thread.Sleep(50);
            }
            new PgyUpdateManager.Builder().SetForced(false).SetUserCanRetry(true).SetDeleteHistroyApk(true).Register();

            this.FindViewById<Button>(Resource.Id.buttonExtract).Click += (sender, args) =>
            {
                new Thread(() =>
                {
                    if (!this._working.WaitOne(10))
                        return;
                    try
                    {
                        PackageInfo packageInfo = this.PackageManager.GetInstalledPackages(PackageInfoFlags.MatchAll)
                            .FirstOrDefault(package => package.PackageName == Constants.GamePackageName);
                        if (packageInfo == null)
                        {
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotInstalledMessage), ToastLength.Short);
                            return;
                        }
                        AlertDialog dialog = null;
                        Utils.ShowProgressDialog(this, Resource.String.Extract, this.Resources.GetText(Resource.String.ExtractingMessage), dlg => { dialog = dlg; });
                        while (dialog == null)
                        {
                            Thread.Sleep(50);
                        }
                        string sourceDir = packageInfo.ApplicationInfo.SourceDir;
                        ZipHelper.UnZip(sourceDir, Path.Combine(Constants.GamePath, "Game/"));
                        dialog.Dismiss();
                        Utils.MakeToast(this, this.Resources.GetText(Resource.String.ExtractedMessage),
                            ToastLength.Long);

                    }
                    finally
                    {
                        this._working.ReleaseMutex();
                    }
                }).Start();
            };
            this.FindViewById<Button>(Resource.Id.buttonGenerate).Click += (sender, args) =>
            {
                new Thread(() =>
                {
                    if (!this._working.WaitOne(10))
                        return;
                    try
                    {
                        if (!new File(Constants.AssemblyPath).Exists())
                        {
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotExtractedMessage), ToastLength.Short);
                            return;
                        }
                        AlertDialog dialog = null;
                        Utils.ShowProgressDialog(this, Resource.String.Generate, this.Resources.GetText(Resource.String.GeneratingMessage), dlg => { dialog = dlg; });
                        while (dialog == null)
                        {
                            Thread.Sleep(50);
                        }
                        MethodPatcher mp = new MethodPatcher();
                        AssemblyDefinition StardewValley = mp.InsertModHooks();
                        StardewValley.Write(Path.Combine(Constants.GamePath, "StardewValley.dll"));
                        AssemblyDefinition MonoFramework = mp.InsertMonoHooks();
                        MonoFramework.Write(Path.Combine(Constants.GamePath, "MonoGame.Framework.dll"));
                        dialog.Dismiss();
                        Utils.MakeToast(this, this.Resources.GetText(Resource.String.GeneratedMessage),
                            ToastLength.Long);
                    }
                    finally
                    {
                        this._working.ReleaseMutex();
                    }
                }).Start();
            };
            this.FindViewById<Button>(Resource.Id.buttonLaunch).Click += (sender, args) =>
            {
                if (!this._working.WaitOne(10))
                    return;
                this._working.ReleaseMutex();
                if (!new File(Path.Combine(Constants.ContentPath, "XACT/FarmerSounds.xgs".Replace('/',Path.DirectorySeparatorChar))).Exists())
                {
                    Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotExtractedMessage), ToastLength.Short);
                    return;
                }
                if (!new File(Path.Combine(Constants.GamePath, "StardewValley.dll")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "MonoGame.Framework.dll")).Exists())
                {
                    Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotGeneratedMessage), ToastLength.Short);
                    return;
                }
                this.StartActivity(typeof(SMainActivity));
                this.Finish();
            };

            this.FindViewById<Button>(Resource.Id.buttonWiki).Click += (sender, args) =>
            {
                Uri uri = Uri.Parse("http://smd.zaneyork.cn");
                Intent intent = new Intent(Intent.ActionView, uri);
                this.StartActivity(intent);
            };
            //this.FindViewById<Button>(Resource.Id.buttonModFolder).Click += (sender, args) =>
            //{

            //    Intent intent = new Intent(Intent.ActionGetContent);
            //    intent.AddCategory(Intent.CategoryOpenable);
            //    File modFolder = new File(Constants.ModPath);
            //    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
            //    {
            //        intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            //        Android.Net.Uri contentUri = FileProvider.GetUriForFile(this, "com.zane.smdroid.fileProvider", modFolder);
            //        intent.SetDataAndType(contentUri, "file/*.json");
            //    }
            //    else
            //    {
            //        intent.SetDataAndType(Android.Net.Uri.FromFile(modFolder), "file/*.json");
            //        intent.SetFlags(ActivityFlags.NewTask);
            //    }

            //    try
            //    {
            //        this.StartActivity(Intent.CreateChooser(intent, ""));
            //    }
            //    catch (ActivityNotFoundException) { }
            //};
            if (!new File(Constants.GamePath).Exists())
            {
                Directory.CreateDirectory(Constants.GamePath);
            }
            if (!new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.config.json")).Exists() ||
                !new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.metadata.json")).Exists()||
                !new File(Path.Combine(Constants.GamePath, "StardewModdingAPI.dll")).Exists()||
                !new File(Path.Combine(Constants.GamePath, "System.Xml.Linq.dll")).Exists()||
                !new File(Path.Combine(Constants.GamePath, "StardewModdingAPI.Toolkit.dll")).Exists() ||
                !new File(Path.Combine(Constants.GamePath, "StardewModdingAPI.Toolkit.CoreInterfaces.dll")).Exists())
            {
                Stream stream = this.Resources.OpenRawResource(Resource.Raw.SMDroidFiles);
                ZipHelper.UnZip(stream, Constants.GamePath);
            }
            string modListFileName = Path.Combine(Constants.GameInternalPath, "ModList.json");
            if (!new File(modListFileName).Exists())
            {
                Stream stream = this.Resources.OpenRawResource(Resource.Raw.ModList);
                Utils.StreamToFile(stream, modListFileName);
            }
            this.PrepareModList();
            new Thread(async () =>
            {
                try
                {
                    HttpResponseMessage responseMessage = this._httpClient
                        .GetAsync("https://github.com/ZaneYork/SMAPI/raw/android/ModLoader/Resources/Raw/ModList.json")
                        .Result.EnsureSuccessStatusCode();
                    string modList = await responseMessage.Content.ReadAsStringAsync();
                    string originJson = System.IO.File.ReadAllText(modListFileName);
                    if (originJson != modList)
                    {
                        new JsonHelper().Deserialise<ModInfo[]>(modList);
                        System.IO.File.WriteAllText(modListFileName, modList);
                        this.InvokeActivityThread(0, this.PrepareModList);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

            }).Start();
        }

        public void InstallMod(ModInfo mod)
        {
            new Thread(async () =>
            {
                if (!this._working.WaitOne(10))
                    return;
                try
                {
                    AlertDialog dialog = null;
                    Utils.ShowProgressDialog(this, Resource.String.ModInstall,
                        this.Resources.GetText(Resource.String.ModDownloadingMessage), dlg => { dialog = dlg; });
                    while (dialog == null)
                    {
                        Thread.Sleep(50);
                    }
                    try
                    {
                        HttpResponseMessage responseMessage = this._httpClient.GetAsync(mod.DownloadUrl).Result.EnsureSuccessStatusCode();
                        byte[] bytes = await responseMessage.Content.ReadAsByteArrayAsync();
                        if (bytes[0] == 80 && bytes[1] == 75)
                        {
                            ZipHelper.UnZip(new MemoryStream(bytes), Constants.ModPath + Path.DirectorySeparatorChar);
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.ModInstalledMessage),
                                ToastLength.Long);
                            this.InvokeActivityThread(0, this.PrepareModList);
                        }
                        else
                        {
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.NetworkErrorMessage),
                                ToastLength.Long);
                        }
                    }
                    catch (Exception)
                    {
                        Utils.MakeToast(this, this.Resources.GetText(Resource.String.NetworkErrorMessage),
                            ToastLength.Long);
                    }
                    finally
                    {
                        dialog.Dismiss();
                    }
                }
                finally
                {
                    this._working.ReleaseMutex();
                }
            }).Start();
        }

        public void RemoveMod(ModInfo mod)
        {
            if (mod.Metadata?.DirectoryPath != null)
            {
                File file = new File(mod.Metadata.DirectoryPath);
                if (file.Exists() && file.IsDirectory)
                {
                    Utils.ShowConfirmDialog(this, Resource.String.Confirm, Resource.String.RemoveConfirmMessage, Resource.String.Confirm, Resource.String.Cancel,
                        () =>
                        {
                            Directory.Delete(mod.Metadata.DirectoryPath, true);
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.ModRemovedMessage),
                                ToastLength.Long);
                            this.InvokeActivityThread(0, this.PrepareModList);
                        });
                }
            }
        }

        private void PrepareModList()
        {
            string modListFileName = Path.Combine(Constants.GameInternalPath, "ModList.json");
            new JsonHelper().ReadJsonFileIfExists(modListFileName, out ModInfo[] modInfos);
            Dictionary<string, ModInfo> modInfoDictionary = modInfos.ToDictionary(info => info.UniqueID, info => info);
            ListView listView = this.FindViewById<ListView>(Resource.Id.listView1);
            ModToolkit toolkit = new ModToolkit();
            ModDatabase modDatabase = toolkit.GetModDatabase(StardewModdingAPI.Constants.ApiMetadataPath);
            ModResolver resolver = new ModResolver();
            IModMetadata[] mods = resolver.ReadManifests(toolkit, Constants.ModPath, modDatabase).ToArray();
            Array.Sort(mods, (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture));
            List<ModInfo> modList = new List<ModInfo>();
            HashSet<string> installedModList = new HashSet<string>();
            foreach (IModMetadata metadata in mods)
            {
                if (!metadata.HasManifest())
                {
                    modList.Add(new ModInfo(metadata));
                }
                else if (modInfoDictionary.ContainsKey(metadata.Manifest.UniqueID))
                {
                    modInfoDictionary[metadata.Manifest.UniqueID].Metadata = metadata;
                    modList.Add(modInfoDictionary[metadata.Manifest.UniqueID]);
                    installedModList.Add(metadata.Manifest.UniqueID);
                }
                else
                {
                    modList.Add(new ModInfo(metadata));
                }
            }

            foreach (ModInfo modInfo in modInfos)
            {
                if (!installedModList.Contains(modInfo.UniqueID))
                {
                    modList.Add(modInfo);
                }
            }
            listView.ScrollStateChanged -= this.ListView_ScrollStateChanged;
            listView.ScrollStateChanged += this.ListView_ScrollStateChanged;
            listView.Adapter = new ModListAdapter(this, Resource.Layout.layout_mod_list, modList);
            if (this._position != -1)
            {
                listView.SetSelection(this._position);
            }
        }

        private int _position = -1;
        private void ListView_ScrollStateChanged(object sender, AbsListView.ScrollStateChangedEventArgs e)
        {
            if (e.ScrollState == ScrollState.Idle)
            {
                this._position = e.View.FirstVisiblePosition;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PgyUpdateManager.UnRegister();
        }
    }
}

