using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Json
{
    public class TimeSpanConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            var span = (TimeSpan)value;
            writer.WriteValue ((long)span.TotalMilliseconds);
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return TimeSpan.FromMilliseconds ((long)reader.Value);
        }

        public override bool CanConvert (Type objectType)
        {
            return objectType == typeof(TimeSpan);
        }
    }
}
