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
    public class ActivityDataList : AppCompatActivity
    {
        private ListView mDataList;
        private AdapterDataList mAdapterDataList;
        private SupportToolbar mToolbar;
        private SwipeRefreshLayout mSwipeRefreshLayout;
        private FloatingActionButton mFloatingActionButton;
        private LinearLayout mSearchLinearLayout;
        private EditText mSearchEditText;
        private Button mSearchButton;
        private LinearLayout mContainer;
        private Task mTaskSearch;
        private TextView mEemptyList;

        private string mRef;
        private bool mIsTreeSelect;
        private string mName;
        private bool mIsSelectedForm;
        private bool mRead;
        private bool mEdit;
        private bool mDelete;

        private bool mAnimatedDown;
        private bool mIsAnimating;

        private Dialog mDialog;
        private List<string> mListDataSet;
        private int mSelectedItemPosition;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityDataList);

            mRef = Intent.GetStringExtra("ref");
            mIsTreeSelect = Intent.GetBooleanExtra("isTreeSelect",false);
            mName = Intent.GetStringExtra("name");
            mIsSelectedForm = Intent.GetBooleanExtra("selected", false);
            mRead = Intent.GetBooleanExtra("read", false);
            mEdit = Intent.GetBooleanExtra("edit", false);
            mDelete = Intent.GetBooleanExtra("delete", false);

            mContainer = FindViewById<LinearLayout>(Resource.Id.Container);

            mSwipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeLayoutDataList);
            mSwipeRefreshLayout.SetColorSchemeResources(Android.Resource.Color.HoloBlueBright, Android.Resource.Color.HoloBlueDark, Android.Resource.Color.HoloGreenLight, Android.Resource.Color.HoloRedLight);
            mSwipeRefreshLayout.Refresh += mSwipeRefreshLayout_Refresh;

            mDataList = FindViewById<ListView>(Resource.Id.ListView);
            if (mRead || mIsSelectedForm) mDataList.ItemClick += MDataList_ItemClick;
            if (mDelete) mDataList.ItemLongClick += MDataList_ItemLongClick;

            mFloatingActionButton = FindViewById<FloatingActionButton>(Resource.Id.Fab);
            mFloatingActionButton.Visibility = mEdit ? ViewStates.Visible : ViewStates.Invisible;
            mFloatingActionButton.Click += Fab_Click;

            mEemptyList = FindViewById<TextView>(Resource.Id.ViewEmpty);

            mToolbar = FindViewById<SupportToolbar>(Resource.Id.Toolbar);
            mToolbar.Title = mName;
            SetSupportActionBar(mToolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            mSearchLinearLayout = FindViewById<LinearLayout>(Resource.Id.Search);
            mSearchEditText = FindViewById<EditText>(Resource.Id.etSearch);
            mSearchButton = FindViewById<Button>(Resource.Id.btnSearch);
            mSearchButton.Click += MSearchButton_Click;
            mSearchEditText.EditorAction += MSearchEditText_EditorAction; ; 

            mSearchLinearLayout.Alpha = 0;
            mContainer.BringToFront();

            mListDataSet = new List<string>();
            mListDataSet.Add("Установить/снять пометку на удаление");
            ArrayAdapter<string> mListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, mListDataSet);

            AlertDialog.Builder mAlertDialog = new AlertDialog.Builder(this)
                .SetTitle("Выберите действие:")
                .SetAdapter(mListAdapter, OnSelectAction);
            mDialog = mAlertDialog.Create();

            UpdateList();
        }
        
        private void OnSelectAction(object sender, DialogClickEventArgs e)
        {
            switch (mListDataSet[e.Which])
            {
                case "Пометить на удаление":
                    SetDeleteMark();
                    break;
            }
        }

        private void SetDeleteMark()
        {
            AlertDialog.Builder quitDialog = new AlertDialog.Builder(this);
            quitDialog.SetTitle("Вы уверены?");
            quitDialog.SetPositiveButton("Да", (senderAlert, args) =>
            {
                var item = mAdapterDataList.GetItemAtPosition(mSelectedItemPosition);

                Dictionary<long, ElementData> mCommands = new Dictionary<long, ElementData>();
                mCommands.Add(0, new ElementData() { Name = "delete", Data = "delete" });
                string output = JsonConvert.SerializeObject(mCommands);
                DataSetWS dataSetWS = new DataSetWS();
                dataSetWS.SetDataCompleted += DataSetWS_SetDataCompleted;
                dataSetWS.SetDataAsync(mRef, item.Ref, output, AppVariable.Variable.getSessionParametersJSON());
                mCommands.Remove(0);
            });
            quitDialog.SetNegativeButton("Нет", (senderAlert, args) =>
            {

            });
            Dialog dialog = quitDialog.Create();
            dialog.Show();
        }

        private void DataSetWS_SetDataCompleted(object sender, MobileAppPozitiv.SetDataCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    Toast.MakeText(this, "Отменено", ToastLength.Short).Show();
                }
                else
                {
                    JObject jsonResult = JObject.Parse(e.Result);
                    string Result = jsonResult["Result"].Value<string>();
                    string Message = jsonResult["Message"].Value<string>();
                    Toast.MakeText(this, Message, ToastLength.Long).Show();
                    UpdateList();
                }
            }
            else
            {
                Toast.MakeText(this, e.Error.Message, ToastLength.Long).Show();
            }
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(ActivityDataView));
            intent.PutExtra("reflistmod", mRef);
            intent.PutExtra("ref", "");
            intent.PutExtra("name", "Новый");
            StartActivity(intent);
        }

        private void MDataList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var item = this.mAdapterDataList.GetItemAtPosition(e.Position);
            if (mIsTreeSelect)
            {
                if (item.IsGroup)
                    UpdateList(item.Ref );
                else
                {
                    Intent intent = new Intent();
                    intent.PutExtra("ref", item.Ref);
                    intent.PutExtra("name", item.Name);
                    SetResult(Result.Ok, intent);
                    Finish();
                }

            }
            else if (mIsSelectedForm)
            {
                Intent intent = new Intent();                
                intent.PutExtra("ref", item.Ref);
                intent.PutExtra("name", item.Name);
                SetResult(Result.Ok, intent);
                Finish();
            }
            else
            {
                //Открываем фрму редактирования и ловим ее закрытие
                Intent intent = new Intent(this, typeof(ActivityDataView));
                intent.PutExtra("reflistmod", mRef);
                intent.PutExtra("ref", item.Ref);
                intent.PutExtra("name", item.Name);
                StartActivityForResult(intent, 1);
            }            
        }

        private void MDataList_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            //var item = this.mAdapterDataList.GetItemAtPosition(e.Position);
            mSelectedItemPosition = e.Position;
            mDialog.Show();
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
                case Resource.Id.action_refresh:                    
                    UpdateList();
                    return true;
                case Resource.Id.action_help:
                    return true;
                case Resource.Id.action_search:
                    mSearchLinearLayout.Visibility = ViewStates.Visible;

                    if (mIsAnimating)
                    {
                        return true;
                    }

                    if (!mAnimatedDown)
                    {
                        //Listview is up
                        UIAnimation anim = new UIAnimation(mDataList, mDataList.Height - mSearchLinearLayout.Height);
                        anim.Duration = 500;
                        mDataList.StartAnimation(anim);
                        anim.AnimationStart += anim_AnimationStartDown;
                        anim.AnimationEnd += anim_AnimationEndDown;
                        mContainer.Animate().TranslationYBy(mSearchLinearLayout.Height).SetDuration(500).Start();
                    }
                    else
                    {
                        //Listview is down
                        UIAnimation anim = new UIAnimation(mDataList, mDataList.Height + mSearchLinearLayout.Height);
                        anim.Duration = 500;
                        mDataList.StartAnimation(anim);
                        anim.AnimationStart += anim_AnimationStartUp;
                        anim.AnimationEnd += anim_AnimationEndUp;
                        mContainer.Animate().TranslationYBy(-mSearchLinearLayout.Height).SetDuration(500).Start();
                    }

                    mAnimatedDown = !mAnimatedDown;
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.action_menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void UpdateList(string groupRef = "")
        {
            if (AppVariable.Variable.isOnline != true) return;
            Task taskUpdate = new Task(() => {
                Thread.Sleep(100);
                RunOnUiThread(() => mSwipeRefreshLayout.Refreshing = true);

                mAdapterDataList = new AdapterDataList(this, mRef, groupRef, mSearchEditText.Text);

                RunOnUiThread(() => {
                    mDataList.Adapter = mAdapterDataList;
                    mSwipeRefreshLayout.Refreshing = false;
                    if (mAdapterDataList.Count == 0)
                    {
                        mDataList.Visibility = ViewStates.Gone;
                        mEemptyList.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        mDataList.Visibility = ViewStates.Visible;
                        mEemptyList.Visibility = ViewStates.Gone;
                    }
                });
            });
            taskUpdate.Start();
        }

        void mSwipeRefreshLayout_Refresh(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void MSearchEditText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            setDataListFilter();
        }

        private void MSearchButton_Click(object sender, EventArgs e)
        {
            setDataListFilter();
        }

        private void setDataListFilter()
        {
            mTaskSearch = new Task(() =>
            {
                RunOnUiThread(() => mSwipeRefreshLayout.Refreshing = true);
                mAdapterDataList.FilterList(mSearchEditText.Text);
                RunOnUiThread(() => { mDataList.Adapter = mAdapterDataList; mSwipeRefreshLayout.Refreshing = false; });
            });
            mTaskSearch.Start();
        }

        void anim_AnimationEndUp(object sender, Android.Views.Animations.Animation.AnimationEndEventArgs e)
        {
            mIsAnimating = false;
            mSearchEditText.ClearFocus();
            InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
            inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
        }

        void anim_AnimationEndDown(object sender, Android.Views.Animations.Animation.AnimationEndEventArgs e)
        {
            mIsAnimating = false;
            mSearchEditText.RequestFocus();
            InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
            inputManager.ShowSoftInput(mSearchEditText, ShowFlags.Implicit);
        }

        void anim_AnimationStartDown(object sender, Android.Views.Animations.Animation.AnimationStartEventArgs e)
        {
            mIsAnimating = true;
            mSearchLinearLayout.Animate().AlphaBy(1.0f).SetDuration(500).Start();
        }

        void anim_AnimationStartUp(object sender, Android.Views.Animations.Animation.AnimationStartEventArgs e)
        {
            mIsAnimating = true;
            mSearchLinearLayout.Animate().AlphaBy(-1.0f).SetDuration(300).Start();
        }
    }
}