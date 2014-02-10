using System;
using Newtonsoft.Json;
using Android.Content.Res;

namespace Toggl.Joey.Bugsnag.Json
{
    public class OrientationConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch ((Orientation)value) {
            case Orientation.Landscape:
                writer.WriteValue ("landscape");
                break;
            case Orientation.Portrait:
                writer.WriteValue ("portrait");
                break;
            case Orientation.Square:
                writer.WriteValue ("square");
                break;
            default:
                writer.WriteNull ();
                break;
            }
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch ((string)reader.Value) {
            case "landscape":
                return Orientation.Landscape;
            case "portrait":
                return Orientation.Portrait;
            case "square":
                return Orientation.Square;
            default:
                return Orientation.Undefined;
            }
        }

        public override bool CanConvert (Type objectType)
        {
            return objectType == typeof(Orientation);
        }
    }
}

