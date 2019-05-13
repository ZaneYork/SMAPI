using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Com.Pgyersdk;
using Com.Pgyersdk.Update;
using Com.Pgyersdk.Update.Javabean;
using DllRewrite;
using Java.Lang;
using ModLoader.Common;
using ModLoader.Helper;
using Mono.Cecil;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.ModData;
using File = Java.IO.File;
using Thread = System.Threading.Thread;

namespace ModLoader
{
    [Activity(Label = "@string/ApplicationName"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        private string[] requiredPermissions => new string[] { "android.permission.ACCESS_NETWORK_STATE", "android.permission.ACCESS_WIFI_STATE", "android.permission.INTERNET", "android.permission.READ_EXTERNAL_STORAGE", "android.permission.VIBRATE", "android.permission.WAKE_LOCK", "android.permission.WRITE_EXTERNAL_STORAGE", "com.android.vending.CHECK_LICENSE" };
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

        protected override void OnCreate(Bundle bundle)
        {
            Type[] services = new Type[] { typeof(Microsoft.AppCenter.Analytics.Analytics), typeof(Microsoft.AppCenter.Crashes.Crashes) };
            Microsoft.AppCenter.AppCenter.Start("b8eaba94-d276-4c97-9953-0c91e7357e21", services);
            base.OnCreate(bundle);
            base.RequestWindowFeature(WindowFeatures.NoTitle);
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
                            .FirstOrDefault(package => package.PackageName == "com.chucklefish.stardewvalley");
                        if (packageInfo == null)
                        {
                            Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotInstalledMessage), ToastLength.Short);
                            return;
                        }
                        Utils.MakeToast(this, this.Resources.GetText(Resource.String.ExtractingMessage),
                            ToastLength.Long);
                        string sourceDir = packageInfo.ApplicationInfo.SourceDir;
                        ZipHelper.UnZip(sourceDir, System.IO.Path.Combine(Constants.GamePath, "Game/"));
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
                        Utils.MakeToast(this, this.Resources.GetText(Resource.String.GeneratingMessage),
                            ToastLength.Long);
                        MethodPatcher mp = new MethodPatcher();
                        AssemblyDefinition StardewValley = mp.InsertModHooks();
                        StardewValley.Write(System.IO.Path.Combine(Constants.GamePath, "StardewValley.dll"));
                        AssemblyDefinition MonoFramework = mp.InsertMonoHooks();
                        MonoFramework.Write(System.IO.Path.Combine(Constants.GamePath, "MonoGame.Framework.dll"));
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
                if (!new File(System.IO.Path.Combine(Constants.ContentPath, "XACT/FarmerSounds.xgs")).Exists())
                {
                    Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotExtractedMessage), ToastLength.Short);
                    return;
                }
                if (!new File(System.IO.Path.Combine(Constants.GamePath, "StardewValley.dll")).Exists() ||
                    !new File(System.IO.Path.Combine(Constants.GamePath, "MonoGame.Framework.dll")).Exists())
                {
                    Utils.MakeToast(this, this.Resources.GetText(Resource.String.NotGeneratedMessage), ToastLength.Short);
                    return;
                }
                this.StartActivity(typeof(SMainActivity));
            };

            this.FindViewById<Button>(Resource.Id.buttonWiki).Click += (sender, args) =>
            {
                Android.Net.Uri uri = Android.Net.Uri.Parse("http://smd.zaneyork.cn");
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
            if (!new File(System.IO.Path.Combine(Constants.GamePath, "smapi-internal/StardewModdingAPI.config.json")).Exists() ||
                !new File(System.IO.Path.Combine(Constants.GamePath, "smapi-internal/StardewModdingAPI.metadata.json")).Exists()||
                !new File(System.IO.Path.Combine(Constants.GamePath, "StardewModdingAPI.dll")).Exists()||
                !new File(System.IO.Path.Combine(Constants.GamePath, "System.Xml.Linq.dll")).Exists()||
                !new File(System.IO.Path.Combine(Constants.GamePath, "StardewModdingAPI.Toolkit.dll")).Exists() ||
                !new File(System.IO.Path.Combine(Constants.GamePath, "StardewModdingAPI.Toolkit.CoreInterfaces.dll")).Exists())
            {
                Stream stream = this.Resources.OpenRawResource(Resource.Raw.SMDroidFiles);
                ZipHelper.UnZip(stream, Constants.GamePath);
            }
            ListView listView = this.FindViewById<ListView>(Resource.Id.listView1);
            ModToolkit toolkit = new ModToolkit();
            ModDatabase modDatabase = toolkit.GetModDatabase(StardewModdingAPI.Constants.ApiMetadataPath);
            ModResolver resolver = new ModResolver();
            IModMetadata[] mods = resolver.ReadManifests(toolkit, Constants.ModPath, modDatabase).ToArray();
            Array.Sort(mods, (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture));
            listView.Adapter = new ModListAdapter(this, Resource.Layout.layout_mod_list, mods);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PgyUpdateManager.UnRegister();
        }
    }
}

