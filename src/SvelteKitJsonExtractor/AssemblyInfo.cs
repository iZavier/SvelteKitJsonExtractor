using System.Runtime.CompilerServices;

// so the test project can reach JsBraceExtractor directly - that's where
// the actual logic lives
[assembly: InternalsVisibleTo("SvelteKitJsonExtractor.Tests")]
