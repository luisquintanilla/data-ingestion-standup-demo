$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed (exit $LASTEXITCODE). Bootstrap stopped." }
}

Push-Location $root
try {
    $env:STANDUP_PROVIDER = 'pdfpig'
    Remove-Item Env:\STANDUP_LIVE -ErrorAction SilentlyContinue
    Remove-Item Env:\STANDUP_BUDGET_PATH -ErrorAction SilentlyContinue
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:POWERSHELL_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    $node = node --version
    if ($LASTEXITCODE -ne 0 -or $node -notmatch '^v24\.') {
        throw "Expected Node.js 24.x (container: 24.22.1); found $node."
    }

    & (Join-Path $PSScriptRoot 'prepare.ps1')
    Invoke-Checked dotnet @('build', 'hero/hero.Web/hero.Web.csproj', '-v:q')
    foreach ($sample in @('01-extract', '02-ingest', '03-retrieve')) {
        Invoke-Checked dotnet @('build', "samples/$sample.cs", '-v:q')
    }
    Invoke-Checked dotnet @('run', '--file', 'checks/verify.cs')
    $npm = if ($IsWindows) { 'npm.cmd' } else { 'npm' }
    Invoke-Checked $npm @('ci', '--ignore-scripts')
    Invoke-Checked node @('build.mjs')

    Write-Host 'OFFLINE bootstrap complete. No servers, Azure credentials, indexing or model calls were started.'
    Write-Host 'Slides: set PORT=8187 in your shell, then run node serve.mjs.'
    Write-Host 'Native samples: dotnet run --file samples/01-extract.cs --no-build -- digital'
    Write-Host '                dotnet run --file samples/02-ingest.cs --no-build -- digital'
}
finally { Pop-Location }
