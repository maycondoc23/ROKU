using System.Configuration;

namespace WebServerSFC
{
    public class AppConfiguration
    {
        public string sfc_value;

        public static string Sfc_value(string workgroup, string step, string status)
        {
            return ConfigurationManager.AppSettings.Get($@"SFC{workgroup}_STEP{step}_{status}");
        }
        public static string Port { get { return ConfigurationManager.AppSettings.Get("PORT"); } }
        public static string ProductLine { get { return ConfigurationManager.AppSettings.Get("PRODUCT_LINE"); } }
        public static int MillisecondsTimeout { get { return int.Parse(ConfigurationManager.AppSettings.Get("MILLISECONDS-TIMEOUT")); } }
    }



}
