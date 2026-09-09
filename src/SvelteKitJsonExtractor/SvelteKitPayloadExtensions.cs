using SvelteKitJsonExtractor.Models;
using System.Text.Json;

namespace SvelteKitJsonExtractor
{
    public static class SvelteKitPayloadExtensions
    {
        // Finds the first route whose data object contains the given
        // property, then deserializes that route into T. Matching by shape
        // rather than array index, since route order isn't guaranteed to
        // stay the same across pages or SvelteKit versions.
        public static T GetRouteData<T>(
            this SvelteKitPayload payload,
            string markerProperty,
            JsonSerializerOptions options = null)
        {
            return payload.GetRouteData<T>(
                e => e.TryGetProperty(markerProperty, out _),
                options);
        }

        // Same idea, but takes a predicate instead of a single property
        // name - useful when one property isn't enough to uniquely
        // identify the route you want (e.g. matching on a "type" field's
        // value, or requiring two properties together).
        public static T GetRouteData<T>(
            this SvelteKitPayload payload,
            Func<JsonElement, bool> matches,
            JsonSerializerOptions options = null)
        {
            foreach (var entry in payload.Data)
            {
                if (entry.Data.ValueKind == JsonValueKind.Object && matches(entry.Data))
                    return entry.As<T>(options);
            }
            return default;
        }
    }
}
