using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using Android.Preferences;
using Android.Content.PM;
using Android.Locations;
using Java.Util;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Firebase.Messaging;
using Firebase.Iid;

namespace MobilePozitivApp
{
    [Activity(Label = "Позитив телеком", Icon = "@drawable/MainIcon", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ActivityLogin : Activity, ILocationListener
    {
        //LocationManager locMgr;

        private LinearLayout mRelativeLayout;
        private Button mButtonLogin;
        private Button mButtonUpdate;
        private EditText mLogin;
        private EditText mPass;
        private CheckBox mSigninwithapin;
        private CheckBox mRemember;
        private TextView mVersion;

        private AppUpdate mUpdateApp;
        private ProgressDialog mProgressDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityLogin);

            bool isUpdate = Intent.GetBooleanExtra("update", false);
            bool islogout = Intent.GetBooleanExtra("logout", false);
            

            mProgressDialog = new ProgressDialog(this);
            mProgressDialog.Indeterminate = true;
            mProgressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            mProgressDialog.Max = 100;
            mProgressDialog.SetCancelable(false);

            mRelativeLayout = FindViewById<LinearLayout>(Resource.Id.loginView);
            mButtonLogin = FindViewById<Button>(Resource.Id.btnLogin);
            mButtonUpdate = FindViewById<Button>(Resource.Id.btnUpdateApp);
            mLogin = FindViewById<EditText>(Resource.Id.Login);
            mPass = FindViewById<EditText>(Resource.Id.Password);
            mSigninwithapin = FindViewById<CheckBox>(Resource.Id.Signinwithapin);
            mRemember = FindViewById<CheckBox>(Resource.Id.Remember);
            mVersion = FindViewById<TextView>(Resource.Id.Version);

            if (islogout)
            {
                Task taskDelID = new Task(() => FirebaseInstanceId.Instance.DeleteInstanceId());
                taskDelID.Start();
                if (AppPreferences.Preferences.GetPreferences("remember", false))
                {
                    AppPreferences.Preferences.SetPreferences("login", AppPreferences.Preferences.GetPreferences("login", ""));
                    mLogin.Text = AppPreferences.Preferences.GetPreferences("login", "");
                    AppPreferences.Preferences.SetPreferences("pass", "");
                }
                AppPreferences.Preferences.SetPreferences("signinwithapin", false);
                AppPreferences.Preferences.SetPreferences("remember", false);
            }
                        
            mRemember.Checked = AppPreferences.Preferences.GetPreferences("remember", false);
            if (mRemember.Checked)
            {
                mLogin.Text = AppPreferences.Preferences.GetPreferences("login", "");
                mPass.Text = AppPreferences.Preferences.GetPreferences("pass", "");
                mSigninwithapin.Checked = AppPreferences.Preferences.GetPreferences("signinwithapin", false);
            }

            mButtonLogin.Click += mButtonLogin_Click;
            mRelativeLayout.Click += mRelativeLayout_Click;

            AppVariable.Variable.versionSDK = Build.VERSION.Sdk;
            AppVariable.Variable.modelName = Build.Model;
            PackageInfo pInfo = PackageManager.GetPackageInfo(PackageName, 0);
            AppVariable.Variable.version = pInfo.VersionCode;
            AppVariable.Variable.versionName = pInfo.VersionName;
            mVersion.Text = pInfo.VersionName;

            //Preferences
            AppVariable.Variable.DebugMode = AppPreferences.Preferences.GetPreferences("debug", false);
            if (AppPreferences.Preferences.GetPreferences("mainServer", true))
            {
                AppVariable.Variable.WsURL = AppPreferences.Preferences.GetPreferences("MainServerURL", AppVariable.Variable.WsURL);
            }
            else
            {
                AppVariable.Variable.WsURL = AppPreferences.Preferences.GetPreferences("DebugServerURL", AppVariable.Variable.WsURL);
            }

            mUpdateApp = new AppUpdate(this);
            mButtonUpdate.Visibility = ViewStates.Invisible;

            Task taskCheckUpdate = new Task(() => {
                bool needUpdate = mUpdateApp.CheckUpdate(pInfo.VersionCode);
                RunOnUiThread(() => {
                    if (needUpdate)
                    {
                        mButtonUpdate.Visibility = ViewStates.Visible;

                        if (isUpdate == false)
                        {
                            int icon = Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop ? Resource.Drawable.MainIconStatus : Resource.Drawable.MainIcon;
                            Intent intent = new Intent(this, typeof(ActivityLogin));
                            intent.PutExtra("update", true);
                            intent.AddFlags(ActivityFlags.ClearTop);
                            PendingIntent pendingIntent = PendingIntent.GetActivity(this, AppVariable.nIDUpdate, intent, PendingIntentFlags.OneShot);
                            Notification.Builder builder = new Notification.Builder(this)
                                .SetContentIntent(pendingIntent)
                                .SetContentTitle("Доступно обновление программы")
                                .SetContentText("Для обновления, нажмите на уведомление.")
                                .SetSmallIcon(icon);
                            Notification notification = builder.Build();
                            NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                            notificationManager.Notify(AppVariable.nIDUpdate, notification);
                        }
                        else
                        {
                            mUpdateApp.Update();
                            mButtonUpdate.Enabled = false;
                        }
                    }
                    else
                    {
                        mButtonUpdate.Visibility = ViewStates.Invisible;
                    }

                    //Автологин
                    if (mRemember.Checked && !isUpdate)
                    {
                        Login();
                    }
                });                
            });
            taskCheckUpdate.Start();

            mButtonUpdate.Click += (sender, args) =>
            {
                mUpdateApp.Update();
                mButtonUpdate.Enabled = false;
            };
            
        }

        void mButtonLogin_Click(object sender, EventArgs e)
        {
            Login();
        }
        
        void Login()
        {
            AppVariable.Variable.Login = mLogin.Text;
            AppVariable.Variable.Password = mPass.Text;


            

            AppVariable.Variable.fireBaseClientID = FirebaseInstanceId.Instance.Token;

            mProgressDialog.SetMessage("Вход...");
            mProgressDialog.Show();

            Dictionary<string, Dictionary<string, string>> LoginData = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> AppInfo = new Dictionary<string, string>();
            AppInfo.Add("AppVersion", AppVariable.Variable.version.ToString());
            AppInfo.Add("AppFirebaseClientID", AppVariable.Variable.fireBaseClientID);
            AppInfo.Add("DevVersionSDK", AppVariable.Variable.versionSDK);
            AppInfo.Add("DevModelName", AppVariable.Variable.modelName);
            LoginData.Add("AppInfo", AppInfo);

            Dictionary<string, string> Parameters = new Dictionary<string, string>();
            if (mSigninwithapin.Checked)
            {
                AppVariable.Variable.Password = "";
                Parameters.Add("pin", mPass.Text);
            }
            LoginData.Add("Parameters", Parameters);

            DataSetWS dataSetWS = new DataSetWS();
            dataSetWS.InitAppAsync(JsonConvert.SerializeObject(LoginData));
            dataSetWS.InitAppCompleted += DataSetWS_InitAppCompleted;
        }

        private void savePreferences()
        {
            AppPreferences.Preferences.SetPreferences("login", mLogin.Text);
            AppPreferences.Preferences.SetPreferences("pass", mPass.Text);
            AppPreferences.Preferences.SetPreferences("signinwithapin", mSigninwithapin.Checked);
            AppPreferences.Preferences.SetPreferences("remember", mRemember.Checked);
        }

        private void clearPreferences()
        {
            AppPreferences.Preferences.SetPreferences("login", "");
            AppPreferences.Preferences.SetPreferences("pass", "");
            AppPreferences.Preferences.SetPreferences("signinwithapin", false);
            AppPreferences.Preferences.SetPreferences("remember", false);
        }

        private void DataSetWS_InitAppCompleted(object sender, MobileAppPozitiv.InitAppCompletedEventArgs e)
        {
            mProgressDialog.Dismiss();
            if (e.Error != null)
            {
                clearPreferences();
                var mAlert = new AlertDialog.Builder(this)
                    .SetTitle("Ошибка авторизации.")
                    .SetMessage("Данные не верны или сервер не доступен. "+ e.Error.Message)
                    .SetCancelable(true)
                    .Show();
                Toast.MakeText(this, e.Error.Message, ToastLength.Short);
            }
            else
            {
                JObject jsonResult = JObject.Parse(e.Result);
                JObject jData = (JObject)jsonResult["Data"];
                JProperty jError = jData.Property("Error");
                if (jError != null)
                {
                    clearPreferences();
                    var mAlert = new AlertDialog.Builder(this)
                        .SetTitle("Ошибка авторизации.")
                        .SetMessage((string)jError.Value)
                        .SetCancelable(true)
                        .Show();
                    return;
                }

                savePreferences();

                AppVariable.Variable.isOnline = true;
                AppVariable.Variable.setSessionParametersJSON(jsonResult);

                //Подписываемся на топики
                FirebaseMessaging.Instance.SubscribeToTopic("news");

                if (jData.Property("UserID") != null)
                {
                    string UserID = (string)jData.Property("UserID").Value;
                    FirebaseMessaging.Instance.SubscribeToTopic("user_" + UserID);
                }

                Intent intent = new Intent(this, typeof(ActivityMain));
                intent.PutExtra("ismessage", Intent.GetStringExtra("isMessage") != null);
                intent.PutExtra("ref", Intent.GetStringExtra("ref"));
                intent.PutExtra("reflistmod", Intent.GetStringExtra("reflistmod"));
                intent.PutExtra("name", Intent.GetStringExtra("name"));
                StartActivity(intent);                
                Finish();                
            }
        }

        void mRelativeLayout_Click(object sender, EventArgs e)
        {
            InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Activity.InputMethodService);
            inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.None);
        }

        protected override void OnResume()
        {
            base.OnResume();
            /*locMgr = GetSystemService(Context.LocationService) as LocationManager;
            if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
                    && locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 2000, 1, this);
            }
            else
            {
                Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
            }*/
        }

        protected override void OnPause()
        {
            base.OnPause();
            //locMgr.RemoveUpdates(this);
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            /*string Text = String.Empty;

            Text += "Latitude: " + location.Latitude.ToString();
            Text += ". Longitude: " + location.Longitude.ToString();
            Text += ". Provider: " + location.Provider.ToString();
            Toast.MakeText(this, Text, ToastLength.Long).Show();*/
        }

        public void OnProviderDisabled(string provider)
        {
        }
        public void OnProviderEnabled(string provider)
        {
        }
        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
        }
    }
}