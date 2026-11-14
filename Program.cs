using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

string url = args.Length > 0 ? args[0] : "https://app.cloudqa.io/home/AutomationPracticeForm";
int failures = 0;
ChromeDriver? driver = null;
try
{
    var options = new ChromeOptions();
    if (Environment.GetEnvironmentVariable("CLOUDQA_HEADLESS") == "1") options.AddArgument("--headless=new");
    driver = new ChromeDriver(options);
    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(45);
    driver.Navigate().GoToUrl(url);
    Console.WriteLine($"Target: {url}\nPage title: {driver.Title}");
    Check("First Name", () =>
    {
        var input = Fields.Find(driver, "First Name", "input[type='text'], input:not([type])");
        input.Clear();
        input.SendKeys("Manob");
        Require(input.GetDomProperty("value") == "Manob", "Live value is not Manob.");
        return "live value = Manob";
    });
    Check("Gender", () =>
    {
        var male = Fields.Find(Fields.Group(driver, "Gender"), "Male", "input[type='radio']");
        male.Click();
        Require(male.Selected, "Male radio is not selected.");
        return "Male selected";
    });
    Check("State", () =>
    {
        var select = Fields.Find(driver, "State", "select");
        Require(select.GetDomAttribute("multiple") == null, "Expected a single-select dropdown.");
        var choices = select.FindElements(By.TagName("option"));
        Console.WriteLine("State options: " + string.Join(" | ", choices.Select(o => $"{o.Text} [{o.GetDomProperty("value")}]")));
        var choice = choices.FirstOrDefault(o => o.Enabled && !string.IsNullOrWhiteSpace(o.GetDomProperty("value"))
            && !o.Text.Trim().StartsWith("--") && !o.Text.Trim().StartsWith("Select", StringComparison.OrdinalIgnoreCase));
        Require(choice != null, "State has no enabled, non-placeholder option with a value.");
        string value = choice!.GetDomProperty("value")!;
        choice.Click();
        Require(choice.Selected && select.GetDomProperty("value") == value, "Selected State did not persist.");
        return $"selected {choice.Text} [{value}]";
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Browser/navigation failure: {ex.GetType().Name}: {ex.Message}");
    foreach (string field in new[] { "First Name", "Gender", "State" })
        Console.WriteLine($"FAIL {field}: check could not run because browser/navigation setup failed.");
    failures++;
}
finally
{
    try { driver?.Quit(); }
    catch (Exception ex) { Console.WriteLine($"Browser cleanup failed: {ex.Message}"); failures++; }
}
return failures == 0 ? 0 : 1;

void Check(string field, Func<string> test)
{
    try { Console.WriteLine($"PASS {field}: {test()}"); }
    catch (Exception ex) { failures++; Console.WriteLine($"FAIL {field}: {ex.Message}"); }
}
static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

// No IDs, names, classes, field order, or placeholder text are hard-coded.
static class Fields
{
    static string Normalize(string text) => string.Join(" ", text.Split((char[]?)null,
        StringSplitOptions.RemoveEmptyEntries)).TrimEnd(':', ' ', '*');
    static bool Exact(IWebElement element, string text) => Normalize(element.Text) == text;
    static List<IWebElement> Controls(ISearchContext scope, string css) =>
        scope.FindElements(By.CssSelector(css)).Where(e => e.Displayed && e.Enabled).ToList();
    static IWebElement One(IEnumerable<IWebElement> elements, string description)
    {
        var matches = elements.Distinct().ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException($"{description}: expected one control, found {matches.Count}; refusing to guess.");
        return matches[0];
    }

    public static IWebElement Find(ISearchContext scope, string name, string css)
    {
        var controls = Controls(scope, css);
        var labels = scope.FindElements(By.CssSelector(css == "input[type='radio']" ? "label, span[for]" : "label")).Where(e => e.Displayed && Exact(e, name)).ToList();
        var candidates = new List<IWebElement>();
        foreach (var label in labels)
        {
            string? id = label.GetDomAttribute("for");
            // Examine all matching IDs in scope, so duplicate IDs cannot silently pick a winner.
            var form = label.FindElements(By.XPath("ancestor::form[1]")).FirstOrDefault();
            var local = form == null ? controls : controls.Intersect(Controls(form, css)).ToList();
            var linked = local.Where(c => !string.IsNullOrEmpty(id) && c.GetDomAttribute("id") == id).ToList();
            if (linked.Count > 0) { candidates.AddRange(linked); continue; }
            var wrapped = Controls(label, css);
            if (wrapped.Count > 0) { candidates.AddRange(wrapped); continue; }
            // Missing/broken association: use the nearest container with this control type.
            foreach (var parent in label.FindElements(By.XPath("ancestor::*")).Reverse())
            {
                if (parent.TagName is "form" or "body" or "html") break;
                var nearby = Controls(parent, css);
                if (nearby.Count == 0) continue;
                candidates.Add(One(nearby, $"Nearby '{name}' label"));
                break;
            }
        }
        // Accessible names also expose competing matches even when one label is linked.
        candidates.AddRange(controls.Where(c => Normalize(((WebElement)c).ComputedAccessibleLabel ?? "") == name));
        return One(candidates, $"Field '{name}' ({css})");
    }

    public static IWebElement Group(ISearchContext scope, string name)
    {
        var groups = scope.FindElements(By.CssSelector("fieldset, [role='group'], [role='radiogroup']"))
            .Where(e => e.Displayed && Normalize(((WebElement)e).ComputedAccessibleLabel ?? "") == name
                && Controls(e, "input[type='radio']").Count > 0).ToList();
        foreach (var label in scope.FindElements(By.CssSelector("label, legend"))
            .Where(e => e.Displayed && Exact(e, name)))
        {
            foreach (var parent in label.FindElements(By.XPath("ancestor::*")).Reverse())
            {
                if (parent.TagName is "form" or "body" or "html") break;
                if (Controls(parent, "input[type='radio']").Count == 0) continue;
                groups.Add(parent);
                break;
            }
        }
        return One(groups, $"Group '{name}'");
    }
}



