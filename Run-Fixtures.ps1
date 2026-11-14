param([switch]$Headless)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
$previousHeadless = $env:CLOUDQA_HEADLESS
try {
    if ($Headless) { $env:CLOUDQA_HEADLESS = '1' }
    dotnet build
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $failed = 0
    foreach ($name in 'native', 'modified', 'accessible', 'ambiguous', 'misleading') {
        $url = ([uri](Join-Path $PSScriptRoot "fixtures\$name.html")).AbsoluteUri
        $output = @(& dotnet run --no-build -- $url 2>&1)
        $code = $LASTEXITCODE
        $output | ForEach-Object { Write-Host $_ }
        $negative = $name -in 'ambiguous', 'misleading'
        $firstResult = if ($negative) { 'FAIL' } else { 'PASS' }
        $expectedCode = if ($negative) { 1 } else { 0 }
        $joined = $output -join "`n"
        $ok = $code -eq $expectedCode -and $joined -match "(?m)^$firstResult First Name:" -and
            $joined -match '(?m)^PASS Gender:' -and $joined -match '(?m)^PASS State:'
        if ($negative) { $ok = $ok -and $joined -match 'refusing to guess' }
        if ($ok) { Write-Host "FIXTURE PASS: $name (exit $code)" }
        else { Write-Host "FIXTURE FAIL: $name (exit $code)"; $failed++ }
    }
    if ($failed) { throw "$failed fixture expectation(s) failed." }
}
finally {
    $env:CLOUDQA_HEADLESS = $previousHeadless
    Pop-Location
}
