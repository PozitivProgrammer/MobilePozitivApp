using System;
using System.Threading.Tasks;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Android.Support.V4.Widget;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using Android.Views.InputMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MobilePozitivApp
{
    [Activity(Label = "ActivityElementsList", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ToggleList : AppCompatActivity
    {
        private LinearLayout mLinearLayout;
        private SupportToolbar mToolbar;

        private IMenu mMenu;
        private string mRef;
        private string mName;

        private string mCursel;
        List<codeRow> curselData = new List<codeRow>();

        public List<DataDataList> items;
        Dictionary<long, TextView> mElements = new Dictionary<long, TextView>();


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityDataView);

            mLinearLayout = FindViewById<LinearLayout>(Resource.Id.Content);
            mRef = Intent.GetStringExtra("ref");
            mName = Intent.GetStringExtra("name");
            mCursel = Intent.GetStringExtra("cursel");

            curSelParse();

            mToolbar = FindViewById<SupportToolbar>(Resource.Id.Toolbar);
            mToolbar.Title = mName;
            SetSupportActionBar(mToolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            

            UpdateList();
        }
        
        

        //Вызывается при закрытии формы редактирования
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                UpdateList();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }




        



        private void plusButtonClicked(object sender, EventArgs e)
        {
            long curId = ((ImageButton)sender).Id;
            mElements[curId - 1000].Text = (int.Parse(mElements[curId - 1000].Text) + 1).ToString();
        }


        private void minusButtonClicked(object sender, EventArgs e)
        {
            long curId = ((ImageButton)sender).Id;
            if (mElements[curId - 2000].Text != "0")
                mElements[curId - 2000].Text = (int.Parse(mElements[curId - 2000].Text) - 1).ToString();
            
        }




        private void clearEquip(object sender, EventArgs e)
        {
            
            for (var i = 0; i < items.Count; i++)
            {
                mElements[i+2].Text = "0";
            }
            
            
        }


        private void finishEquip(object sender, EventArgs e)
        {
            var tookEquipCodes = "";
            var tookEquipNames = "";
            for (var i = 0; i < items.Count; i++)
            {
                if (mElements[i + 2].Text != "0")
                {
                    tookEquipCodes += "," + items[i].Ref.Replace(",", "").Replace(":", "") + ":" + mElements[i + 2].Text;
                    tookEquipNames += ", " + items[i].Name.Replace(",", "").Replace(":", "") + ": " + mElements[i + 2].Text;
                }
                
            }
            if (tookEquipCodes.Length > 0)
                tookEquipCodes = tookEquipCodes.Substring(1);
            if (tookEquipNames.Length > 0)
                tookEquipNames = tookEquipNames.Substring(2);

            


            Intent intent = new Intent();
            intent.PutExtra("ref", tookEquipCodes);
            intent.PutExtra("name", tookEquipNames);
            SetResult(Result.Ok, intent);
            Finish();

        }



        private void UpdateList()
        {


            



            items = new List<DataDataList>();


            //извлечение данных из 1с   начало
            var mContext = this;

            DataSetWS dataSetWS = new DataSetWS();

            string DatalistResult = string.Empty;

            try
            {
                DatalistResult = dataSetWS.GetList(mRef, AppVariable.Variable.getSessionParametersJSON());
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
                    Newtonsoft.Json.Linq.JValue Name = (Newtonsoft.Json.Linq.JValue)Group["Name"];
                    Newtonsoft.Json.Linq.JValue Ref = (Newtonsoft.Json.Linq.JValue)Group["Ref"];
                    Newtonsoft.Json.Linq.JValue Description = (Newtonsoft.Json.Linq.JValue)Group["Description"];

                    

                    items.Add(new DataDataList() { Name = (string)Name.Value, Description = (string)Description.Value, Ref = (string)Ref.Value });
                }
                
            }
            else
            {
                mContext.RunOnUiThread(() => {
                    Toast.MakeText(mContext, (string)jsonResult.Property("Error").Value, ToastLength.Long).Show();
                    mContext.Finish();
                });
            }
            //извлечение данных из 1с   конец

            Button nButton = new Android.Support.V7.Widget.AppCompatButton(this) { Id = 0 };
            nButton.Text = "Очистить";
            nButton.Click += clearEquip;
            mLinearLayout.AddView(nButton);

            Button nButton2 = new Android.Support.V7.Widget.AppCompatButton(this) { Id = 1 };
            nButton2.Text = "Выбрать";
            nButton2.Click += finishEquip;
            mLinearLayout.AddView(nButton2);
            

            TableLayout mTableLayout = new TableLayout(this);
            var param = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            mTableLayout.SetColumnStretchable(0, true);
            mTableLayout.SetColumnShrinkable(0, true);        
            mLinearLayout.AddView(mTableLayout, param);




            var curId = 2;
            foreach (var itt in items)
            {
                TableRow mNewRow = new TableRow(this);
                mNewRow.SetGravity(GravityFlags.CenterVertical);
                mTableLayout.AddView(mNewRow);

                TextView mTextViewDesc = new TextView(this);
                mTextViewDesc.Text = itt.Name;
                mNewRow.AddView(mTextViewDesc);


                TextView nTextView = new TextView(this) { Id = curId };
                nTextView.SetTextSize(Android.Util.ComplexUnitType.Sp, 18);
                nTextView.Text = getSelItemCount(itt.Ref);
                

                mElements.Add(curId, nTextView);

                mNewRow.AddView(nTextView);
                ImageButton myImageButton1 = new ImageButton(this) { Id = curId + 1000 };
                myImageButton1.SetImageResource(Resource.Drawable.ic_action_new);
                myImageButton1.Click += plusButtonClicked;
                mNewRow.AddView(myImageButton1);

                ImageButton myImageButton2 = new ImageButton(this) { Id = curId + 2000 };
                myImageButton2.SetImageResource(Resource.Drawable.ic_action_minus);
                myImageButton2.Click += minusButtonClicked;
                mNewRow.AddView(myImageButton2);

                curId++;
            }

            
        }


        private string getSelItemCount(string fcode)
        {
            for (var i = 0; i < curselData.Count; i++)
            {
                if (curselData[i].code == fcode)
                {
                    return curselData[i].count;
                }
            }
            return "0";

        }


        private void curSelParse()
        {
            
            string[] curLine = mCursel.Split(',');
            foreach (var elem in curLine)
            {
                string[] curLineSplit = elem.Split(':');

                if (curLineSplit.Length > 1)
                {
                    curselData.Add(new codeRow { code = curLineSplit[0], count = curLineSplit[1] });
                }

                
            }
            
        }




        private struct codeRow
        {
            public string code;
            public string count;
        }


    }

    


}