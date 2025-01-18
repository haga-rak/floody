using System.Text.Json;
using System.Text.Json.Serialization;

namespace floody
{
    [JsonSerializable(typeof(FloodResult))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )]
    public partial class FloodySerializationContext : JsonSerializerContext
    {
        public static JsonSerializerOptions CustomOptions => new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}