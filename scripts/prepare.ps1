$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
function ConvertTo-NativePath([string]$Path) {
    $Path.Replace('\', [IO.Path]::DirectorySeparatorChar).Replace('/', [IO.Path]::DirectorySeparatorChar)
}
Push-Location $root
try {
    $manifest = Get-Content (Join-Path $root (ConvertTo-NativePath 'vendor\source-manifest.json')) -Raw | ConvertFrom-Json
    $provenance = Get-Content (Join-Path $root (ConvertTo-NativePath 'vendor\s0-provenance.json')) -Raw | ConvertFrom-Json
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
    $archive = Join-Path $root (ConvertTo-NativePath 'vendor\s0-source.zip')
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.archiveSha256) {
        throw 'Pinned source archive hash mismatch.'
    }
    $sdk = dotnet --version
    if ($LASTEXITCODE -ne 0 -or $sdk -ne '10.0.403') { throw "Expected SDK 10.0.403; found $sdk." }
    $source = Join-Path $root (ConvertTo-NativePath '.build\s0')
    $feed = Join-Path $root (ConvertTo-NativePath '.build\feed')
    $cache = Join-Path $root (ConvertTo-NativePath '.build\packages')
    $locks = Join-Path $root (ConvertTo-NativePath '.build\locks')
    $evidence = Join-Path $root 'evidence'
    New-Item -ItemType Directory -Force $source, $feed, $cache, $locks, $evidence | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $source, $true)
    foreach ($entry in $manifest.files) {
        $path = Join-Path $source (ConvertTo-NativePath $entry.path)
        if (!(Test-Path $path) -or (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
            throw "Pinned source mismatch: $($entry.path)"
        }
    }
    $names = @(
        'Microsoft.Extensions.AI.Abstractions', 'Microsoft.Extensions.AI', 'Microsoft.Extensions.AI.OpenAI',
        'Microsoft.Extensions.Documents.Abstractions', 'Microsoft.Extensions.DocumentExtraction.Abstractions',
        'Microsoft.Extensions.DocumentExtraction', 'Microsoft.Extensions.DataIngestion.Abstractions',
        'Microsoft.Extensions.DataIngestion'
    )
    foreach ($name in $names) {
        $project = Join-Path $source (ConvertTo-NativePath "src\Libraries\$name\$name.csproj")
        $log = Join-Path $evidence "pack-$name.log"
        # Preserve frozen CRLF bytes instead of enforcing the host's newline style.
        dotnet pack $project -p:TargetFrameworks=net10.0 -p:EnforceCodeStyleInBuild=false -p:VersionPrefix=10.8.0 `
            -p:VersionSuffix=standup.s0.0ff84e7.1 -p:EnablePackageValidation=false `
            -p:DisableMinimumSupportedTfmCompatErrors=true -p:NuGetAudit=false `
            "-p:RestorePackagesPath=$cache" `
            -p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false `
            -p:RepositoryUrl=https://github.com/dotnet/extensions `
            -p:RepositoryCommit=9112251596986014c3a4baccddf099bd43eb918b `
            -p:SourceRevisionId=9112251596986014c3a4baccddf099bd43eb918b `
            -o $feed -v:q *> $log
        if ($LASTEXITCODE -ne 0) { throw "Snapshot pack failed: $log" }
        Write-Host "Prepared $name 10.8.0-standup.s0.0ff84e7.1 (unofficial, net10 only)"
    }
    if (@(Get-ChildItem $feed -Filter '*.10.8.0-standup.s0.0ff84e7.1.nupkg').Count -ne $names.Count) {
        throw 'Expected exactly eight final S0 packages.'
    }
    $contentHashes = @{}
    $packages = foreach ($name in $names) {
        $file = Join-Path $feed "$name.10.8.0-standup.s0.0ff84e7.1.nupkg"
        $contentHashes[$name] = [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($file)))
        @{
            package = Split-Path $file -Leaf
            sha256 = (Get-FileHash $file).Hash.ToLowerInvariant()
            contentHash = $contentHashes[$name]
        }
        # Invalidate only this workspace's rebuilt version, never a shared global cache.
        $cachedVersion = Join-Path (Join-Path $cache $name.ToLowerInvariant()) '10.8.0-standup.s0.0ff84e7.1'
        if ([IO.Directory]::Exists($cachedVersion)) { [IO.Directory]::Delete($cachedVersion, $true) }
    }
    if ($packages.Count -ne $names.Count) { throw 'Expected exactly eight final S0 packages.' }

    $templates = @{
        'Standup.Core' = 'src\Standup.Core\packages.lock.json'
        'hero.Web' = 'hero\hero.Web\packages.lock.json'
        'hero.ServiceDefaults' = 'hero\hero.ServiceDefaults\packages.lock.json'
        'hero.AppHost' = 'hero\hero.AppHost\packages.lock.json'
        '01-extract' = 'samples\packages.lock.json'
        '02-ingest' = 'samples\packages.lock.json'
        '03-retrieve' = 'samples\packages.lock.json'
        'verify' = 'checks\packages.lock.json'
        'reset-index' = 'scripts\packages.lock.json'
    }
    foreach ($template in $templates.GetEnumerator()) {
        $lock = Get-Content (Join-Path $root (ConvertTo-NativePath $template.Value)) -Raw |
            ConvertFrom-Json -AsHashtable
        foreach ($framework in $lock.dependencies.Values) {
            foreach ($dependency in $framework.GetEnumerator()) {
                if ($contentHashes.ContainsKey($dependency.Key)) {
                    if ($dependency.Value.resolved -ne '10.8.0-standup.s0.0ff84e7.1') {
                        throw "Unexpected S0 version in committed lock template: $($dependency.Key)"
                    }
                    $dependency.Value.contentHash = $contentHashes[$dependency.Key]
                }
            }
        }
        $lock | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $locks "$($template.Key).packages.lock.json")
    }
    @{ sdk = $sdk; snapshot = 'S0 composite; see vendor receipts'; localPackages = $packages } |
        ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'package-identities.json')
}
finally { Pop-Location }
