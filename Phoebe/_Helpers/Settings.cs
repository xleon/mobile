using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace Toggl.Phoebe._Helpers
{
    /// <summary>
    /// This is the Settings static class that can be used in your Core solution or in any
    /// of your client applications. All settings are laid out the same exact way with getters
    /// and setters.
    /// </summary>
    public static class Settings
    {
        private static ISettings AppSettings { get { return CrossSettings.Current; } }

        private const string SerializedSettingsKey = "serialized_key";
        private const string IsStagingKey = "staging_key";

        private static readonly string SerializedSettingsDefault = string.Empty;
        private static readonly bool IsStagingDefault = false;

        public static string SerializedSettings
        {
            get { return AppSettings.GetValueOrDefault (SerializedSettingsKey, SerializedSettingsDefault); }
            set { AppSettings.AddOrUpdateValue (SerializedSettingsKey, value); }
        }

        public static bool IsStaging
        {
            get { return AppSettings.GetValueOrDefault (IsStagingKey, IsStagingDefault); }
            set { AppSettings.AddOrUpdateValue (IsStagingKey, value); }
        }
    }
}