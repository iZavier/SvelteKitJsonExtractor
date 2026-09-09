# SvelteKitJsonExtractor

Pulls the JSON hydration payload out of a SvelteKit page - the object
that gets passed into `kit.start(app, element, {...})`. Needs a real
browser (Selenium) since that data isn't there until JS actually runs.

## Layout

```
SvelteKitJsonExtractor.sln
src/SvelteKitJsonExtractor/          - the library
tests/SvelteKitJsonExtractor.Tests/  - xUnit tests
  Integration/                       - Run off a real browser
```

Open the `.sln` in VS and both projects load together. Build once and
Test Explorer picks up the tests automatically.

## Usage

```csharp
using SvelteKitJsonExtractor;

using var extractor = SvelteKitDataExtractor.CreateWithFirefox(headless: true);

string json = extractor.ExtractAsJson("https://example.com/some/page/");

// or straight into a type
var data = extractor.ExtractAs<MyData>("https://example.com/some/page/");

using var doc = extractor.ExtractAsJsonDocument("https://example.com/some/page/");
var root = doc.RootElement;
```

Or we could do 

```csharp
using var extractor = SvelteKitDataExtractor.CreateWithFirefox(headless: true);

var payload = extractor.ExtractAs<SvelteKitPayload>(url, SvelteKitJson.Options);
//     ▲
//     └── THIS line runs Selenium: launches Firefox, navigates, waits,
//         scrapes the script tag, evals it, deserializes the JSON.
//         Everything browser-related happens here.

var ratios = payload.GetRouteData<FinancialRatiosRouteData>("financialData");
```

## How it actually works

Navigates to the page, waits for it to finish loading, then goes looking
through `<script>` tags for one containing `kit.start(`. Once it's found
that, it walks the source text (skipping over anything inside strings or
comments so it doesn't miscount) to find the balanced `{ ... }` object
that's the third argument to that call.

That text isn't valid JSON on its own - unquoted keys, `void 0`, that
kind of thing - so instead of writing a JS parser by hand, it just hands
the text to the browser's own JS engine (`eval` + `JSON.stringify`) and
lets that sort it out. Comes back as proper JSON you can deserialize
normally.

