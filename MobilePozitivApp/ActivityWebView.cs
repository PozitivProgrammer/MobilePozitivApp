using System;
using System.IO;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Webkit;
using System.Text.RegularExpressions;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Graphics;
using Java.IO;
using Console = System.Console;
using File = Java.IO.File;

namespace MobilePozitivApp
{
    [Activity(Label = "ActivityElementsList", Theme = "@style/MyTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    class ActivityWebView : AppCompatActivity
    {
        WebView mWebView;
        private SupportToolbar mToolbar;

        string mName;
        string mHtmlBase64;
        private string mReportAddr;

        private string html;

        private string html_preprocessor(string procStr)
        {
            var Matches = Regex.Matches(procStr, "[:\\s]+#[0123456789abcdef]{6}[\\s;\\}\"]", RegexOptions.IgnoreCase);
            foreach (Match match in Matches)
            {

                var Matches2 = Regex.Matches(match.Value, "#[0123456789abcdef]{6}", RegexOptions.IgnoreCase);
                foreach (Match match2 in Matches2)
                {
                    var rComponent = int.Parse(match2.Value.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                    var gComponent = int.Parse(match2.Value.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    var bComponent = int.Parse(match2.Value.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

                    var totalStr = " rgb(" + rComponent.ToString() + ", " + gComponent.ToString() + ", " + bComponent.ToString() + ") ";

                    procStr = Regex.Replace(procStr, "([:\\s]+)" + match2.Value + "([\\s;\\}\"])", "$1" + totalStr + "$2", RegexOptions.IgnoreCase);


                }



            }
            return procStr;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ActivityWebView);

            mName = Intent.GetStringExtra("name");
            mHtmlBase64 = Intent.GetStringExtra("htmlbase64");
            int mScale = Intent.GetIntExtra("zoom", 0);
            int mTop = Intent.GetIntExtra("top", 0);
            int mLeft = Intent.GetIntExtra("left", 0);

            mToolbar = FindViewById<SupportToolbar>(Resource.Id.Toolbar);
            mToolbar.Title = mName;
            SetSupportActionBar(mToolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            var base64EncodedBytes = Convert.FromBase64String(mHtmlBase64);
            html = Encoding.UTF8.GetString(base64EncodedBytes);

            mReportAddr = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath +
                          "/Download/1C_Files/Report.html";

            mWebView = (WebView)FindViewById(Resource.Id.WebView);
            mWebView.Settings.BuiltInZoomControls = true;
            mWebView.Settings.DisplayZoomControls = true;
            var customWebViewClient = new CustomWebViewClient();
            customWebViewClient.OnPageLoaded += (sender, b) => 
            {
                if (mScale != 0)
                {
                    mWebView.SetInitialScale(mScale);
                    mWebView.ScrollTo(mLeft, mTop);
                }
            };
            mWebView.SetWebViewClient(customWebViewClient);

            
            mWebView.LoadData(html_preprocessor(html), "text/html", "UTF-8");

            
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.action_refresh:
                    Intent intent = new Intent();
                    intent.PutExtra("zoom", (int)(mWebView.Scale * 100));
                    intent.PutExtra("top", mWebView.ScrollY);
                    intent.PutExtra("left", mWebView.ScrollX);
                    SetResult(Result.Ok, intent);
                    Finish();
                    return true;
                case Resource.Id.action_help:
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.action_menu_web, menu);
            var mMenuItem = menu.FindItem(Resource.Id.action_share);
            var overflow_provider = (ShareActionProvider)Android.Support.V4.View.MenuItemCompat.GetActionProvider(mMenuItem);
            overflow_provider.ShareTargetSelected += Overflow_provider_ShareTargetSelected;
            overflow_provider.SetShareIntent(CreateIntent());
            return base.OnCreateOptionsMenu(menu);
        }

        private void Overflow_provider_ShareTargetSelected(object sender, ShareActionProvider.ShareTargetSelectedEventArgs e)
        {
            File folder = new File(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Download/1C_Files/");
            if (folder.Exists() == false) folder.Mkdir();

            using (StreamWriter write = new StreamWriter(mReportAddr, false, Encoding.UTF8))
            {
                write.Write(html);
            }
        }

        Intent CreateIntent()
        {
            var sendPictureIntent = new Intent(Intent.ActionSend);
            sendPictureIntent.SetType("text/*");
            sendPictureIntent.PutExtra(Intent.ExtraSubject, "Отчет \"" + mName + "\"");
            sendPictureIntent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(new File(mReportAddr)));
            return sendPictureIntent;
        }
    }

    public class CustomWebViewClient : WebViewClient
    {
        public event EventHandler<bool> OnPageLoaded;

        public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
        {
            view.LoadUrl(request.Url.ToString());
            return true;
        }

        public override void OnPageStarted(WebView view, string url, global::Android.Graphics.Bitmap favicon)
        {
            base.OnPageStarted(view, url, favicon);
        }

        public override void OnPageFinished(WebView view, string url)
        {
            OnPageLoaded?.Invoke(this, true);
        }

        public override void OnReceivedError(WebView view, IWebResourceRequest request, WebResourceError error)
        {
            OnPageLoaded?.Invoke(this, false);
        }
    }
}