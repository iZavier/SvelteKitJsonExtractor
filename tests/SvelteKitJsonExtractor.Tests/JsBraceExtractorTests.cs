using Xunit;

namespace SvelteKitJsonExtractor.Tests
{
    public class JsBraceExtractorTests
    {
        [Fact]
        public void ExtractsSimpleConfigObject()
        {
            string source = "kit.start(app, element, { a: 1, b: 2 });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Equal("{ a: 1, b: 2 }", result);
        }

        [Fact]
        public void ExtractsNestedObjectsAndArrays()
        {
            string source = @"
                kit.start(app, element, {
                    node_ids: [0, 24, 170],
                    data: [{ type: ""data"", data: { a: { b: [1, 2, 3] } } }]
                });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.NotNull(result);
            Assert.StartsWith("{", result);
            Assert.EndsWith("}", result);
            Assert.Contains("node_ids", result);
            Assert.Contains("[1, 2, 3]", result);
        }

        [Fact]
        public void IgnoresBracesInsideDoubleQuotedStrings()
        {
            string source = @"kit.start(app, element, { title: ""a { fake } brace"", ok: true });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Equal(@"{ title: ""a { fake } brace"", ok: true }", result);
        }

        [Fact]
        public void IgnoresBracesInsideSingleQuotedStrings()
        {
            string source = "kit.start(app, element, { title: 'a { fake } brace', ok: true });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Equal("{ title: 'a { fake } brace', ok: true }", result);
        }

        [Fact]
        public void IgnoresBracesInsideTemplateLiterals()
        {
            string source = "kit.start(app, element, { title: `a ${1 + 1} { fake } brace`, ok: true });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Equal("{ title: `a ${1 + 1} { fake } brace`, ok: true }", result);
        }

        [Fact]
        public void IgnoresBracesInsideLineComments()
        {
            string source = "kit.start(app, element, {\n  a: 1, // a fake } brace here\n  b: 2\n});";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.NotNull(result);
            Assert.StartsWith("{", result);
            Assert.EndsWith("}", result);
            Assert.Contains("a: 1", result);
            Assert.Contains("b: 2", result);
        }

        [Fact]
        public void IgnoresBracesInsideBlockComments()
        {
            string source = "kit.start(app, element, {\n  a: 1, /* fake } brace */ b: 2\n});";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.NotNull(result);
            Assert.Contains("a: 1", result);
            Assert.Contains("b: 2", result);
        }

        [Fact]
        public void HandlesEscapedQuotesInsideStrings()
        {
            string source = @"kit.start(app, element, { title: ""she said \""hi\"" then { left }"", ok: true });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.NotNull(result);
            Assert.StartsWith("{", result);
            Assert.EndsWith("}", result);
        }

        [Fact]
        public void ReturnsNullWhenNoKitStartCallPresent()
        {
            string source = "someOtherFunction(app, element, { a: 1 });";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsNullWhenBracesAreUnbalanced()
        {
            string source = "kit.start(app, element, { a: 1, b: { c: 2 };";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsNullForNullInput()
        {
            Assert.Null(JsBraceExtractor.ExtractKitStartArgument(null!));
        }

        [Fact]
        public void HandlesRealisticSvelteKitBootstrapShape()
        {
            // trimmed-down version of what SvelteKit actually spits out
            string source = @"
                {
                    __sveltekit_1nzj0pg = { base: """" };
                    const element = document.currentScript.parentElement;
                    Promise.all([
                        import(""/_app/immutable/entry/start.js""),
                        import(""/_app/immutable/entry/app.js"")
                    ]).then(([kit, app]) => {
                        kit.start(app, element, {
                            node_ids: [0, 24, 170],
                            data: [
                                { type: ""data"", data: { session: null, user: null } },
                                { type: ""data"", data: { info: { symbol: ""WMT"" } } }
                            ],
                            form: null,
                            error: null
                        });
                    });
                }";

            string result = JsBraceExtractor.ExtractKitStartArgument(source);

            Assert.NotNull(result);
            Assert.Contains("node_ids", result);
            Assert.Contains("\"WMT\"", result);
            Assert.DoesNotContain("__sveltekit_1nzj0pg", result);
        }
    }
}
