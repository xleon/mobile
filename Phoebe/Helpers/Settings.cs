using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace Toggl.Phoebe.Helpers
{
    /// <summary>
    /// This is the Settings static class that can be used in your Core solution or in any
    /// of your client applications. All settings are laid out the same exact way with getters
    /// and setters.
    /// </summary>
    public static class Settings
    {
        private static ISettings AppSettings
        {
            get
            {
#if __MOBILE__
                return CrossSettings.Current;
#else
                // Used for tests only
                return new CrossSettingsTest();
#endif
            }
        }

        private const string SerializedSettingsKey = "serialized_key";
        private const string IsStagingKey = "staging_key";

        private static readonly string SerializedSettingsDefault = string.Empty;
        private static readonly bool IsStagingDefault = false;

        public static string SerializedSettings
        {
            get { return AppSettings.GetValueOrDefault(SerializedSettingsKey, SerializedSettingsDefault); }
            set { AppSettings.AddOrUpdateValue(SerializedSettingsKey, value); }
        }

        public static bool IsStaging
        {
            get { return AppSettings.GetValueOrDefault(IsStagingKey, IsStagingDefault); }
            set { AppSettings.AddOrUpdateValue(IsStagingKey, value); }
        }

        // Helper class to deserialize using private properties
        // http://stackoverflow.com/questions/4066947/private-setters-in-json-net/4110232#4110232
        public static JsonSerializerSettings GetNonPublicPropertiesResolverSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new NonPublicPropertiesResolver()
            };
            return settings;
        }

        public class NonPublicPropertiesResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                var pi = member as PropertyInfo;
                if (pi != null)
                {
                    prop.Readable = (pi.GetMethod != null);
                    prop.Writable = (pi.SetMethod != null);
                }
                return prop;
            }
        }

        class CrossSettingsTest : ISettings
        {
            public bool AddOrUpdateValue<T> (string key, T value)
            {
                return true;
            }

            public T GetValueOrDefault<T> (string key, T defaultValue = default (T))
            {
                return defaultValue;
            }

            public void Remove(string key)
            {
                // Do Nothing.
            }
        }
    }
}