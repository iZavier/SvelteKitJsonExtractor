using System;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SvelteKitJsonExtractor
{
    // Pulls the JSON payload out of a SvelteKit page's hydration script -
    // basically the object passed into kit.start(app, element, {...}).
    // Needs a real browser since the data only exists after JS runs.
    public class SvelteKitDataExtractor : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly bool _ownsDriver;
        private readonly TimeSpan _timeout;

        // Use this if you already have a WebDriver set up (auth, cookies, etc).
        // You're responsible for disposing it yourself.
        public SvelteKitDataExtractor(IWebDriver driver, TimeSpan? timeout = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _ownsDriver = false;
            _timeout = timeout ?? TimeSpan.FromSeconds(15);
        }

        private SvelteKitDataExtractor(IWebDriver driver, bool ownsDriver, TimeSpan timeout)
        {
            _driver = driver;
            _ownsDriver = ownsDriver;
            _timeout = timeout;
        }

        // Spins up a headless Firefox instance and owns it - Dispose() closes it.
        public static SvelteKitDataExtractor CreateWithFirefox(bool headless = true, TimeSpan? timeout = null)
        {
            var options = new OpenQA.Selenium.Firefox.FirefoxOptions();
            if (headless) options.AddArgument("-headless");
            var driver = new OpenQA.Selenium.Firefox.FirefoxDriver(options);
            return new SvelteKitDataExtractor(driver, ownsDriver: true, timeout ?? TimeSpan.FromSeconds(15));
        }

        // Grabs the raw JS object literal text from the kit.start(...) call.
        // Not valid JSON on its own (unquoted keys, void 0, etc) - use
        // ExtractAsJson if you want that cleaned up.
        public string ExtractRawStartPayload(string url)
        {
            _driver.Navigate().GoToUrl(url);
            WaitForDocumentReady();

            string scriptText = FindKitStartScriptText();
            if (scriptText == null)
                throw new InvalidOperationException(
                    "Couldn't find a kit.start(...) bootstrap script on this page - " +
                    "is it actually a SvelteKit page?");

            string payload = JsBraceExtractor.ExtractKitStartArgument(scriptText);
            if (payload == null)
                throw new InvalidOperationException(
                    "Found kit.start(...) but couldn't isolate the config object " +
                    "(braces didn't balance, or the format's unexpected).");

            return payload;
        }

        public string ExtractAsJson(string url)
        {
            string raw = ExtractRawStartPayload(url);
            var js = (IJavaScriptExecutor)_driver;

            // parens force JS to treat the leading { as an object literal
            // instead of a block statement
            const string script = @"
                try {
                    const obj = eval('(' + arguments[0] + ')');
                    return JSON.stringify(obj);
                } catch (e) {
                    return '__EXTRACT_ERROR__:' + e.message;
                }";

            var result = (string)js.ExecuteScript(script, raw);
            if (result != null && result.StartsWith("__EXTRACT_ERROR__:"))
                throw new InvalidOperationException(
                    "Browser couldn't evaluate the extracted payload: " +
                    result.Substring("__EXTRACT_ERROR__:".Length));

            return result;
        }

        public T ExtractAs<T>(string url, JsonSerializerOptions options = null)
        {
            string json = ExtractAsJson(url);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public JsonDocument ExtractAsJsonDocument(string url)
        {
            string json = ExtractAsJson(url);
            return JsonDocument.Parse(json);
        }

        private void WaitForDocumentReady()
        {
            var wait = new WebDriverWait(_driver, _timeout);
            wait.Until(d => ((IJavaScriptExecutor)d)
                .ExecuteScript("return document.readyState").Equals("complete"));

            // give the bootstrap script a beat to actually land in the DOM
            wait.Until(d => d.FindElements(By.TagName("script")).Count > 0);
        }

        private string FindKitStartScriptText()
        {
            foreach (var script in _driver.FindElements(By.TagName("script")))
            {
                string text;
                try
                {
                    text = script.GetAttribute("innerHTML") ?? "";
                }
                catch (StaleElementReferenceException)
                {
                    continue;
                }

                if (text.Contains("kit.start("))
                    return text;
            }
            return null;
        }

        public void Dispose()
        {
            if (_ownsDriver)
                _driver.Quit();
        }
    }
}
