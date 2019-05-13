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
using Com.Pgyersdk;
using Com.Pgyersdk.Crash;

namespace ModLoader
{
    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownerShip) : base(handle, ownerShip)
        {
        }
        public override void OnCreate()
        {
            base.OnCreate();
            Pgyer.SetAppId("8bb59a7cd53b363d849b099428ae2a8a");
        }
    }
}
