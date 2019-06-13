using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Com.Pgyersdk.Update;
using DllRewrite;
using Java.Lang;
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
using static ModLoader.Common.Utils;
using Exception = System.Exception;
using File = Java.IO.File;
using Object = System.Object;
using Thread = System.Threading.Thread;
using Uri = Android.Net.Uri;

namespace ModLoader
{
    [Activity(Label = "@string/ApplicationName"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    [SuppressMessage("ReSharper", "ArrangeThisQualifier")]
    public class Activity1 : AndroidGameActivity
    {
        private string[] requiredPermissions => new[] { "android.permission.ACCESS_NETWORK_STATE", "android.permission.ACCESS_WIFI_STATE", "android.permission.INTERNET", "android.permission.READ_EXTERNAL_STORAGE", "android.permission.VIBRATE", "android.permission.WAKE_LOCK", "android.permission.WRITE_EXTERNAL_STORAGE", "com.android.vending.CHECK_LICENSE" };
        private string[] DeniedPermissionsArray
        {
            get
            {
                List<string> list = new List<string>();
                foreach (string permission in this.requiredPermissions)
                {
                    if (this.PackageManager.CheckPermission(permission, this.PackageName) != Permission.Granted)
                    {
                        list.Add(permission);
                    }
                }
                return list.ToArray();
            }
        }
        public bool HasPermissions => this.PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.INTERNET", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.READ_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.VIBRATE", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.WAKE_LOCK", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("android.permission.WRITE_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted && this.PackageManager.CheckPermission("com.android.vending.CHECK_LICENSE", this.PackageName) == Permission.Granted;

        public void PromptForPermissions()
        {
            ActivityCompat.RequestPermissions(this, this.DeniedPermissionsArray, 0);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            if (permissions.Length == 0)
            {
            }
            else
            {
                string languageCode = Java.Util.Locale.Default.Language.Substring(0, 2);
                int num = 0;
                if (requestCode == 0)
                {
                    for (int index = 0; index < grantResults.Length; ++index)
                    {
                        if (grantResults[index] == Android.Content.PM.Permission.Granted)
                            ++num;
                        else if (grantResults[index] == Android.Content.PM.Permission.Denied)
                        {
                            try
                            {
                                AlertDialog.Builder builder = new AlertDialog.Builder((Context)this);
                                if (ActivityCompat.ShouldShowRequestPermissionRationale((Activity)this, permissions[index]))
                                {
                                    builder.SetMessage(this.PermissionMessageA(languageCode));
                                    builder.SetPositiveButton(this.GetOKString(languageCode), (EventHandler<DialogClickEventArgs>)((senderAlert, args) =>
                                    {
                                        this.PromptForPermissions();
                                    }));
                                }
                                else
                                {
                                    builder.SetMessage(this.PermissionMessageB(languageCode));
                                    builder.SetPositiveButton(this.GetOKString(languageCode), (EventHandler<DialogClickEventArgs>)((senderAlert, args) => OpenAppSettingsOnPhone(this)));
                                }
                                Dialog dialog = (Dialog)builder.Create();
                                if (this.IsFinishing)
                                    return;
                                dialog.Show();
                                return;
                            }
                            catch (IllegalArgumentException ex)
                            {
                                // ISSUE: variable of the null type
                                Microsoft.AppCenter.Crashes.Crashes.TrackError((System.Exception)ex, null);
                                OpenInPlayStore();
                                this.Finish();
                                return;
                            }
                        }
                    }
                }
                if (num != permissions.Length)
                    return;
                this.OnCreatePartTwo();
            }
        }
        private readonly Mutex _working = new Mutex(false);

        public static Activity1 Instance { get; private set; }

        private readonly HttpClient _httpClient = new HttpClient();

        private static readonly Dictionary<int, Action> MessageHandler = new Dictionary<int, Action>();
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
            base.OnCreate(bundle);
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
            {
                StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder().Build());
            }
            try { 
                File errorLog = this.FilesDir.ListFiles().FirstOrDefault(f => f.IsDirectory && f.Name == "error")?.ListFiles().FirstOrDefault(f => f.Name.EndsWith(".dat"));
                if (errorLog != null)
                {
                    string errorLogPath = Path.Combine(this.ExternalCacheDir.AbsolutePath, "error.dat");
                    StreamToFile(new FileStream(errorLog.AbsolutePath, FileMode.Open), errorLogPath);
                    ShowConfirmDialog(this, Resource.String.Error, Resource.String.CrashReportMessage, Resource.String.View, Resource.String.Dismiss, () => { OpenTextFile(this, errorLogPath); });
                }
                Type[] services = { typeof(Analytics), typeof(Crashes) };
                AppCenter.Start("b8eaba94-d276-4c97-9953-0c91e7357e21", services);
                IEnumerable<File> errorLogs = this.FilesDir.ListFiles().FirstOrDefault(f => f.IsDirectory && f.Name == "error")?.ListFiles().ToList().FindAll(f => f.Name.EndsWith(".dat") || f.Name.EndsWith(".throwable"));
                if (errorLogs != null) foreach (var file in errorLogs) file.Delete();
            }
            catch (Exception)
            {
                // ignored
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            this.SetContentView(Resource.Layout.layout_main);
            if (!this.HasPermissions)
                this.PromptForPermissions();
            else
                this.OnCreatePartTwo();
        }

        private void OnCreatePartTwo()
        {
            if (GetConfig(this, "compatCheck", "true") == "false")
                Constants.CompatCheck = false;
            if (GetConfig(this, "upgradeCheck", "true") == "false")
                Constants.UpgradeCheck = false;
            else
                new PgyUpdateManager.Builder().SetForced(false).SetUserCanRetry(true).SetDeleteHistroyApk(true).Register();
            this.InitEnvironment();
            this.FindViewById<Button>(Resource.Id.buttonSetting).Click += (sender, args) =>
            {
                this.StartActivity(typeof(ActivitySetting));
            };

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
                            MakeToast(this, this.Resources.GetText(Resource.String.NotInstalledMessage), ToastLength.Short);
                            return;
                        }
                        if (!new File(Constants.GamePath).Exists())
                        {
                            Directory.CreateDirectory(Constants.GamePath);
                        }
                        StatFs sf = new StatFs(Constants.GamePath);
                        if (sf.AvailableBytes + GetDirectoryLength(Path.Combine(Constants.GamePath, "Game" + Path.DirectorySeparatorChar)) -
                            160 * 1024 * 1024 < 0)
                        {
                            MakeToast(this, this.Resources.GetText(Resource.String.StorageIsFullMessage), ToastLength.Short);
                            return;
                        }
                        AlertDialog dialog = null;
                        ShowProgressDialog(this, Resource.String.Extract, this.Resources.GetText(Resource.String.ExtractingMessage), dlg => { dialog = dlg; });
                        while (dialog == null)
                        {
                            Thread.Sleep(50);
                        }
                        string sourceDir = packageInfo.ApplicationInfo.SourceDir;
                        ZipHelper.UnZip(sourceDir, Path.Combine(Constants.GamePath, "Game" + Path.DirectorySeparatorChar));
                        dialog.Dismiss();
                        MakeToast(this, this.Resources.GetText(Resource.String.ExtractedMessage),
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
                            MakeToast(this, this.Resources.GetText(Resource.String.NotExtractedMessage), ToastLength.Short);
                            return;
                        }
                        AlertDialog dialog = null;
                        ShowProgressDialog(this, Resource.String.Generate, this.Resources.GetText(Resource.String.GeneratingMessage), dlg => { dialog = dlg; });
                        while (dialog == null)
                        {
                            Thread.Sleep(50);
                        }
                        MethodPatcher mp = new MethodPatcher();
                        AssemblyDefinition stardewValley = mp.InsertModHooks();
                        FileStream stream = new FileStream(Path.Combine(Constants.GamePath, "StardewValley.dll"), FileMode.Create,
                            FileAccess.Write, FileShare.Read);
                        stardewValley.Write(stream);
                        stream.Close();
                        AssemblyDefinition monoFramework = mp.InsertMonoHooks();
                        stream = new FileStream(Path.Combine(Constants.GamePath, "MonoGame.Framework.dll"), FileMode.Create,
                            FileAccess.Write, FileShare.Read);
                        monoFramework.Write(stream);
                        stream.Close();
                        Stream stream2 = this.Resources.OpenRawResource(Resource.Raw.SMDroidFiles);
                        ZipHelper.UnZip(stream2, Constants.GamePath);
                        dialog.Dismiss();
                        MakeToast(this, this.Resources.GetText(Resource.String.GeneratedMessage),
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
                if (!new File(Constants.GamePath).Exists())
                {
                    Directory.CreateDirectory(Constants.GamePath);
                }
                if (!new File(Path.Combine(Constants.ContentPath, "XACT/FarmerSounds.xgs".Replace('/', Path.DirectorySeparatorChar))).Exists())
                {
                    MakeToast(this, this.Resources.GetText(Resource.String.NotExtractedMessage), ToastLength.Short);
                    return;
                }
                if (!new File(Path.Combine(Constants.GamePath, "StardewValley.dll")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "MonoGame.Framework.dll")).Exists())
                {
                    MakeToast(this, this.Resources.GetText(Resource.String.NotGeneratedMessage), ToastLength.Short);
                    return;
                }
                if (!new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.config.json")).Exists() ||
                    !new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.metadata.json")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "StardewModdingAPI.dll")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "System.Xml.Linq.dll")).Exists())
                {
                    Stream stream = this.Resources.OpenRawResource(Resource.Raw.SMDroidFiles);
                    ZipHelper.UnZip(stream, Constants.GamePath);
                }
                this.StartActivityForResult(typeof(SMainActivity), 1);
            };

            this.FindViewById<Button>(Resource.Id.buttonWiki).Click += (sender, args) =>
            {
                Uri uri = Uri.Parse("http://smd.zaneyork.cn");
                Intent intent = new Intent(Intent.ActionView, uri);
                this.StartActivity(intent);
            };
        }


        private void InitEnvironment()
        {
            this._working.WaitOne();
            try
            {
                if (!new File(Constants.GamePath).Exists())
                {
                    Directory.CreateDirectory(Constants.GamePath);
                }
                if (!new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.config.json")).Exists() ||
                    !new File(Path.Combine(Constants.GameInternalPath, "StardewModdingAPI.metadata.json")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "StardewModdingAPI.dll")).Exists() ||
                    !new File(Path.Combine(Constants.GamePath, "System.Xml.Linq.dll")).Exists())
                {
                    Stream stream = this.Resources.OpenRawResource(Resource.Raw.SMDroidFiles);
                    ZipHelper.UnZip(stream, Constants.GamePath);
                }
                string modListFileName = Path.Combine(Constants.GameInternalPath, "ModList.json");
                if (!new File(modListFileName).Exists())
                {
                    Stream stream = this.Resources.OpenRawResource(Resource.Raw.ModList);
                    StreamToFile(stream, modListFileName);
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
                            if (!this.IsFinishing)
                            {
                                this.InvokeActivityThread(0, this.PrepareModList);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }).Start();
            }
            finally
            {
                this._working.ReleaseMutex();
            }
        }

