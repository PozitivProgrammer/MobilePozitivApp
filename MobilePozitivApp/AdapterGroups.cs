using System;
using System.Collections.Generic;

using XamarinShortcutBadger;

using Android.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json.Linq;

namespace MobilePozitivApp
{
    public class AdapterGroups : BaseAdapter
    {
        Activity mContext;
        public List<DataGroups> items;

        public AdapterGroups(Activity context) //We need a context to inflate our row view from
          : base()
        {
            this.mContext = context;

            UpdateList();
        }
        public override int Count
        {
            get { return items.Count; }
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];
            var view = (convertView ?? mContext.LayoutInflater.Inflate(Resource.Layout.ListViewGroups, parent, false)) as LinearLayout;

            var imageItem = view.FindViewById(Resource.Id.Image) as ImageView;
            var textTop = view.FindViewById(Resource.Id.Name) as TextView;
            var textBottom = view.FindViewById(Resource.Id.Description) as TextView;
            var textCount = view.FindViewById(Resource.Id.Count) as TextView;

            imageItem.SetImageResource(item.Image);
            textTop.SetText(item.Name, TextView.BufferType.Normal);
            if (item.Description == string.Empty) textBottom.Visibility = ViewStates.Gone;
                else textBottom.SetText(item.Description, TextView.BufferType.Normal);
            textCount.Text = item.ItemCount;
            textCount.Visibility = item.ItemCount == String.Empty ? ViewStates.Gone : ViewStates.Visible;

            return view;
        }
        public DataGroups GetItemAtPosition(int position)
        {
            return items[position];
        }

        public void UpdateList()
        {
            items = new List<DataGroups>();

            DataSetWS dataSetWS = new DataSetWS();

            string AllowGroupsResult = string.Empty;
            try
            {
                AllowGroupsResult = dataSetWS.GetGroups();
            }
            catch (Exception e)
            {
                mContext.RunOnUiThread(() => {
                    Toast.MakeText(mContext, e.Message, ToastLength.Long).Show();
                    return;
                });
                return;
            }

            JObject jsonResult = JObject.Parse(AllowGroupsResult);
            
            items.Add(new DataGroups() { Name = "Главная страница", Description = "Новости", Image = Resource.Drawable.Main, Ref = "" , ItemCount = String.Empty});
            items.Add(new DataGroups() { Name = "Сообщения", Description = "", Image = Resource.Drawable.Messages, Ref = "", ItemCount = String.Empty });
            int totalItemCount = 0;
            foreach (JObject Group in jsonResult["Data"])
            {
                string Name = (string)(Group.Property("Name").Value);
                string Ref = (string)(Group.Property("Ref").Value);
                string ItemCount = Group.Property("ItemCount") == null ? String.Empty : (string)(Group.Property("ItemCount").Value);
                totalItemCount += Group.Property("ItemCount") == null ? 0 : Convert.ToInt16((string)(Group.Property("ItemCount").Value)); 
                int imageId = 0;
                switch (Name)
                {
                    case "Задачи":
                        imageId = Resource.Drawable.Tasks;
                        break;
                    case "Справочники":
                        imageId = Resource.Drawable.Directories;
                        break;
                    case "Документы":
                        imageId = Resource.Drawable.Documents;
                        break;
                    case "Обработки":
                        imageId = Resource.Drawable.DataProcessor;
                        break;
                    case "Отчеты":
                        imageId = Resource.Drawable.Reports;
                        break;
                    case "Прочее":
                        imageId = Resource.Drawable.Other;
                        break;
                }
                items.Add(new DataGroups() { Name = Name, Description = "", Image = imageId, Ref = Ref, ItemCount = ItemCount });
            }
            ShortcutBadger.ApplyCount(mContext, totalItemCount);
        }
    }
}