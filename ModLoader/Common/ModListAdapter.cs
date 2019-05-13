using Android.Content;
using Android.Views;
using Android.Widget;
using StardewModdingAPI.Framework;

namespace ModLoader.Common
{
    class ModListAdapter : ArrayAdapter<IModMetadata>
    {
        private int textViewResourceId;
        public ModListAdapter(Context context, int textViewResourceId, IModMetadata[] mods) : base(context, textViewResourceId, mods)
        {
            this.textViewResourceId = textViewResourceId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            IModMetadata mod = this.GetItem(position);
            View view = LayoutInflater.From(this.Context).Inflate(this.textViewResourceId, parent, false);
            TextView headText = view.FindViewById<TextView>(Resource.Id.textModName);
            headText.Text = mod.DisplayName;
            //Button disableButton = view.FindViewById<Button>(Resource.Id.buttonModDisable);
            //if (mod.IsIgnored)
            //{
            //    disableButton.Text = this.Context.Resources.GetText(Resource.String.Enable);
            //}
            //else
            //{
            //    disableButton.Text = this.Context.Resources.GetText(Resource.String.Disable);
            //}
            //disableButton.Click += (sender, args) =>
            //{
            //};
            return view;
        }
    }
}
