using System;

using Android.App;
using Android.Content;
using Android.Util;

using XamarinShortcutBadger;

using Newtonsoft.Json.Linq;


namespace MobilePozitivApp
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    class AppReceiverBootCompleted : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info(typeof(AppReceiverBootCompleted).Name, "Update task count. {0}", DateTime.UtcNow);
            AppAlarmManager.SetAppAlarmManager(context);
        }
    }

    [BroadcastReceiver(Enabled = true)]
    class AppReceiverAlarm : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            AppPeriodic preiodic = new AppPeriodic();
            preiodic.GetPeriodicData(context);
            AppAlarmManager.SetAppAlarmManager(context);            
        }
    }

    class AppPeriodic
    {
        private Context context;

        public void GetPeriodicData(Context context)
        {
            this.context = context;

            if (AppPreferences.Preferences.GetPreferences("remember", false))
            {
                if (!AppVariable.Variable.isOnline)
                {
                    AppPreferences.Preferences.LoadPreferences();
                }             
             
                DataSetWS dataSetWS = new DataSetWS();
                dataSetWS.GetPeriodicDataCompleted += DataSetWS_GetPeriodicDataCompleted;
                dataSetWS.GetPeriodicDataAsync("");               
            }
        }

        private void DataSetWS_GetPeriodicDataCompleted(object sender, MobileAppPozitiv.GetPeriodicDataCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    
                }
                else
                {
                    int itemCount = 0;
                    JObject jsonPeriodicDataResult = JObject.Parse(e.Result);
                    JProperty jItemCount;
                    if ((jItemCount = jsonPeriodicDataResult.Property("ItemCount")) != null)
                    {
                        itemCount = (int)jItemCount.Value;
                    }
                    Log.Info(typeof(AppReceiverAlarm).Name, "Update task count. {0} - count {1}", DateTime.UtcNow, itemCount);
                    ShortcutBadger.ApplyCount(context, itemCount);
                }
            }
            else
            {
                Log.Error(typeof(AppPeriodic).Name, "Error update task count. {0}", DateTime.UtcNow);
            }
        }
    }
}