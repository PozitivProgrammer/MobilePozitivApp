using Android.Content;
using Android.Preferences;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MobilePozitivApp
{
    public class AppVariable
    {
        public readonly static AppVariable Variable = new AppVariable();

        //Notification IDs
        public const int nIDUpdate = 0;
        public const int nIDFcm = 1;

        public bool isOnline { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public bool DebugMode { get; set; }

        //App info
        public int version;
        public string versionName;
        public string versionSDK;
        public string modelName;
        public string fireBaseClientID;

        private Dictionary<string, string> SessionParameters = new Dictionary<string, string>();
        
#if DEBUG
        public string WsURL = "http://1c.pozitivtelecom.ru/TestDmitry/ws/app.1cws";
        //public string WsURL = "http://1c.pozitivtelecom.ru/SkladPozitiv/ws/app.1cws";
#endif
#if !DEBUG
        public string WsURL = "http://1c.pozitivtelecom.ru/SkladPozitiv/ws/app.1cws";
#endif
        public AppVariable()
        {

        }

        public void setSessionParameter(string name, string value)
        {
            if (SessionParameters.ContainsKey(name))
            {
                SessionParameters[name] = value;
            }
            else
            {
                SessionParameters.Add(name, value);
            }
        }

        public string getSessionParameter(string name)
        {
            return SessionParameters[name];
        }

        public void setSessionParametersJSON(JObject jsonResult)
        {
            foreach (JObject Element in jsonResult["Data"]["SessionParameters"])
            {
                setSessionParameter((string)Element.Property("Name"), (string)Element.Property("Value"));
            }
        }

        public string getSessionParametersJSON()
        {
            return JsonConvert.SerializeObject(SessionParameters);
        }
    }
    
    public class AppPreferences
    {
        ISharedPreferences mPrefs;

        public readonly static AppPreferences Preferences = new AppPreferences();

        AppPreferences()
        {
            
        }

        public void InitPreferences(Context context)
        {
            mPrefs = PreferenceManager.GetDefaultSharedPreferences(context);
        }

        /////////////////////////////
        public void SetPreferences(string key, string value)
        {
            ISharedPreferencesEditor mPerfsEditor = mPrefs.Edit();
            mPerfsEditor.PutString(key, value);
            mPerfsEditor.Apply();
        }

        public void SetPreferences(string key, bool value)
        {
            ISharedPreferencesEditor mPerfsEditor = mPrefs.Edit();
            mPerfsEditor.PutBoolean(key, value);
            mPerfsEditor.Apply();
        }

        public void SetPreferences(string key, int value)
        {
            ISharedPreferencesEditor mPerfsEditor = mPrefs.Edit();
            mPerfsEditor.PutInt(key, value);
            mPerfsEditor.Apply();
        }

        /////////////////////////////
        public string GetPreferences(string key, string defValue)
        {
            return mPrefs.GetString(key, defValue);
        }

        public bool GetPreferences(string key, bool defValue)
        {
            return mPrefs.GetBoolean(key, defValue);
        }

        public int GetPreferences(string key, int defValue)
        {
            return mPrefs.GetInt(key, defValue);
        }

        public void LoadPreferences()
        {
            AppVariable.Variable.Login = GetPreferences("login", "");
            AppVariable.Variable.Password = GetPreferences("pass", "");
            AppVariable.Variable.DebugMode = GetPreferences("debug", false);
            if (GetPreferences("mainServer", true))
            {
                AppVariable.Variable.WsURL = GetPreferences("MainServerURL", AppVariable.Variable.WsURL);
            }
            else
            {
                AppVariable.Variable.WsURL = GetPreferences("DebugServerURL", AppVariable.Variable.WsURL);
            }
        }
    }

    public class AppOther
    {
        public static string Translit(string str)
        {
            string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "", "Y", "", "E", "Yu", "Ya" };
            string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "", "y", "", "e", "yu", "ya" };
            string[] rus_up = { "À", "Á", "Â", "Ã", "Ä", "Å", "¨", "Æ", "Ç", "È", "É", "Ê", "Ë", "Ì", "Í", "Î", "Ï", "Ð", "Ñ", "Ò", "Ó", "Ô", "Õ", "Ö", "×", "Ø", "Ù", "Ú", "Û", "Ü", "Ý", "Þ", "ß" };
            string[] rus_low = { "à", "á", "â", "ã", "ä", "å", "¸", "æ", "ç", "è", "é", "ê", "ë", "ì", "í", "î", "ï", "ð", "ñ", "ò", "ó", "ô", "õ", "ö", "÷", "ø", "ù", "ú", "û", "ü", "ý", "þ", "ÿ" };
            for (int i = 0; i <= 32; i++)
            {
                str = str.Replace(rus_up[i], lat_up[i]);
                str = str.Replace(rus_low[i], lat_low[i]);
            }
            return str;
        }
    }
}