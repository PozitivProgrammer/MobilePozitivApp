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
using Android.Preferences;


namespace MobilePozitivApp
{
    [Application]
    class AppApplication : Application, Application.IActivityLifecycleCallbacks
    {
        public AppApplication(IntPtr handle, JniHandleOwnership ownerShip) : base(handle, ownerShip)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();
            RegisterActivityLifecycleCallbacks(this);
            AppPreferences.Preferences.InitPreferences(this);
            AppAlarmManager.SetAppAlarmManager(this);
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }

        public void CheckSession(Activity Contex)
        {
            if (AppVariable.Variable.isOnline != true)
            {
                Intent intent = new Intent(Contex, typeof(ActivityLogin));
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.ClearTask | ActivityFlags.NewTask);
                Contex.StartActivity(intent);
            }
        }

        ///////////////////////////////////
        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            if (activity.GetType() != typeof(ActivitySplashScreen) && activity.GetType() != typeof(ActivityLogin)) CheckSession(activity);
        }

        public void OnActivityDestroyed(Activity activity)
        {

        }

        public void OnActivityPaused(Activity activity)
        {

        }

        public void OnActivityResumed(Activity activity)
        {

        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {

        }

        public void OnActivityStarted(Activity activity)
        {

        }

        public void OnActivityStopped(Activity activity)
        {

        }
    }
}