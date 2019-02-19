using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using SupportFragment = Android.Support.V4.App.Fragment;
using Android.Support.V7.App;
using Android.Support.V4.Widget;


namespace MobilePozitivApp
{
    [Activity(Label = "Позитив телеком", Icon = "@drawable/MainIcon", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ActivityMain : AppCompatActivity
    {
        //Переменные интерфейса
        private SupportToolbar mToolbar;
        private UIActionBarDrawerToggle mDrawerToggle;
        private DrawerLayout mDrawerLayout;
        private SwipeRefreshLayout mSwipeRefreshLayout;
        private TextView mTextViewLogin;
        private Button mButtonLogout;

        private ListView mLeftDrawer;
        private ListView mRightDrawer;
        private AdapterGroups mLeftAdapter;
        private ArrayAdapter mRightAdapter;
        private List<string> mRightDataSet;

        //Фрагменты экранов
        private FragmentMain Fragment;
        private FragmentMessages messagesFragment;
        private FragmentElements tasksFragment;
        private FragmentElements directoriesFragment;
        private FragmentElements docsFragment;
        private FragmentElements ReportsFragment;
        private FragmentElements DataProcessorsFragment;

        private SupportFragment mCurrentFragment = new SupportFragment();
        private Stack<SupportFragment> mStackFragments;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.ActivityMain);

            bool mMessage = Intent.GetBooleanExtra("ismessage", false);
            string mRef = Intent.GetStringExtra("ref");
            string mRefListMod = Intent.GetStringExtra("reflistmod");
            string mName = Intent.GetStringExtra("name");

            //Переменные объектов активити
            mToolbar = FindViewById<SupportToolbar>(Resource.Id.Toolbar);
            mDrawerLayout = FindViewById<DrawerLayout>(Resource.Id.DrawerLayout);
            mLeftDrawer = FindViewById<ListView>(Resource.Id.mainLeftDrawer);
            mRightDrawer = FindViewById<ListView>(Resource.Id.mainRightDrawer);
            mTextViewLogin = FindViewById<TextView>(Resource.Id.mainTextViewLogin);
            mButtonLogout = FindViewById<Button>(Resource.Id.btnLogout);

            mSwipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.mainSwipeLayout);
            mSwipeRefreshLayout.SetColorSchemeResources(Android.Resource.Color.HoloBlueBright, Android.Resource.Color.HoloBlueDark, Android.Resource.Color.HoloGreenLight, Android.Resource.Color.HoloRedLight);
            mSwipeRefreshLayout.Refresh += MSwipeRefreshLayout_Refresh;

            mTextViewLogin.Text = AppVariable.Variable.Login;

            mButtonLogout.Click += MButtonLogout_Click;

            //Классы фрагментов
            Fragment = new FragmentMain();
            messagesFragment = new FragmentMessages();
            tasksFragment = new FragmentElements();
            directoriesFragment = new FragmentElements();
            docsFragment = new FragmentElements();
            ReportsFragment = new FragmentElements() { isReport = true };
            DataProcessorsFragment = new FragmentElements() { isReport = true };

            mStackFragments = new Stack<SupportFragment>();

            mLeftDrawer.Tag = 0;
            mRightDrawer.Tag = 1;

            mToolbar.Title = mMessage ? "Сообщения" : "Главная страница";
            SetSupportActionBar(mToolbar);

            UpdateLeftDrawer();

            mLeftDrawer.ItemClick += MenuListView_ItemClick;

            mRightDataSet = new List<string>();
            mRightDataSet.Add("Справка");
            mRightDataSet.Add("О программе");

            if (AppVariable.Variable.DebugMode) mRightDataSet.Add("Форма для тестирования");

            mRightAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, mRightDataSet);
            mRightDrawer.Adapter = mRightAdapter;
            mRightDrawer.ItemClick += MRightDrawer_ItemClick;

            mDrawerToggle = new UIActionBarDrawerToggle(this, mDrawerLayout);

            mDrawerLayout.AddDrawerListener(mDrawerToggle);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayShowTitleEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            mDrawerToggle.SyncState();

            Android.Support.V4.App.FragmentTransaction tx = SupportFragmentManager.BeginTransaction();

            tx.Add(Resource.Id.FragmentLayout, Fragment);
            tx.Add(Resource.Id.FragmentLayout, messagesFragment);
            tx.Add(Resource.Id.FragmentLayout, tasksFragment);
            tx.Add(Resource.Id.FragmentLayout, directoriesFragment);
            tx.Add(Resource.Id.FragmentLayout, docsFragment);
            tx.Add(Resource.Id.FragmentLayout, ReportsFragment);
            tx.Add(Resource.Id.FragmentLayout, DataProcessorsFragment);
            tx.Hide(mMessage ? (SupportFragment)Fragment : (SupportFragment)messagesFragment);
            tx.Hide(tasksFragment);
            tx.Hide(directoriesFragment);
            tx.Hide(docsFragment);
            tx.Hide(ReportsFragment);
            tx.Hide(DataProcessorsFragment);

