using System.Text.Json;
using SvelteKitJsonExtractor.Models;
using Xunit;

namespace SvelteKitJsonExtractor.Tests.Models
{
    public class SvelteKitPayloadExtensionsTests
    {
        private class SampleRouteData
        {
            public string Foo { get; set; }
        }

        private static SvelteKitPayload ParsePayload(string json) =>
            JsonSerializer.Deserialize<SvelteKitPayload>(json, SvelteKitJson.Options);

        [Fact]
        public void GetRouteDataFindsEntryByMarkerProperty()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""session"": null } },
                    { ""type"": ""data"", ""data"": { ""financialData"": true, ""foo"": ""bar"" } }
                ]
            }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>("financialData");

            Assert.Equal("bar", result.Foo);
        }

        [Fact]
        public void GetRouteDataReturnsFirstMatchWhenSeveralQualify()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""financialData"": true, ""foo"": ""first"" } },
                    { ""type"": ""data"", ""data"": { ""financialData"": true, ""foo"": ""second"" } }
                ]
            }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>("financialData");

            Assert.Equal("first", result.Foo);
        }

        [Fact]
        public void GetRouteDataReturnsNullWhenNothingMatches()
        {
            string json = @"{ ""node_ids"": [], ""data"": [] }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>("financialData");

            Assert.Null(result);
        }

        [Fact]
        public void GetRouteDataSkipsEntriesWhoseDataIsntAnObject()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": [1, 2, 3] },
                    { ""type"": ""data"", ""data"": { ""financialData"": true, ""foo"": ""ok"" } }
                ]
            }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>("financialData");

            Assert.Equal("ok", result.Foo);
        }

        [Fact]
        public void GetRouteDataWithPredicateMatchesOnNestedValue()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""info"": { ""symbol"": ""AAPL"" } } },
                    { ""type"": ""data"", ""data"": { ""info"": { ""symbol"": ""WMT"" }, ""foo"": ""matched"" } }
                ]
            }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>(e =>
                e.TryGetProperty("info", out var info) &&
                info.TryGetProperty("symbol", out var symbol) &&
                symbol.GetString() == "WMT");

            Assert.Equal("matched", result.Foo);
        }

        [Fact]
        public void GetRouteDataWithPredicateReturnsNullWhenNothingMatches()
        {
            string json = @"{
                ""node_ids"": [],
                ""data"": [
                    { ""type"": ""data"", ""data"": { ""info"": { ""symbol"": ""AAPL"" } } }
                ]
            }";

            var result = ParsePayload(json).GetRouteData<SampleRouteData>(e =>
                e.TryGetProperty("info", out var info) &&
                info.TryGetProperty("symbol", out var symbol) &&
                symbol.GetString() == "WMT");

            Assert.Null(result);
        }
    }
}