        internal void InstallMod(ModInfo mod)
        {
            new Thread(async () =>
            {
                if (!this._working.WaitOne(10))
                    return;
                try
                {
                    AlertDialog dialog = null;
                    ShowProgressDialog(this, Resource.String.ModInstall,
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
                            MakeToast(this, this.Resources.GetText(Resource.String.ModInstalledMessage),
                                ToastLength.Long);
                            this.InvokeActivityThread(0, this.PrepareModList);
                        }
                        else
                        {
                            MakeToast(this, this.Resources.GetText(Resource.String.NetworkErrorMessage),
                                ToastLength.Long);
                        }
                    }
                    catch (Exception)
                    {
                        MakeToast(this, this.Resources.GetText(Resource.String.NetworkErrorMessage),
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
        internal void ConfigMod(string configPath)
        {
            OpenTextFile(this, configPath);
        }

        internal void RemoveMod(ModInfo mod)
        {
            if (mod.Metadata?.DirectoryPath != null)
            {
                File file = new File(mod.Metadata.DirectoryPath);
                if (file.Exists() && file.IsDirectory)
                {
                    ShowConfirmDialog(this, Resource.String.Confirm, Resource.String.RemoveConfirmMessage, Resource.String.Confirm, Resource.String.Cancel,
                        () =>
                        {
                            Directory.Delete(mod.Metadata.DirectoryPath, true);
                            MakeToast(this, this.Resources.GetText(Resource.String.ModRemovedMessage),
                                ToastLength.Long);
                            this.InvokeActivityThread(0, this.PrepareModList);
                        });
                }
            }
        }

        private void PrepareModList()
        {
            if (!new File(Constants.ModPath).Exists())
            {
                Directory.CreateDirectory(Constants.ModPath);
            }
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

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == 1)
            {
                this.Finish();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PgyUpdateManager.UnRegister();
        }

        private string PermissionMessageA(string languageCode)
        {
            if (languageCode == "de")
                return "Du musst die Erlaubnis zum Lesen/Schreiben auf dem externen Speicher geben, um das Spiel zu speichern und Speicherstände auf andere Plattformen übertragen zu können. Bitte gib diese Genehmigung, um spielen zu können.";
            if (languageCode == "es")
                return "Para guardar la partida y transferir partidas guardadas a y desde otras plataformas, se necesita permiso para leer/escribir en almacenamiento externo. Concede este permiso para poder jugar.";
            if (languageCode == "ja")
                return "外部機器への読み込み/書き出しの許可が、ゲームのセーブデータの保存や他プラットフォームとの双方向のデータ移行実行に必要です。プレイを続けるには許可をしてください。";
            if (languageCode == "pt")
                return "Para salvar o jogo e transferir jogos salvos entre plataformas é necessário permissão para ler/gravar em armazenamento externo. Forneça essa permissão para jogar.";
            if (languageCode == "ru")
                return "Для сохранения игры и переноса сохранений с/на другие платформы нужно разрешение на чтение-запись на внешнюю память. Дайте разрешение, чтобы начать играть.";
            if (languageCode == "ko")
                return "게임을 저장하려면 외부 저장공간에 대한 읽기/쓰기 권한이 필요합니다. 또한 저장 데이터 이전 기능을 허용해 다른 플랫폼에서 게임 진행상황을 가져올 때에도 권한이 필요합니다. 게임을 플레이하려면 권한을 허용해 주십시오.";
            if (languageCode == "tr")
                return "Oyunu kaydetmek ve kayıtları platformlardan platformlara taşımak için harici depolamada okuma/yazma izni gereklidir. Lütfen oynayabilmek için izin verin.";
            if (languageCode == "fr")
                return "Une autorisation de lecture / écriture sur un stockage externe est requise pour sauvegarder le jeu et vous permettre de transférer des sauvegardes vers et depuis d'autres plateformes. Veuillez donner l'autorisation afin de jouer.";
            if (languageCode == "hu")
                return "A játék mentéséhez, és ahhoz, hogy a különböző platformok között hordozhasd a játékmentést, engedélyezned kell a külső tárhely olvasását/írását, Kérjük, a játékhoz engedélyezd ezeket.";
            if (languageCode == "it")
                return "È necessaria l'autorizzazione a leggere/scrivere su un dispositivo di memorizzazione esterno per salvare la partita e per consentire di trasferire i salvataggi da e su altre piattaforme. Concedi l'autorizzazione per giocare.";
            return languageCode == "zh" ? "《星露谷物语》请求获得授权用来保存游戏数据以及访问线上功能。" : "Read/write to external storage permission is required to save the game, and to allow to you transfer saves to and from other platforms. Please give permission in order to play.";
        }

        private string PermissionMessageB(string languageCode)
        {
            if (languageCode == "de")
                return "Bitte geh in die Handy-Einstellungen > Apps > Stardew Valley > Berechtigungen und aktiviere den Speicher, um das Spiel zu spielen.";
            if (languageCode == "es")
                return "En el teléfono, ve a Ajustes > Aplicaciones > Stardew Valley > Permisos y activa Almacenamiento para jugar al juego.";
            if (languageCode == "ja")
                return "設定 > アプリ > スターデューバレー > 許可の順に開いていき、ストレージを有効にしてからゲームをプレイしましょう。";
            if (languageCode == "pt")
                return "Acesse Configurar > Aplicativos > Stardew Valley > Permissões e ative Armazenamento para jogar.";
            if (languageCode == "ru")
                return "Перейдите в меню Настройки > Приложения > Stardew Valley > Разрешения и дайте доступ к памяти, чтобы начать играть.";
            if (languageCode == "ko")
                return "휴대전화의 설정 > 어플리케이션 > 스타듀 밸리 > 권한 에서 저장공간을 활성화한 뒤 게임을 플레이해 주십시오.";
            if (languageCode == "tr")
                return "Lütfen oyunu oynayabilmek için telefonda Ayarlar > Uygulamalar > Stardew Valley > İzinler ve Depolamayı etkinleştir yapın.";
            if (languageCode == "fr")
                return "Veuillez aller dans les Paramètres du téléphone> Applications> Stardew Valley> Autorisations, puis activez Stockage pour jouer.";
            if (languageCode == "hu")
                return "Lépje be a telefonodon a Beállítások > Alkalmazások > Stardew Valley > Engedélyek menübe, majd engedélyezd a Tárhelyet a játékhoz.";
            if (languageCode == "it")
                return "Nel telefono, vai su Impostazioni > Applicazioni > Stardew Valley > Autorizzazioni e attiva Memoria archiviazione per giocare.";
            return languageCode == "zh" ? "可在“设置-权限隐私-按应用管理权限-星露谷物语”进行设置，并打开“电话”、“读取位置信息”、“存储”权限。" : "Please go into phone Settings > Apps > Stardew Valley > Permissions, and enable Storage to play the game.";
        }
        private string GetOKString(string languageCode)
        {
            if (languageCode == "de")
                return "OK";
            if (languageCode == "es")
                return "DE ACUERDO";
            if (languageCode == "ja")
                return "OK";
            if (languageCode == "pt")
                return "Está bem";
            if (languageCode == "ru")
                return "Хорошо";
            if (languageCode == "ko")
                return "승인";
            if (languageCode == "tr")
                return "tamam";
            if (languageCode == "fr")
                return "D'accord";
            if (languageCode == "hu")
                return "rendben";
            if (languageCode == "it")
                return "ok";
            return languageCode == "zh" ? "好" : "OK";
        }
    }

}

