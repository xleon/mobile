using System;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data
{
    public class ProjectColorJsonConverter : JsonConverter
	{
        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Null) {
                return ProjectColor.Default();
            } else {
                int colorIndex = Convert.ToInt32 (existingValue);
                return ProjectColor.All [colorIndex % ProjectColor.All.Length];
            }
        }

        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            var colorIndex = Array.IndexOf (ProjectColor.All, (ProjectColor)value);
            writer.WriteValue (colorIndex);
        }

        public override bool CanConvert (Type objectType)
        {
            return objectType.IsSubclassOf (typeof(ProjectColor));
        }
	}
}
