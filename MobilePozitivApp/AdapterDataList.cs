using System;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace MobilePozitivApp
{
    public class AdapterDataList : BaseAdapter
    {
        Activity mContext;
        public List<DataDataList> items;
        public List<DataDataList> itemsOrig;
        public string mRef;
        public string mGroupRef;
        

        public AdapterDataList(Activity Context, string Ref, string GroupRef, string PrevParentRef, string searchStr = null) //We need a context to inflate our row view from
          : base()
        {
            mContext = Context;
            mRef = Ref;
            mGroupRef = GroupRef;

            UpdateList();

            if (searchStr != null && searchStr != string.Empty)
            {
                FilterList(searchStr);
            }
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
            var view = (convertView ?? mContext.LayoutInflater.Inflate(Resource.Layout.ListViewData, parent, false)) as LinearLayout;

            var imageItem = view.FindViewById(Resource.Id.Image) as ImageView;
            var textTop = view.FindViewById(Resource.Id.Name) as TextView;
            var textBottom = view.FindViewById(Resource.Id.Description) as TextView;

            imageItem.SetImageResource(item.Image);
            textTop.SetText(item.Name, TextView.BufferType.Normal);
            if (item.Description == string.Empty) textBottom.Visibility = ViewStates.Gone;
                else textBottom.SetText(item.Description, TextView.BufferType.Normal);

            return view;
        }

        public DataDataList GetItemAtPosition(int position)
        {
            return items[position];
        }

        public void UpdateList()
        {
            itemsOrig = new List<DataDataList>();
            items = itemsOrig;

            DataSetWS dataSetWS = new DataSetWS();            

            string DatalistResult = string.Empty;

            try
            {
                if (mGroupRef == "")
                {
                    DatalistResult = dataSetWS.GetList(mRef, AppVariable.Variable.getSessionParametersJSON());
                }
                else
                {
                    DatalistResult = dataSetWS.GetParentList(mRef, mGroupRef, AppVariable.Variable.getSessionParametersJSON());
                }
            }
            catch (Exception e)
            {
                mContext.RunOnUiThread(() => {
                    Toast.MakeText(mContext, e.Message, ToastLength.Long).Show();
                    mContext.Finish();                    
                });
                return;
            }

            JObject jsonResult = JObject.Parse(DatalistResult);
            
            if (jsonResult.Property("Error") == null)
            {                
                foreach (JObject Group in jsonResult["Data"])
                {
                    JValue Name = (JValue)Group["Name"];
                    JValue Ref = (JValue)Group["Ref"];
                    JValue lIsGroup = (JValue)Group["IsGroup"];
                    JValue Description = (JValue)Group["Description"];

                    int imgeID = Resource.Drawable.ic_document;
                    if (Group.Property("Image") != null)
                    { 
                        JValue Image = (JValue)Group["Image"];
                        switch ((string)Image.Value)
                        {
                            case "Document":
                                imgeID = Resource.Drawable.ic_document;
                            break;
                            case "DocumentDeleted":
                                imgeID = Resource.Drawable.ic_document_deleted;
                            break;
                            case "DocumentAccept":
                                imgeID = Resource.Drawable.ic_document_accept;
                            break;
                            case "Folder":
                                imgeID = Resource.Drawable.ic_folder;
                            break;
                        }
                    }

                    itemsOrig.Add(new DataDataList() { Name = (string)Name.Value, Description = (string)Description.Value, Image = imgeID, Ref = (string)Ref.Value, IsGroup = lIsGroup != null ? (bool)lIsGroup.Value : false });
                }
                items = itemsOrig;
            }
            else
            {
                mContext.RunOnUiThread(() => {
                    Toast.MakeText(mContext, (string)jsonResult.Property("Error").Value, ToastLength.Long).Show();
                    mContext.Finish();
                });
            }
        }

        public void FilterList(string streSearch)
        {
            /*items = (from mItem in itemsOrig
                     where mItem.Name.IndexOf(streSearch, 0, StringComparison.OrdinalIgnoreCase) != -1 || mItem.Description.IndexOf(streSearch, 0, StringComparison.OrdinalIgnoreCase) != -1
                     select mItem).ToList<DataDataList>();
                     */
            streSearch = streSearch.Replace("\\", @"");
            streSearch = streSearch.Replace("^", @"\^");
            streSearch = streSearch.Replace("$", @"\$");
            streSearch = streSearch.Replace(".", @"\.");
            streSearch = streSearch.Replace("*", @"\*");
            streSearch = streSearch.Replace("+", @"\+");
            streSearch = streSearch.Replace("?", @"\?");

            streSearch = @"[\S\s]*" + streSearch.Replace(" ", @"[\S\s]*");

            items = (from mItem in itemsOrig
                     where new Regex(streSearch, RegexOptions.IgnoreCase).IsMatch(mItem.Name) || new Regex(streSearch, RegexOptions.IgnoreCase).IsMatch(mItem.Description)
                     select mItem).ToList<DataDataList>();
        }
    }
}