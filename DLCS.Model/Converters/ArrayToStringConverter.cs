using System;
using System.Linq;
using DLCS.Core.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DLCS.Model.Converters
{
    public class ArrayToStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            if (value != null)
            {
                var strVal = value.ToString();
                if (!string.IsNullOrWhiteSpace(strVal))
                {
                    foreach (var val in strVal.Split(",", StringSplitOptions.RemoveEmptyEntries))
                    {
                        writer.WriteValue(val);
                    }
                }
            }
            
            writer.WriteEndArray();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            return array.HasValues
                ? string.Join(",", array.Children().Select(s => s.ToString()))
                : null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnumerable();
        }
    }
}