#if SMAPI_FOR_MOBILE
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
            POSITIVE,
            NEGATIVE
        }

        public static void AlertMessage(string message, string title = "Error",
            string positive = null, string negative = null,
            Action<ActionType> callback = null)
        {
            try
            {
                SMainActivity.Instance.RunOnUiThread(() => SAlertDialogUtil.ShowDialog(message, title, positive, negative, callback));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void ShowDialog(string message, string title, string positive, string negative, Action<ActionType> callback)
        {
            try
            {
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(SMainActivity.Instance)
                    .SetTitle(title)
                    .SetMessage(message)
                    .SetCancelable(false);
                if (positive != null)
                {
                    dialogBuilder.SetPositiveButton(positive, (sender, args) =>
                    {
                        callback?.Invoke(ActionType.POSITIVE);
                    });
                }

                if (negative != null)
                {
                    dialogBuilder.SetNegativeButton(negative, (sender, args) =>
                    {
                        callback?.Invoke(ActionType.NEGATIVE);
                    });
                }
                else if (positive == null)
                {
                    dialogBuilder.SetPositiveButton("OK", (sender, args) =>
                    {
                        callback?.Invoke(ActionType.POSITIVE);
                    });
                }

                Dialog dialog = dialogBuilder.Create();
                if (!SMainActivity.Instance.IsFinishing)
                {
                    dialog.Show();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
#endif
