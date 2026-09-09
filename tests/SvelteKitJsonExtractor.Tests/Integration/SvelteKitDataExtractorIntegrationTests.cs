using System.Text.Json;
using Xunit;

namespace SvelteKitJsonExtractor.Tests.Integration
{
    // these actually spin up Firefox and hit a real URL, so they're slower
    // and need geckodriver on PATH + network access. tagged so they're easy
    // to skip:
    //   dotnet test --filter Category!=Integration
    // or run on their own:
    //   dotnet test --filter Category=Integration
    [Trait("Category", "Integration")]
    public class SvelteKitDataExtractorIntegrationTests
    {
        // swap for whatever page you're actually testing
        private const string TargetUrl = "https://stockanalysis.com/stocks/wmt/financials/ratios/";

        [Fact]
        public void ExtractsValidJsonFromLiveUrl()
        {
            using var extractor = SvelteKitDataExtractor.CreateWithFirefox(headless: true);

            string json = extractor.ExtractAsJson(TargetUrl);

            Assert.False(string.IsNullOrWhiteSpace(json));

            // make sure it's actually valid JSON, not just an error string
            // that happened to not throw
            using var doc = JsonDocument.Parse(json);
            Assert.True(
                doc.RootElement.ValueKind == JsonValueKind.Object ||
                doc.RootElement.ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public void ExtractedJsonContainsExpectedTopLevelField()
        {
            using var extractor = SvelteKitDataExtractor.CreateWithFirefox(headless: true);

            using var doc = extractor.ExtractAsJsonDocument(TargetUrl);

            // adjust to a field you know actually shows up in this page's
            // payload - this is the "did I get real data" check
            bool hasExpectedField =
                doc.RootElement.TryGetProperty("data", out _) ||
                doc.RootElement.TryGetProperty("node_ids", out _);

            Assert.True(hasExpectedField,
                "Expected the extracted payload to contain a top-level 'data' or 'node_ids' field.");
        }

        // in case you want to sweep a few pages at once
        [Theory]
        [InlineData("https://stockanalysis.com/stocks/wmt/financials/ratios/")]
        [InlineData("https://stockanalysis.com/stocks/aapl/financials/ratios/")]
        public void ExtractsValidJsonFromMultipleUrls(string url)
        {
            using var extractor = SvelteKitDataExtractor.CreateWithFirefox(headless: true);

            string json = extractor.ExtractAsJson(url);

            Assert.False(string.IsNullOrWhiteSpace(json));
            using var doc = JsonDocument.Parse(json); // throws if not valid JSON
        }
    }
}