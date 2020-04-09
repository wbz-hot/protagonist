using System;
using DLCS.Core.Reflection;
using Newtonsoft.Json;

namespace DLCS.Model.Converters
{
    public class ArrayToStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // output string
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var readAsString = reader.ReadAsString();
            return readAsString;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnumerable();
        }
    }
}