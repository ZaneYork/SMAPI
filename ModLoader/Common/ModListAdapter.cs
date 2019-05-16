using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using ModLoader.Helper;
using StardewModdingAPI.Framework;

namespace ModLoader.Common
{
    class ModListAdapter : ArrayAdapter<ModInfo>
    {
        private int textViewResourceId;
        public ModListAdapter(Context context, int textViewResourceId, List<ModInfo> mods) : base(context, textViewResourceId, mods)
        {
            this.textViewResourceId = textViewResourceId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ModInfo mod = this.GetItem(position);
            View view = LayoutInflater.From(this.Context).Inflate(this.textViewResourceId, parent, false);
            TextView headText = view.FindViewById<TextView>(Resource.Id.textModName);
            TextView descriptionText = view.FindViewById<TextView>(Resource.Id.textDescription);
            Button buttonAddOrRemove = view.FindViewById<Button>(Resource.Id.buttonAddOrRemove);
            headText.Text = mod.Name;
            descriptionText.Text = mod.Description;
            if (mod.Metadata == null)
            {
                buttonAddOrRemove.Text = this.Context.Resources.GetText(Resource.String.ModInstall);
                headText.SetTextColor(Color.Gray);
            }
            else
            {
                buttonAddOrRemove.Text = this.Context.Resources.GetText(Resource.String.ModRemove);
            }
            buttonAddOrRemove.Click += (sender, args) =>
            {
                if (mod.Metadata == null)
                {
                    Activity1.Instance.InstallMod(mod);
                }
                else
                {
                    Activity1.Instance.RemoveMod(mod);
                }
            };
            return view;
        }
    }
}
