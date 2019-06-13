using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.IO;

namespace ModLoader.Common
{
    class Utils
    {
        public static byte[] FileToMemory(string filename)
        {
            byte[] bytes = new byte[2048];
            FileInputStream fs = new FileInputStream(filename);
            MemoryStream outStream = new MemoryStream();
            int len;
            while ((len = fs.Read(bytes, 0, bytes.Length)) > 0)
            {
                outStream.Write(bytes, 0, len);
            }

            fs.Close();
            return outStream.ToArray();
        }

        public static void StreamToFile(Stream stream, string fileName)
        {
            byte[] bytes = new byte[2048];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            int len;
            while ((len = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                bw.Write(bytes, 0, len);
            }

            bw.Close();
            fs.Close();
        }

        public static long GetDirectoryLength(string dirPath)
        {
            //判断给定的路径是否存在,如果不存在则退出
            if (!Directory.Exists(dirPath))
                return 0;
            long len = 0;
            //定义一个DirectoryInfo对象
            DirectoryInfo di = new DirectoryInfo(dirPath);
            //通过GetFiles方法,获取di目录中的所有文件的大小
            foreach (FileInfo fi in di.GetFiles())
            {
                len += fi.Length;
            }
            //获取di中所有的文件夹,并存到一个新的对象数组中,以进行递归
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {

                    len += GetDirectoryLength(dis[i].FullName);
                }
            }
            return len;
        }

        public static void InvokeLooperThread(Action action)
        {
            new Thread(() =>
            {

                Looper.Prepare();
                new Handler().Post(action);
                Looper.Loop();
            }).Start();
        }

        public static void MakeToast(Context context, string message, ToastLength toastLength)
        {
            InvokeLooperThread(() => Toast.MakeText(context, message, toastLength).Show());
        }

        public static void ShowProgressDialog(Context context, int titleId, string message, Action<AlertDialog> returnCallback)
        {
            InvokeLooperThread(() =>
            {
                ProgressDialog dialog = new ProgressDialog(context);
                dialog.SetTitle(titleId);
                dialog.SetMessage(message);
                dialog.SetCancelable(false);
                dialog.SetProgressStyle(ProgressDialogStyle.Spinner);
                dialog.Show();
                returnCallback(dialog);
            });
        }

        public static void ShowConfirmDialog(Context context, int titleId, int messageId, int confirmId, int cancelId, Action onConfirm = null,
            Action onCancel = null)
        {
            InvokeLooperThread(() =>
            {
                new AlertDialog.Builder(context).SetTitle(titleId).SetMessage(messageId).SetCancelable(true)
                    .SetPositiveButton(confirmId, (sender, args) => onConfirm?.Invoke())
                    .SetNegativeButton(cancelId, (sender, args) => onCancel?.Invoke())
                    .Show();
            });
        }

        public static void ShowAlertDialog(Context context, int titleId, string message, int confirmId, Action onConfirm = null)
        {
            InvokeLooperThread(() =>
            {
                new AlertDialog.Builder(context).SetTitle(titleId).SetMessage(message).SetCancelable(true)
                    .SetPositiveButton(confirmId, (sender, args) => onConfirm?.Invoke())
                    .Show();
            });
        }
        public static void OpenAppSettingsOnPhone(Context context)
        {
            Intent intent = new Intent();
            intent.SetAction("android.settings.APPLICATION_DETAILS_SETTINGS");
            Android.Net.Uri data = Android.Net.Uri.FromParts("package", context.PackageName, (string)null);
            intent.SetData(data);
            context.StartActivity(intent);
        }
        public static void OpenInPlayStore()
        {
            try
            {
                Intent intent = new Intent("android.intent.action.VIEW", Android.Net.Uri.Parse("market://details?id=" + Constants.GamePackageName));
                intent.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                Intent intent = new Intent("android.intent.action.VIEW", Android.Net.Uri.Parse("https://play.google.com/store/apps/details?id=" + Constants.GamePackageName));
                intent.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
            catch (System.Exception ex)
            {
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex);
            }
        }

        public static void OpenTextFile(Context context, string filename)
        {
            Intent intent = new Intent(Intent.ActionView);
            intent.AddCategory(Intent.CategoryDefault);
            Java.IO.File configFile = new Java.IO.File(filename);
            intent.SetDataAndType(Android.Net.Uri.FromFile(configFile), "text/plain");
            intent.AddFlags(ActivityFlags.NewTask);
            try
            {
                context.StartActivity(intent);
            }
            catch (ActivityNotFoundException) { }
        }

        public static string GetConfig(Context context, string key, string defValue)
        {
            ISharedPreferences sp = context.GetSharedPreferences("main_prefs", FileCreationMode.Private);
            return sp.GetString(key, defValue);
        }
        public static void SetConfig(Context context, string key, string value)
        {
            ISharedPreferences sp = context.GetSharedPreferences("main_prefs", FileCreationMode.Private);
            ISharedPreferencesEditor editor = sp.Edit();
            editor.PutString(key, value);
            editor.Apply();
        }
    }
}
