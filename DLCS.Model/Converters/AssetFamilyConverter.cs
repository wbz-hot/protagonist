using System;
using DLCS.Model.Assets;
using Newtonsoft.Json;

namespace DLCS.Model.Converters
{
    public class AssetFamilyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue(AssetFamily.Image);
                return;
            }

            var assetFamily = (AssetFamily)value;
            writer.WriteValue((char)assetFamily);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            var enumChar = enumString.ToCharArray()[0];

            return (AssetFamily) enumChar;
        }

        public override bool CanConvert(Type objectType)
            => objectType == typeof(string);
    }
}