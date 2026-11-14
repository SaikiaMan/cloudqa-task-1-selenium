# CloudQA internship — Task 1

C# console program using Selenium WebDriver. It fills First Name with `Manob`, selects Male inside Gender, and selects an actual option from State. Each check prints PASS/FAIL independently. Exit code is 0 only when all checks pass; failures return 1. The form is never submitted. Chrome closes after the checks.

## Prerequisites and setup (Windows PowerShell)

Install .NET 10 SDK and Google Chrome. This project retains Selenium.WebDriver 4.52.0. Selenium Manager resolves ChromeDriver; the first run needs network access to download it unless already cached. Chrome must be allowed to launch in your environment.

```powershell
Set-Location C:\Users\yunus\ResilientFormTests
dotnet restore
dotnet build
dotnet run
dotnet run -- "https://your-host/modified-form"
$LASTEXITCODE
```

For optional headless execution, set `$env:CLOUDQA_HEADLESS = '1'`; use `Remove-Item Env:CLOUDQA_HEADLESS` to return to visible Chrome.

## Fixtures

No web server is needed. The same executable accepts file URLs.

```powershell
$url = ([uri](Join-Path $PWD 'fixtures\modified.html')).AbsoluteUri
dotnet run -- $url
powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Fixtures.ps1 -Headless
```

The runner builds once, executes all five fixtures, checks field results and exit codes, and throws if any expectation fails. Omit `-Headless` for visible Chrome.

| Fixture | Purpose | Expected First Name / Gender / State | Exit |
|---|---|---|---|
| native.html | Native and wrapping labels; similar field and another Male group | PASS / PASS / PASS | 0 |
| modified.html | Renamed IDs/names/classes, reordered fields, nested wrappers, missing First Name label link; disabled State option | PASS / PASS / PASS | 0 |
| accessible.html | Exact accessible names and named radio group | PASS / PASS / PASS | 0 |
| ambiguous.html | Two equally plausible First Name inputs | FAIL / PASS / PASS | 1 |
| misleading.html | Unlinked First Name label near two text inputs | FAIL / PASS / PASS | 1 |

Fixture guards disable radios/dropdowns if the program edits a decoy field. Negative fixtures guard every text input: later Gender and State passes confirm that ambiguity was detected before typing. These guards test behavior and are not part of production locator logic.

## Locator approach to explain

`Fields.Find` collects visible, enabled controls of the required native type. Label text matches exactly after whitespace normalization and removal of trailing colon/required markers. It follows `for` to the current ID within the label's owning form, or finds a control wrapped by the label. IDs are read at runtime, never hard-coded. Form scoping matters because CloudQA repeats IDs in a separate Shadow DOM example.

If the link is absent/broken, it walks outward to the nearest container containing that control type, stopping before form/body/html. Multiple candidates cause a descriptive failure before interacting. Exact computed accessible names also contribute candidates, so an extra plausible control cannot silently win. Candidate elements are deduplicated, and exactly one is required.

`Fields.Group` resolves Gender from an exact label/legend or named fieldset/ARIA group. Male is resolved only within that group, by exact caption and radio type. The live page uses `span[for]` radio captions; those are supported within the group as well as native labels and accessible names. Female never matches Male by substring.

State is located by its label and select type, never its misleading placeholder. Options are printed at runtime, and the first enabled option with a nonempty value and non-placeholder text is clicked. Selection and live value are checked. First Name uses `GetDomProperty("value")`; Gender uses `Selected`.

## Known limitations

English labels and standard HTML text/radio/select controls are assumed. Shadow-root and iframe traversal, custom dropdown widgets, delayed SPA rendering, and stale-element retries are outside Task 1. Nearby-label fallback requires an unambiguous local container; broad containers with several candidates fail. Fully unlabeled controls cannot be inferred safely. Readonly/covered controls produce a field failure. The browser closes promptly; use recording or terminal output to capture the run.

## Verified on 9 November 2026

.NET SDK 10.0.403: build passed with zero warnings/errors. Chrome worked when launched outside the execution sandbox; inside it Chrome failed with `DevToolsActivePort file doesn't exist`. The live CloudQA URL was reachable through Selenium: all three checks passed, and State selected `Afganistan` (the site's spelling) from the actual options. No submission occurred.

All five fixture expectations passed: the three positive fixtures passed every field; the two negative fixtures rejected First Name and continued to pass Gender and State with exit 1. See `VERIFICATION.md` for the final run record.

## Short recording checklist

1. Show Program.cs, the default URL, and the three independent checks.
2. Run `dotnet run` against the original page; show three PASS lines and printed State options.
3. Open modified.html to show reordered fields, renamed attributes, nested wrappers, and Emergency Contact First Name. Run its file URL and show three passes.
4. Run ambiguous.html; show the useful ambiguity failure, continued Gender/State passes, and `$LASTEXITCODE` equal to 1.
5. Explain exact labels, runtime associations, Gender scope, uniqueness checks, and live-value assertions. Show that the form is unsubmitted.

The provided directory has no `.git` repository. Review the files, then initialize Git or copy them into your intended repository before committing; bin/obj are ignored.