            mCurrentFragment = mMessage ? (SupportFragment)messagesFragment : (SupportFragment)Fragment;
            tx.Commit();

            if (mRef != null && mRefListMod != null)
            {
                Intent intent = new Intent(this, typeof(ActivityDataView));
                intent.PutExtra("reflistmod", mRefListMod);
                intent.PutExtra("ref", mRef);
                intent.PutExtra("name", mName);
                StartActivity(intent);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.action_menu_main, menu);
            IMenuItem mMenuItem = menu.FindItem(Resource.Id.action_search);
            mMenuItem.SetVisible(false);
            return base.OnCreateOptionsMenu(menu);
        }

        private void MButtonLogout_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(ActivityLogin));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.ClearTask | ActivityFlags.NewTask);
            intent.PutExtra("logout", true);
            StartActivity(intent);
        }

        private void MSwipeRefreshLayout_Refresh(object sender, EventArgs e)
        {
            UpdateLeftDrawer();
        }

        private void UpdateLeftDrawer()
        {
            if (AppVariable.Variable.isOnline != true) return;
            Task taskUpdateGroups = new Task(() => {
                RunOnUiThread(() => mSwipeRefreshLayout.Refreshing = true);
                mLeftAdapter = new AdapterGroups(this);
                RunOnUiThread(() => {
                    mLeftDrawer.Adapter = mLeftAdapter;
                    RunOnUiThread(() => mSwipeRefreshLayout.Refreshing = false);
                });
            });
            taskUpdateGroups.Start();
        }

        private void MRightDrawer_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            string item = this.mRightAdapter.GetItem(e.Position).ToString();
            switch (item)
            {
                case "Справка":

                    break;
                case "О программе":
                    Intent intentAbout = new Intent(this, typeof(ActivityAbout));
                    StartActivity(intentAbout);
                    break;
                case "Форма для тестирования":
                    Intent intentTest = new Intent(this, typeof(ActivityTest));
                    StartActivity(intentTest);
                    break;
                default:

                    break;
            }

            mDrawerLayout.CloseDrawers();
            mDrawerToggle.SyncState();
        }

        void MenuListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var item = this.mLeftAdapter.GetItemAtPosition(e.Position);
            mToolbar.Title = item.Name;

            switch (item.Name)
            {
                case "Главная страница":
                    ShowFragment(Fragment);
                    Fragment.UpdateList();
                    break;
                case "Сообщения":
                    ShowFragment(messagesFragment);
                    messagesFragment.UpdateList();
                    break;
                case "Задачи":
                    ShowFragment(tasksFragment);
                    tasksFragment.UpdateList(item.Ref);
                    break;
                case "Справочники":
                    ShowFragment(directoriesFragment);
                    directoriesFragment.UpdateList(item.Ref);
                    break;
                case "Документы":
                    ShowFragment(docsFragment);
                    docsFragment.UpdateList(item.Ref);
                    break;
                case "Обработки":
                    ShowFragment(DataProcessorsFragment);
                    DataProcessorsFragment.UpdateList(item.Ref);
                    break;
                case "Отчеты":
                    ShowFragment(ReportsFragment);
                    ReportsFragment.UpdateList(item.Ref);
                    break;
                case "Прочее":
                    //ShowFragment(otherFragment);
                    break;
                default:
                    Toast.MakeText(this, item.Name, ToastLength.Short).Show();
                    break;
            }

            mDrawerLayout.CloseDrawers();
            mDrawerToggle.SyncState();
        }

        private void ShowFragment(SupportFragment fragment)
        {
            if (fragment.IsVisible)
            {
                return;
            }

            var trans = SupportFragmentManager.BeginTransaction();

            fragment.View.BringToFront();
            mCurrentFragment.View.BringToFront();

            trans.Hide(mCurrentFragment);
            trans.Show(fragment);

            trans.AddToBackStack(null);
            mStackFragments.Push(mCurrentFragment);

            mCurrentFragment = fragment;

            trans.Commit();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    mDrawerToggle.OnOptionsItemSelected(item);
                    return true;
                case Resource.Id.action_refresh:
                    mCurrentFragment.OnResume();
                    return true;
                case Resource.Id.action_help:
                    mDrawerLayout.OpenDrawer(mRightDrawer);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            mDrawerToggle.SyncState();
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            mDrawerToggle.OnConfigurationChanged(newConfig);
        }

        public override void OnBackPressed()
        {
            openQuitDialog();
        }

        private void openQuitDialog()
        {
            Android.Support.V7.App.AlertDialog.Builder quitDialog = new Android.Support.V7.App.AlertDialog.Builder(this);
            quitDialog.SetTitle("Выход: Вы уверены?");
            quitDialog.SetPositiveButton("Да", (senderAlert, args) =>
            {
                Finish();

                Intent intent = new Intent(Intent.ActionMain);
                intent.AddCategory(Intent.CategoryHome);
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.ClearTask | ActivityFlags.NewTask);
                StartActivity(intent);
            });
            quitDialog.SetNegativeButton("Нет", (senderAlert, args) =>
            {

            });
            Dialog dialog = quitDialog.Create();
            dialog.Show();
        }
    }
}