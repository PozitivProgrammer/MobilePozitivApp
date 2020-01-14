using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Widget;

using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Util;


namespace MobilePozitivApp
{
    [Activity(Label = "ActivityTest", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    class ActivityTest: AppCompatActivity
    {
        private TabLayout tabLayout;
        private ViewPager viewPager;
        private FragmentTest mFragmentTest1;
        private FragmentTest mFragmentTest2;
        private Button mButtonSettingsApp;
        private Button mButtonSettingsAdmin;
        private Button mButtonSettingsUser;
        private Button mButtonActivityMessage;
        SupportToolbar toolbar;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityTest);

            //Работа с контекстом приложения
            AppApplication myApp = (AppApplication)ApplicationContext;

            // Find views
            viewPager = FindViewById<ViewPager>(Resource.Id.pager);
            tabLayout = FindViewById<TabLayout>(Resource.Id.sliding_tabs);
            AdapterCustomPager adapter = new AdapterCustomPager(this, SupportFragmentManager);
            toolbar = FindViewById<SupportToolbar>(Resource.Id.my_toolbar);

            mButtonSettingsApp = FindViewById<Button>(Resource.Id.settingsApp);
            mButtonSettingsApp.Click += (sender, args) => {
                Intent IntentSettings = new Intent(this, typeof(ActivitySettings));
                IntentSettings.PutExtra("settings", "app");
                StartActivity(IntentSettings);
            };
            mButtonSettingsAdmin = FindViewById<Button>(Resource.Id.settingsAdmin);
            mButtonSettingsAdmin.Click += (sender, args) =>
            {
                Intent IntentSettings = new Intent(this, typeof(ActivitySettings));
                IntentSettings.PutExtra("settings", "admin");
                StartActivity(IntentSettings);
            };

            mButtonSettingsUser = FindViewById<Button>(Resource.Id.settingsUser);
            mButtonSettingsUser.Click += (sender, args) =>
            {
                Intent IntentSettings = new Intent(this, typeof(ActivitySettings));
                IntentSettings.PutExtra("settings", "user");
                StartActivity(IntentSettings);
            };

            mButtonActivityMessage = FindViewById<Button>(Resource.Id.activityMessage);
            mButtonActivityMessage.Click += (sender, args) =>
            {
                Intent IntentMessage = new Intent(this, typeof(ActivityMessage));
                StartActivity(IntentMessage);
            };

            Button mButtonActivityMap = FindViewById<Button>(Resource.Id.activityMap);
            mButtonActivityMap.Click += (sender, args) =>
            {
                Intent IntentMessage = new Intent(this, typeof(ActivityMaps));
                StartActivity(IntentMessage);
            };


            // Setup Toolbar
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "View для тестирования";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            setupViewPager(viewPager, adapter);

            tabLayout.SetupWithViewPager(viewPager);
            setupTabIcons();

            for (int i = 0; i < tabLayout.TabCount; i++)
            {
                TabLayout.Tab tab = tabLayout.GetTabAt(i);
                tab.SetCustomView(adapter.GetTabView(i));
            }
        }

        private void setupTabIcons()
        {
            /*tabLayout.GetTabAt(0).SetIcon(tabIcons[0]);
            tabLayout.GetTabAt(1).SetIcon(tabIcons[1]);
            tabLayout.GetTabAt(2).SetIcon(tabIcons[2]);
            tabLayout.GetTabAt(3).SetIcon(tabIcons[3]);*/
        }

        public void setupViewPager(ViewPager viewPager, AdapterCustomPager adapter)
        {
            mFragmentTest1 = FragmentTest.newInstance(1);
            mFragmentTest2 = FragmentTest.newInstance(2);

            adapter.addFragment(mFragmentTest1, "Фрагмент 1");
            adapter.addFragment(mFragmentTest2, "Фрагмент 2");

            viewPager.Adapter = adapter;
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
    }
}