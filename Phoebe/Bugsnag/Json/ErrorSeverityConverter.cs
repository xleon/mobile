using System;
using Newtonsoft.Json;
using Toggl.Phoebe.Bugsnag.Data;

namespace Toggl.Phoebe.Bugsnag.Json
{
    public class ErrorSeverityConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch ((ErrorSeverity)value) {
            case ErrorSeverity.Info:
                writer.WriteValue ("info");
                break;
            case ErrorSeverity.Warning:
                writer.WriteValue ("warning");
                break;
            case ErrorSeverity.Fatal:
                writer.WriteValue ("fatal");
                break;
            case ErrorSeverity.Error:
            default:
                writer.WriteValue ("error");
                break;
            }
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.Value.ToString ().ToLower ()) {
            case "info":
                return ErrorSeverity.Info;
            case "warning":
                return ErrorSeverity.Warning;
            case "fatal":
                return ErrorSeverity.Fatal;
            case "error":
            default:
                return ErrorSeverity.Error;
            }
        }

        public override bool CanConvert (Type objectType)
        {
            return objectType == typeof(ErrorSeverity);
        }
    }
}
