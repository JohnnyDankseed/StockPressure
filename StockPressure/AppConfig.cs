using System.Configuration;
using System.Reflection;
using System.Text;

namespace GetShares
{
    public class AppConfig
    {
        public static bool CheckForToday { get { return bool.Parse(ConfigurationManager.AppSettings["checkForToday"]); } }
        public static bool ShowVolumes { get { return bool.Parse(ConfigurationManager.AppSettings["showVolumes"]); } }

        public static decimal PressureLine { get { return decimal.Parse(ConfigurationManager.AppSettings["pressureLine"]); } }

        public static string ApiKey
        {
            get { return ConfigurationManager.AppSettings["apiKey"]; }
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);
                config.AppSettings.Settings.Add("ApiKey", value);
                config.Save();
            }
        }

        public static string ApiHost
        {
            get { return ConfigurationManager.AppSettings["apiHost"]; }
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);
                config.AppSettings.Settings.Add("ApiHost", value);
                config.Save();
            }
        }

    }
}
