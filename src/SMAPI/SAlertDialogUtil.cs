using System;
using Android.App;
using Android.OS;
using Java.Lang;
using Exception = System.Exception;

namespace StardewModdingAPI
{
    internal static class SAlertDialogUtil
    {
        internal enum ActionType
        {
            POSITIVE, NEGATIVE
        }
        public static void AlertMessage(string message, string title = "Error",
            string positive = null, string negative = null,
            Action<ActionType> callback = null)
        {
            try
            {
                Handler handler = new Handler((msg) => throw new RuntimeException());
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(SMainActivity.Instance)
                    .SetTitle(title)
                    .SetMessage(message)
                    .SetCancelable(false);
                if (positive != null)
                {
                    dialogBuilder.SetPositiveButton(positive, (sender, args) =>
                    {
                        handler.SendEmptyMessage(0);
                        callback?.Invoke(ActionType.POSITIVE);
                    });
                }

                if (negative != null)
                {
                    dialogBuilder.SetNegativeButton(negative, (sender, args) =>
                    {
                        handler.SendEmptyMessage(0);
                        callback?.Invoke(ActionType.NEGATIVE);
                    });
                }
                else if (positive == null)
                {
                    dialogBuilder.SetPositiveButton("OK", (sender, args) =>
                    {
                        handler.SendEmptyMessage(0);
                        callback?.Invoke(ActionType.POSITIVE);
                    });
                }

                Dialog dialog = dialogBuilder.Create();
                if (!SMainActivity.Instance.IsFinishing)
                {
                    dialog.Show();
                    try
                    {
                        Looper.Prepare();
                    }
                    catch (Exception)
                    {
                    }

                    Looper.Loop();
                }
            }
            catch(Exception)
            {
                // ignored
            }
        }
    }
}
