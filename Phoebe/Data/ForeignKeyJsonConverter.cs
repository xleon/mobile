using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class ForeignKeyJsonConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            var model = (Model)value;

            if (model == null) {
                writer.WriteNull ();
                return;
            }

            writer.WriteValue (model.RemoteId);
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }

            var remoteId = Convert.ToInt64 (reader.Value);
            lock (Model.SyncRoot) {
                var model = Model.Manager.GetByRemoteId (objectType, remoteId);
                if (model == null) {
                    model = (Model)Activator.CreateInstance (objectType);
                    model.RemoteId = remoteId;
                    model.ModifiedAt = new DateTime ();
                    model = Model.Update (model);
                }

                return model;
            }
        }

        public override bool CanConvert (Type objectType)
        {
            return objectType.IsSubclassOf (typeof(Model));
        }
    }
}
