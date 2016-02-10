using System;
using System.Configuration;
using WindowsADExplorer.DataModeling;

namespace WindowsADExplorer.Settings
{
    public class ADSettings : IADSettings
    {
        public bool IsWindowsADAvailable
        {
            get { return parseBoolean(ConfigurationManager.AppSettings["IsWindowsADAvailable"]); }
        }

        private static bool parseBoolean(string value, bool fallbackValue = false)
        {
            bool result;
            if (Boolean.TryParse(value, out result))
            {
                return result;
            }
            return fallbackValue;
        }
    }
}
