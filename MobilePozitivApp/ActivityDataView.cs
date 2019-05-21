using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using ZXing.Mobile;

using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using System.Text.RegularExpressions;


namespace MobilePozitivApp
{

    
    [Activity(Label = "ActivityElementsList", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ActivityDataView : AppCompatActivity
    {
        private LinearLayout mLinearLayout;
        private SupportToolbar mToolbar;
        private ProgressDialog mProgressDialog;
        private IMenu mMenu;

        private string mRefListMod;
        private string mRef;
        private string mName;

        private int mIdDataSelect;
        private EditText mEditTextDataSelect;        
        

        private bool mSaveButtonVisible = false;
        private bool mDialogLoading = false;

        private string lastButtonName;
        private string lastButtonValue;
        private int mWebViewZoom;
        private int mWebViewTop;
        private int mWebViewLeft;

        Dictionary<long, ElementData> mElements = new Dictionary<long, ElementData>();
        private long FindElementByKey(string pKey)
        {
            foreach (KeyValuePair<long, ElementData> item in mElements)
            {
                if (item.Value.Key == pKey)
                {
                    return item.Key;
                }
            }
            return -1;
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityDataView);
           
            mRefListMod = Intent.GetStringExtra("reflistmod");
            mRef = Intent.GetStringExtra("ref");
            mName = Intent.GetStringExtra("name");

            mLinearLayout = FindViewById<LinearLayout>(Resource.Id.Content);

            mToolbar = FindViewById<SupportToolbar>(Resource.Id.Toolbar);
            mToolbar.Title = mName;
            SetSupportActionBar(mToolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            mProgressDialog = new ProgressDialog(this);
            mProgressDialog.Indeterminate = true;
            mProgressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            mProgressDialog.Max = 100;
            mProgressDialog.SetCancelable(false);

            GetDataFromWS();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.action_save_dataView:
                    SetDataByRef("Сохранить", "save");
                    return true;
                case Resource.Id.action_refresh_dataView:    
                    Recreate();
                    return true;
                case Resource.Id.action_help_dataView:
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            mMenu = menu;
            MenuInflater.Inflate(Resource.Menu.action_menu_view_element, menu);
            IMenuItem mMenuItem;
            if (mSaveButtonVisible == false)
            {
                mMenuItem = menu.FindItem(Resource.Id.action_save_dataView);
                mMenuItem.SetVisible(false);
            }
            //mMenuItem = menu.FindItem(Resource.Id.action_refresh_dataView);
            //mMenuItem.SetVisible(false);
            return base.OnCreateOptionsMenu(menu);
        }

        private void GetDataFromWS()
        {
            if (AppVariable.Variable.isOnline != true) return;
            DataSetWS dataSetWS = new DataSetWS();
            dataSetWS.GetDataCompleted += DataSetWS_GetDataCompleted;
            dataSetWS.GetDataAsync(mRefListMod, mRef, AppVariable.Variable.getSessionParametersJSON());
         
            mProgressDialog.SetMessage("Загрузка...");
            mProgressDialog.Show();
        }

        private void DataSetWS_GetDataCompleted(object sender, MobileAppPozitiv.GetDataCompletedEventArgs e)
        {
            mProgressDialog.Dismiss();

            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    Toast.MakeText(this, "Отменено", ToastLength.Short).Show();
                    Finish();
                }
                else
                {
                    ParseData(e.Result);
                    if (mMenu != null)
                    {
                        mMenu.Clear();
                        OnCreateOptionsMenu(mMenu);
                    }
                }
            }
            else
            {
                Toast.MakeText(this, e.Error.Message, ToastLength.Long).Show();
                Finish();
            }
        }

        private void ParseData(string JSONString)
        {
            

            JObject jsonResult = JObject.Parse(JSONString);

            //Добавляем таблицу в интерфейс
            TableLayout mTableLayout = new TableLayout(this);
            var param = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            mTableLayout.SetColumnStretchable(1, true);
            mTableLayout.SetColumnShrinkable(1, true);
            mLinearLayout.AddView(mTableLayout, param);

            foreach (JObject Element in jsonResult["Data"])
            {
                CreatElement(Element, mTableLayout);
            }
        }

        private void CreatElement(JObject jElement, TableLayout mTableLayout)
        {
            JValue jReadOnly;
            JObject jParam;
            JValue jSubType;

            var jType = jElement["Type"];
            var jValue = jElement["Value"];

            long Id = (long)((JValue)jElement["Id"]).Value;
            string Name = (string)((JValue)jElement["Name"]).Value;
            

            string Description = (string)((JValue)jElement["Description"]).Value;

            TableRow mNewRow = new TableRow(this);
            mTableLayout.AddView(mNewRow);

            TextView mTextViewDesc = new TextView(this);
            if (Description == "")
            {
                mTextViewDesc.Visibility = ViewStates.Invisible;
            }
            else
            {
                mTextViewDesc.Text = Description + ":";
                mTextViewDesc.SetPadding(10, 0, 0, 0);
            }


            string Key = "";
            if (jElement.ContainsKey("Key"))
            {
                Key = (string)((JValue)jElement["Key"]).Value;
                if (Key.Length > 0)
                {
                    mTextViewDesc.SetTextColor(new Android.Graphics.Color(0, 0, 200));
                }
            }

            mNewRow.AddView(mTextViewDesc);

            if (jType.Type != JTokenType.Array) //елси это одиночный тип
            {
                string Type = (string)((JValue)jType).Value;
                switch (Type)
                {
                    case "button":
                        mTextViewDesc.Visibility = ViewStates.Gone;
                        string buttonValue = (string)((JValue)jValue).Value;

                        if (buttonValue == "save")
                        {
                            mSaveButtonVisible = true;
                        }
                        else
                        {
                            Button nButton = new Android.Support.V7.Widget.AppCompatButton(this) { Id = (int)Id };                                                  

                            nButton.Text = Description;
                            nButton.Click += (sender, args) =>
                            {
                                lastButtonName = Name;
                                lastButtonValue = buttonValue;
                                SetDataByRef(Name, buttonValue);
                            };
                            mLinearLayout.AddView(nButton);
                            
                            
                        }
                        break;
                    case "textview":
                        TextView nTextView = new TextView(this) { Id = (int)Id };
                        nTextView.SetTextSize(Android.Util.ComplexUnitType.Sp, 18); 
                        nTextView.Text = (string)((JValue)jValue).Value;
                        mNewRow.AddView(nTextView);

                        if (jElement.Property("Param") != null)
                        {
                            jParam = (JObject)jElement["Param"];
                            if (jParam.Property("SubType") != null)
                            {
                                jSubType = (JValue)jParam["SubType"];
                                switch ((string)jSubType.Value)
                                {
                                    case "phone":
                                        ImageButton nButtonOpenFile = new ImageButton(this) { Id = (int)Id };
                                        nButtonOpenFile.SetImageResource(Resource.Drawable.ic_action_call);
                                        nButtonOpenFile.Click += (bsender, bargs) => {
                                            var uri = Android.Net.Uri.Parse("tel:" + (string)((JValue)jValue).Value);
                                            Intent callIntent = new Intent(Intent.ActionDial, uri);
                                            StartActivity(callIntent);
                                        };
                                        mNewRow.AddView(nButtonOpenFile);
                                        break;
                                }
                            }
                        }                        
                        break;
                    case "textedit":
                        string TextEditValue = (string)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() {Name = Name, Data = TextEditValue });

                        jSubType = (JValue)jElement["Param"]["SubType"];
                        jReadOnly = (JValue)jElement["Param"]["ReadOnly"];
                        JValue jMultiline = (JValue)jElement["Param"]["Multiline"];                        

                        EditText nEditText = new EditText(this) { Id = (int)Id };
                        switch ((string)jSubType.Value)
                        {
                            case "text":
                                if ((bool)jMultiline.Value)
                                {
                                    nEditText.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextFlagMultiLine;
                                    nEditText.SetMinLines(1);
                                    nEditText.SetMaxLines(3);
                                    nEditText.SetScroller(new Scroller(this));
                                    nEditText.VerticalScrollBarEnabled = true;
                                }
                                else
                                {
                                    nEditText.InputType = Android.Text.InputTypes.ClassText;
                                }
                                nEditText.SetSingleLine(false);
                                break;
                            case "number":
                                nEditText.InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal | Android.Text.InputTypes.NumberFlagSigned;
                                break;
                            case "email":
                                nEditText.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationEmailAddress;
                                break;
                            case "password":
                                nEditText.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPassword;
                                break;
                            case "phone":
                                nEditText.InputType = Android.Text.InputTypes.ClassPhone;
                                break;
                        }
                        nEditText.Text = TextEditValue;
                        if ((bool)jReadOnly.Value)
                        {
                            nEditText.Focusable = false;
                        }
                       
                        nEditText.AfterTextChanged += (sender, args) =>
                        {
                            mElements[Id].Data = nEditText.Text;
                        };
                        mNewRow.AddView(nEditText);
                        break;
                    case "dateedit":
                        DateTime DateEditValue = (DateTime)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() {Name = Name, Data = DateEditValue });

                        EditText nTextDateEdit = new EditText(this) { Id = (int)Id };
                        nTextDateEdit.Focusable = false;
                        nTextDateEdit.Text = DateEditValue.ToShortDateString();
                        mNewRow.AddView(nTextDateEdit);

                        ImageButton nButtonDateEdit = new ImageButton(this) { Id = (int)Id };
                        nButtonDateEdit.SetImageResource(Resource.Drawable.ic_action_edit);
                        nButtonDateEdit.Click += (bsender, bargs) => {
                            if (mDialogLoading == false)
                            {
                                mDialogLoading = true;
                                DateTime currently = (mElements[Id].Data == null) ? DateTime.Now : mElements[Id].Data;
                                DatePickerDialog ddialog = new DatePickerDialog(this, (dsender, dargs) => {
                                    string strDate = ((DateTime)dargs.Date).ToShortDateString();
                                    nTextDateEdit.Text = strDate;
                                    mElements[Id].Data = (DateTime)dargs.Date;
                                    mDialogLoading = false;
                                }, currently.Year, currently.Month - 1, currently.Day);
                                ddialog.Show();
                                ddialog.CancelEvent += (dsender, dargs) =>
                                {
                                    mDialogLoading = false;
                                };                                
                            }
                        };
                        mNewRow.AddView(nButtonDateEdit);
                        break;
                    case "timeedit":
                        DateTime TimeEditValue = (DateTime)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() {Name = Name, Data = TimeEditValue });

                        EditText nTextTimeEdit = new EditText(this) { Id = (int)Id };
                        nTextTimeEdit.Focusable = false;
                        nTextTimeEdit.Text = TimeEditValue.ToShortTimeString();
                        mNewRow.AddView(nTextTimeEdit);

                        ImageButton nButtonTimeEdit = new ImageButton(this) { Id = (int)Id };
                        nButtonTimeEdit.SetImageResource(Resource.Drawable.ic_action_edit);
                        nButtonTimeEdit.Click += (bsender, bargs) => {
                            if (mDialogLoading == false)
                            {
                                mDialogLoading = true;
                                DateTime tcurrently = (mElements[Id].Data == null) ? DateTime.Now : mElements[Id].Data;
                                TimePickerDialog tdialog = new TimePickerDialog(this, (tsender, targs) =>
                                {
                                    string strTime = String.Format("{0:d2}", (int)targs.HourOfDay) + ":" + String.Format("{0:d2}", (int)targs.Minute);
                                    nTextTimeEdit.Text = strTime;
                                    mElements[Id].Data = DateTime.Parse("01.01.0001 " + strTime + ":00");
                                    mDialogLoading = false;
                                }, tcurrently.Hour, tcurrently.Minute, true);
                                tdialog.Show();
                                tdialog.CancelEvent += (dsender, dargs) =>
                                {
                                    mDialogLoading = false;
                                };
                            }
                        };
                        mNewRow.AddView(nButtonTimeEdit);

                        break;
                    case "checkbox":
                        bool CheckBoxValue = (bool)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() { Name = Name, Data = CheckBoxValue });

