$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $manifest = Get-Content vendor\source-manifest.json -Raw | ConvertFrom-Json
    $provenance = Get-Content vendor\s0-provenance.json -Raw | ConvertFrom-Json
    if ($manifest.files.Count -ne $provenance.exportedFileCount -or
        $provenance.documentsWorkingTreeOverlay.Count -ne 26) {
        throw 'Composite S0 receipt counts do not match.'
    }
    foreach ($entry in $provenance.documentsWorkingTreeOverlay) {
        if ($entry.path.StartsWith('src\')) {
            $exported = @($manifest.files | Where-Object path -EQ $entry.path)
            if ($exported.Count -ne 1 -or $exported[0].sha256 -ne $entry.sha256) {
                throw "Frozen Documents production overlay mismatch: $($entry.path)"
            }
        }
    }
    $archive = Join-Path $root 'vendor\s0-source.zip'
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.archiveSha256) {
        throw 'Pinned source archive hash mismatch.'
    }
    $source = Join-Path $root '.build\s0'
    New-Item -ItemType Directory -Force $source, '.build\feed', 'evidence' | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $source, $true)
    foreach ($entry in $manifest.files) {
        $path = Join-Path $source $entry.path
        if (!(Test-Path $path) -or (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
            throw "Pinned source mismatch: $($entry.path)"
        }
    }
    $sdk = dotnet --version
    if ($LASTEXITCODE -ne 0 -or $sdk -ne '10.0.403') { throw "Expected SDK 10.0.403; found $sdk." }
    $names = @(
        'Microsoft.Extensions.AI.Abstractions', 'Microsoft.Extensions.AI', 'Microsoft.Extensions.AI.OpenAI',
        'Microsoft.Extensions.Documents.Abstractions', 'Microsoft.Extensions.DocumentExtraction.Abstractions',
        'Microsoft.Extensions.DocumentExtraction', 'Microsoft.Extensions.DataIngestion.Abstractions',
        'Microsoft.Extensions.DataIngestion'
    )
    foreach ($name in $names) {
        $project = Join-Path $source "src\Libraries\$name\$name.csproj"
        dotnet pack $project -p:TargetFrameworks=net10.0 -p:VersionPrefix=10.8.0 `
            -p:VersionSuffix=standup.s0.0ff84e7.1 -p:EnablePackageValidation=false `
            -p:DisableMinimumSupportedTfmCompatErrors=true -p:NuGetAudit=false `
            -p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false `
            -p:RepositoryUrl=https://github.com/dotnet/extensions `
            -p:RepositoryCommit=9112251596986014c3a4baccddf099bd43eb918b `
            -p:SourceRevisionId=9112251596986014c3a4baccddf099bd43eb918b `
            -o .build\feed -v:q *> "evidence\pack-$name.log"
        if ($LASTEXITCODE -ne 0) { throw "Snapshot pack failed: evidence\pack-$name.log" }
        Write-Host "Prepared $name 10.8.0-standup.s0.0ff84e7.1 (unofficial, net10 only)"
    }
    $packages = Get-ChildItem .build\feed\*.nupkg |
        Where-Object Name -Like '*.10.8.0-standup.s0.0ff84e7.1.nupkg' |
        ForEach-Object { @{ package = $_.Name; sha256 = (Get-FileHash $_.FullName).Hash.ToLowerInvariant() } }
    if ($packages.Count -ne $names.Count) { throw 'Expected exactly eight final S0 packages.' }
    @{ sdk = $sdk; snapshot = 'S0 composite; see vendor receipts'; localPackages = $packages } |
        ConvertTo-Json -Depth 5 | Set-Content evidence\package-identities.json
}
finally { Pop-Location }
