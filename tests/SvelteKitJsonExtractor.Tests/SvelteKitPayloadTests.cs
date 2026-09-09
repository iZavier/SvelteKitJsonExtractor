using System.Text.Json;
using Xunit;
using SvelteKitJsonExtractor.Models;

namespace SvelteKitJsonExtractor.Tests
{
    public class SvelteKitPayloadTests
    {
        private class SampleRouteData
        {
            public string Foo { get; set; }
        }

        [Fact]
        public void DeserializesNodeIdsAndData()
        {
            string json = @"{
                ""node_ids"": [0, 24],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""foo"": ""bar"" } }
                ]
            }";

            var payload = JsonSerializer.Deserialize<SvelteKitPayload>(json, SvelteKitJson.Options);

            Assert.Equal(new[] { 0, 24 }, payload.NodeIds);
            Assert.Single(payload.Data);
        }

        [Fact]
        public void RouteEntryAsDeserializesItsDataProperty()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""foo"": ""bar"" } }
                ]
            }";

            var payload = JsonSerializer.Deserialize<SvelteKitPayload>(json, SvelteKitJson.Options);
            var result = payload.Data[0].As<SampleRouteData>();

            Assert.Equal("bar", result.Foo);
        }
    }
}