                        jReadOnly = (JValue)jElement["Param"]["ReadOnly"];

                        CheckBox nCheckBox = new CheckBox(this) { Id = (int)Id };
                        if ((bool)jReadOnly.Value) nCheckBox.Enabled = false;
                        nCheckBox.Checked = CheckBoxValue;
                        nCheckBox.Click += (csender, cargs) =>
                        {
                            mElements[Id].Data = nCheckBox.Checked;
                        };
                        mNewRow.AddView(nCheckBox);
                        break;
                    case "datalist":
                        string DataListValue = (string)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() {Name = Name, Data = DataListValue });

                        EditText nTextDataList = new EditText(this) { Id = (int)Id };
                        nTextDataList.Focusable = false;
                        mNewRow.AddView(nTextDataList);

                        ImageButton nButtonDataList = new ImageButton(this) { Id = (int)Id };
                        nButtonDataList.SetImageResource(Resource.Drawable.ic_action_edit);

                        PopupMenu DataListMenu = new PopupMenu(this, nButtonDataList);
                        int menuItemId = 0;
                        Dictionary<int, string> mElementsDropMenu = new Dictionary<int, string>();
                        foreach (var jDataListValue in jElement["DataArray"])
                        {
                            string PresentMenuItem = String.Empty;
                            string DataMenuItem = String.Empty;
                            if (jDataListValue.Type == JTokenType.String)
                            {
                                PresentMenuItem = (string)((JValue)jDataListValue).Value;
                                DataMenuItem = PresentMenuItem;
                            }
                            else
                            {
                                PresentMenuItem = ((JObject)jDataListValue).Property("Present") != null ? (string)(((JObject)jDataListValue).Property("Present")).Value : (string)(((JObject)jDataListValue).Property("Data")).Value;
                                DataMenuItem = (string)(((JObject)jDataListValue).Property("Data")).Value;
                            }
                            DataListMenu.Menu.Add(0, menuItemId, Menu.None, PresentMenuItem);
                            mElementsDropMenu.Add(menuItemId, DataMenuItem);
                            menuItemId++;

                            if (DataListValue == DataMenuItem)
                                nTextDataList.Text = PresentMenuItem;
                        }

                        DataListMenu.MenuItemClick += (psender, pargs) =>
                        {
                            string selectedData = pargs.Item.TitleFormatted.ToString();
                            nTextDataList.Text = selectedData;
                            mElements[Id].Data = mElementsDropMenu[pargs.Item.ItemId];
                        };
                        
                        nButtonDataList.Click += (bsender, bargs) => {                            
                            DataListMenu.Show();
                        };
                        mNewRow.AddView(nButtonDataList);
                        break;
                    case "data":
                        string dataValue = (string)((JValue)jValue["Present"]).Value;
                        string dataRef = (string)((JValue)jValue["Ref"]).Value;
                        string dataType = (string)((JValue)jElement["DataType"]).Value;

                        mElements.Add(Id, new ElementData() { Name = Name, Data = dataRef });

                        EditText nTextDataEdit = new EditText(this) { Id = (int)Id };
                        nTextDataEdit.Focusable = false;
                        nTextDataEdit.Text = dataValue;
                        mNewRow.AddView(nTextDataEdit);

                        ImageButton nButtonDataEdit = new ImageButton(this) { Id = 1000 + (int)Id };
                        nButtonDataEdit.SetImageResource(Resource.Drawable.ic_action_edit);
                        nButtonDataEdit.Click += (bsender, bargs) => {
                            mIdDataSelect = (int)Id;
                            Intent intentData = new Intent(this, typeof(ActivityDataList));
                            mEditTextDataSelect = nTextDataEdit;
                            intentData.PutExtra("ref", dataType);
                            intentData.PutExtra("name", Description);
                            intentData.PutExtra("selected", true);
                            StartActivityForResult(intentData, 1);
                        };
                        mNewRow.AddView(nButtonDataEdit);
                        
                        break;
                    case "multiselect":
                        //````в мобильном приложении техники должны указывать не только поставляемое оборудование, но и забираемое оборудование.:0
                        
                        string multi_dataType = (string)((JValue)jElement["DataType"]).Value;
                        string multi_value = "";

                        
                        var curViewVal = jElement["Value"]["viewVal"];
                        var curWorkVal = jElement["Value"]["workVal"];
                        if (curWorkVal != null)
                        {
                            multi_value = (string)((JValue)curWorkVal).Value;
                        }

                                                

                        mElements.Add(Id, new ElementData() { Name = Name, Data = multi_value });

                        EditText multi_textEdit = new EditText(this) { Id = (int)Id };
                        multi_textEdit.Focusable = false;
                        if (curViewVal != null)
                        {
                            multi_textEdit.Text = (string)((JValue)curViewVal).Value;
                        }
                        mNewRow.AddView(multi_textEdit);

                        ImageButton multi_nButtonDataEdit = new ImageButton(this) { Id = 1000 + (int)Id };
                        multi_nButtonDataEdit.SetImageResource(Resource.Drawable.ic_action_edit);
                        multi_nButtonDataEdit.Click += (bsender, bargs) =>
                        {
                            mIdDataSelect = (int)Id;
                            Intent intentData = new Intent(this, typeof(ToggleList));
                            mEditTextDataSelect = multi_textEdit;
                            intentData.PutExtra("ref", multi_dataType);
                            intentData.PutExtra("name", Description);
                            intentData.PutExtra("cursel", mElements[Id].Data);

                            StartActivityForResult(intentData, 1);
                        };
                        
                        mNewRow.AddView(multi_nButtonDataEdit);
                        

                        break;
                    case "file":
                        string filesValue = (string)((JValue)jValue["Present"]).Value;
                        string filesRef = (string)((JValue)jValue["Ref"]).Value;                        

                        TextView nTextViewFile = new TextView(this) { Id = (int)Id };
                        nTextViewFile.SetTextSize(Android.Util.ComplexUnitType.Sp, 18);
                        nTextViewFile.Text = filesValue;
                        mNewRow.AddView(nTextViewFile);

                        ImageButton nButtonTextView = new ImageButton(this) { Id = (int)Id };
                        nButtonTextView.SetImageResource(Resource.Drawable.ic_action_attachment);
                        nButtonTextView.Click += (bsender, bargs) => {
                            Intent intentFiles = new Intent(this, typeof(ActivityFiles));
                            intentFiles.PutExtra("ref", filesRef);
                            intentFiles.PutExtra("name", filesValue);
                            StartActivity(intentFiles);
                        };
                        mNewRow.AddView(nButtonTextView);
                        break;
                    case "barcode":
                        string BarCodeEditValue = (string)((JValue)jValue).Value;

                        mElements.Add(Id, new ElementData() { Name = Name, Data = BarCodeEditValue });

                        EditText nTextBarCodeEdit = new EditText(this) { Id = (int)Id };
                        nTextBarCodeEdit.Text = BarCodeEditValue;
                        mNewRow.AddView(nTextBarCodeEdit);

                        ImageButton nButtonBarCodeEdit = new ImageButton(this) { Id = (int)Id };
                        nButtonBarCodeEdit.SetImageResource(Resource.Drawable.ic_action_camera);
                        nButtonBarCodeEdit.Click += async (bsender, bargs) => {
                            /*if (mDialogLoading == false)
                            {
                                mDialogLoading = true;
                                DateTime currently = (mElements[Id].Data == null) ? DateTime.Now : mElements[Id].Data;
                                DatePickerDialog ddialog = new DatePickerDialog(this, (dsender, dargs) => {
                                    string strDate = ((DateTime)dargs.Date).ToShortDateString();
                                    nTextDateEdit.Text = strDate;
                                    mElements[Id].Data = (DateTime)dargs.Date;
                                    mDialogLoading = false;
                                }, currently.Year, currently.Month - 1, currently.Day);
                                ddialog.Show();
                                ddialog.CancelEvent += (dsender, dargs) =>
                                {
                                    mDialogLoading = false;
                                };
                            }*/

                            MobileBarcodeScanner.Initialize(Application);
                            var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                            var result = await scanner.Scan();
                            if (result != null)
                            { 
                                nTextBarCodeEdit.Text = result.Text;
                                mElements[Id].Data = result.Text;
                            }
                        };
                        mNewRow.AddView(nButtonBarCodeEdit);
                        break;
                    default:

                        break;
                }
            }
            else //если это массив типов
            {

            }
        }

        protected override void OnActivityResult(int requestCode, [Android.Runtime.GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                if (requestCode == 1)
                {
                    mElements[mIdDataSelect].Data = data.GetStringExtra("ref");
                    mEditTextDataSelect.Text = data.GetStringExtra("name");
                }
                else if (requestCode == 2)
                {
                    mWebViewZoom = data.GetIntExtra("zoom", 0);
                    mWebViewTop = data.GetIntExtra("top", 0);
                    mWebViewLeft = data.GetIntExtra("left", 0);
                    SetDataByRef(lastButtonName, lastButtonValue);
                }
            }
        }

        private void SetDataByRef(string Name, string Data)
        {
            mElements.Add(0, new ElementData() {Name = Name, Data = Data });
            string output = JsonConvert.SerializeObject(mElements);
            DataSetWS dataSetWS = new DataSetWS();
            dataSetWS.SetDataCompleted += DataSetWS_SetDataCompleted;
            dataSetWS.SetDataAsync(mRefListMod, mRef, output, AppVariable.Variable.getSessionParametersJSON());
            mElements.Remove(0);

            mProgressDialog.SetMessage("Отправка...");
            mProgressDialog.Show();
        }

        private void DataSetWS_SetDataCompleted(object sender, MobileAppPozitiv.SetDataCompletedEventArgs e)
        {
            mProgressDialog.Dismiss();

            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    Toast.MakeText(this, "Отменено", ToastLength.Short).Show();
                }
                else
                {
                    JObject jsonResult = JObject.Parse(e.Result);
                    JValue jResult = (JValue)jsonResult["Result"];
                    JValue jMessage = (JValue)jsonResult["Message"];

                    string Result = (string)jResult.Value;
                    string Message = (string)jMessage.Value;
                    Toast.MakeText(this, Message, ToastLength.Long).Show();

                    bool Close = false;
                    bool Refresh = false;
                    switch (Result)
                    {
                        case "Completed":
                            break;
                        case "CompletedClose":
                            Close = true;
                            Refresh = true;
                            break;
                        case "ReportGenerated":
                            JValue jDataBase64 = (JValue)jsonResult["Data"];
                            Intent intent = new Intent(this, typeof(ActivityWebView));
                            intent.PutExtra("name", mName);
                            intent.PutExtra("htmlbase64", (string)jDataBase64.Value);
                            intent.PutExtra("zoom", mWebViewZoom);
                            intent.PutExtra("top", mWebViewTop);
                            intent.PutExtra("left", mWebViewLeft);
                            StartActivityForResult(intent, 2);
                            break;
                        case "DataRecived":
                            break;
                        case "Error":
                            break;
                        case "ErrorClose":
                            Close = true;
                            break;
                    }
                    if (Close)
                    {
                        if (Refresh)
                        {
                            SetResult(Android.App.Result.Ok);
                        }
                        else
                        {
                            SetResult(Android.App.Result.Canceled);
                        }
                        Finish();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, e.Error.Message, ToastLength.Long).Show();
            }                 
        }
    }

    public class ElementData
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public dynamic Data { get; set; }
    }
}