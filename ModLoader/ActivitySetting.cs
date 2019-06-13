using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ModLoader.Common;
using static ModLoader.Common.Utils;

namespace ModLoader
{
    [Activity(Label = "ActivitySetting")]
    public class ActivitySetting : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.SetContentView(Resource.Layout.layout_setting);
            CheckBox checkBoxCompat = this.FindViewById<CheckBox>(Resource.Id.checkBoxCompat);
            this.WireConfig(checkBoxCompat, "compatCheck", b => Constants.CompatCheck = b);
            CheckBox checkBoxUpgrade = this.FindViewById<CheckBox>(Resource.Id.checkBoxUpgrade);
            this.WireConfig(checkBoxUpgrade, "upgradeCheck", b => Constants.UpgradeCheck = b);
        }

        private void WireConfig(CheckBox checkBox, string configSection, Action<bool> onChecked)
        {
            if (GetConfig(this, configSection, "true") == "false")
            {
                onChecked(false);
                checkBox.Checked = false;
            }
            else
            {
                onChecked(true);
                checkBox.Checked = true;
            }

            checkBox.Click += (sender, args) =>
            {
                onChecked(checkBox.Checked);
                SetConfig(this, configSection, checkBox.Checked ? "true" : "false");
            };
        }
    }
}
