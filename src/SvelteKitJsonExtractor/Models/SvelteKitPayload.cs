using System.Text.Json;
using System.Text.Json.Serialization;

namespace SvelteKitJsonExtractor.Models
{
    // The shape kit.start(...) always has, regardless of which site/page
    // you're pulling from.
    public class SvelteKitPayload
    {
        [JsonPropertyName("node_ids")]
        public int[] NodeIds { get; set; }

        public List<RouteEntry> Data { get; set; }

        public JsonElement Form { get; set; }
        public JsonElement Error { get; set; }
    }

    // Each entry in "data" is one route's server-loaded data. The actual
    // shape of Data varies per route (session info vs page-specific stuff),
    // so it stays a raw JsonElement until you deserialize it into whatever
    // type matches the route you actually want.
    public class RouteEntry
    {
        public string Type { get; set; }
        public JsonElement Data { get; set; }

        public T As<T>(JsonSerializerOptions options = null) =>
            Data.Deserialize<T>(options ?? SvelteKitJson.Options);
    }

    public static class SvelteKitJson
    {
        // without needing [JsonPropertyName] on nearly every property
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}