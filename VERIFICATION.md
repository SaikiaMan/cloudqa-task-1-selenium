# Task 1 verification — 9 November 2026

Executed on Windows with .NET SDK 10.0.403, Selenium.WebDriver 4.52.0, installed Chrome, and Selenium Manager's ChromeDriver. Browser runs required execution outside the agent sandbox.

| Target | First Name | Gender | State | Process exit |
|---|---|---|---|---|
| Original CloudQA URL | PASS: live value Manob | PASS: Male selected | PASS: Afganistan [Afganistan] | 0 |
| native.html | PASS | PASS | PASS: Karnataka [KA] | 0 |
| modified.html | PASS | PASS | PASS: Tamil Nadu [TN] | 0 |
| accessible.html | PASS | PASS | PASS: Kerala [KL] | 0 |
| ambiguous.html | Expected FAIL: 2 exact candidates, refusing to guess | PASS | PASS: Karnataka [KA] | 1 |
| misleading.html | Expected FAIL: 2 nearby candidates, refusing to guess | PASS | PASS: Karnataka [KA] | 1 |

All three checks ran on each target. Both intentional failures occurred before text entry; fixture event guards would disable subsequent controls if an ambiguous or decoy input were edited. The fixture runner verified all expected outcomes and exit codes with guards enabled. Forms were left unsubmitted.

Commands executed:

```powershell
dotnet build
$env:CLOUDQA_HEADLESS = '1'
dotnet run --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Fixtures.ps1 -Headless
```

Build: zero warnings and zero errors. Fixture runner: five FIXTURE PASS results, exit 0.

Environment observations: Chrome inside the restricted sandbox crashed during session creation with `DevToolsActivePort file doesn't exist`. Running the same executable outside the sandbox succeeded. A PowerShell HTTP fetch failed with an unexpected connection closure; Selenium itself successfully loaded and exercised the original site. The live page's title was empty, but its actual fields and State options were available and verified. Initial conservative locators rejected duplicate IDs across forms; final locators scope runtime label associations to the owning form and support the page's radio caption spans.

The workspace had no Git metadata, so there was no Git diff/status or commit to inspect. Added .gitignore excludes build artifacts. No push or form submission was performed.
