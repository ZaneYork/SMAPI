using Android.App;
using Android.OS;
using Java.Lang;
using Microsoft.AppCenter.Crashes;

namespace StardewModdingAPI
{
    static class SAlertDialogUtil
    {
        public static void AlertMessage(string message, string title = "Error")
        {
            try
            {
                Handler handler = new Handler((msg) => throw new RuntimeException());
                Dialog dialog = new AlertDialog.Builder(SMainActivity.Instance)
                    .SetTitle(title)
                    .SetMessage(message)
                    .SetCancelable(false)
                    .SetPositiveButton("OK", (senderAlert, arg) => { handler.SendEmptyMessage(0); }).Create();
                if (!SMainActivity.Instance.IsFinishing)
                {
                    dialog.Show();
                    try
                    {
                        Looper.Prepare();
                    }
                    catch { }
                    Looper.Loop();
                }
            }
            catch { }
        }
    }
}
