param(
    [string]$Output = '',
    [string]$BuildPath = ''
)
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
if ([string]::IsNullOrWhiteSpace($BuildPath)) {
    throw 'Select the tested current build explicitly with -BuildPath Builds/Windows-Istasyon/<folder>; no old build is selected automatically.'
}
if (-not $Output) {
    $Output = Join-Path $root ('Builds/Teslim/KuleNobeti_YazLab_Teslim-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')
}
if (-not [System.IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }
$outputPath = [System.IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw 'Output already exists. Use another filename; the existing package was not overwritten.' }
if (-not [System.IO.Path]::IsPathRooted($BuildPath)) { $BuildPath = Join-Path $root $BuildPath }
$build = [System.IO.Path]::GetFullPath($BuildPath)
foreach ($required in @('KuleNobeti.exe', 'UnityPlayer.dll', 'KuleNobeti_Data')) {
    if (-not (Test-Path -LiteralPath (Join-Path $build $required))) { throw "Build missing: $required ($build)" }
}
if (-not (Test-Path -LiteralPath (Join-Path $root 'Assets/Scenes/KuleNobeti.unity'))) { throw 'Game scene not generated.' }
$readme = Join-Path $root 'README.md'
if (Select-String -LiteralPath $readme -Pattern '\[DOLDUR\]' -Quiet) { throw 'Complete the report and actual test results in README.md before packaging.' }
foreach ($image in @('01-navmesh.png','02-nobetci.png','03-bitis.png')) {
    if (-not (Test-Path -LiteralPath (Join-Path $root ("docs/" + $image)))) { throw "Screenshot missing: $image" }
}
$stagingRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'Temp/TeslimStaging'))
$temp = [System.IO.Path]::GetFullPath((Join-Path $stagingRoot ([Guid]::NewGuid().ToString('N'))))
$stagingPrefix = $stagingRoot.TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $temp.StartsWith($stagingPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Package staging must stay inside the workspace Temp/TeslimStaging directory.'
}
try {
    $src = New-Item -ItemType Directory -Path (Join-Path $temp 'Kaynak') -Force
    foreach ($name in @('Assets','Packages','ProjectSettings','docs','Tools','README.md','.gitignore','ollama.example.json')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination $src.FullName -Recurse
    }
    Copy-Item -LiteralPath $build -Destination (Join-Path $temp 'WindowsBuild') -Recurse
    $tempPrefix = $temp.TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
    $files = @(Get-ChildItem -LiteralPath $temp -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            Path = $_.FullName.Substring($tempPrefix.Length).Replace('\', '/')
            Bytes = $_.Length
            SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
    $rootPrefix = $root.TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
    $selectedBuild = if ($build.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        $build.Substring($rootPrefix.Length).Replace('\', '/')
    } else { Split-Path $build -Leaf }
    [ordered]@{
        CreatedAt = (Get-Date -Format o)
        SelectedBuild = $selectedBuild
        Scope = 'Source project, report and the explicitly selected Windows build; hashes describe packaged files, not proof of unperformed tests.'
        Files = $files
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $temp 'TeslimBilgisi.json') -Encoding UTF8
    New-Item -ItemType Directory -Path (Split-Path $outputPath -Parent) -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($temp, $outputPath)
    $archive = [System.IO.Compression.ZipFile]::OpenRead($outputPath)
    try {
        $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        foreach ($entry in @('Kaynak/README.md', 'Kaynak/Assets/Scenes/KuleNobeti.unity',
            'Kaynak/Packages/manifest.json', 'Kaynak/ProjectSettings/ProjectVersion.txt',
            'WindowsBuild/KuleNobeti.exe', 'WindowsBuild/UnityPlayer.dll', 'TeslimBilgisi.json')) {
            if ($entryNames -notcontains $entry) { throw "ZIP verification failed; missing entry: $entry" }
        }
        if (-not ($entryNames | Where-Object { $_.StartsWith('WindowsBuild/KuleNobeti_Data/') })) {
            throw 'ZIP verification failed; KuleNobeti_Data contents are missing.'
        }
        $fileEntries = @($archive.Entries | Where-Object { -not $_.FullName.Replace('\', '/').EndsWith('/') })
        if ($fileEntries.Count -ne $files.Count + 1) { throw 'ZIP verification failed; file count differs from the manifest.' }
        $expectedHashes = @{}
        foreach ($file in $files) { $expectedHashes[$file.Path] = $file.SHA256 }
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            foreach ($entry in $fileEntries) {
                $entryName = $entry.FullName.Replace('\', '/')
                if ($entryName -eq 'TeslimBilgisi.json') { continue }
                if (-not $expectedHashes.ContainsKey($entryName)) { throw "ZIP verification failed; unexpected file: $entryName" }
                $stream = $entry.Open()
                try { $actualHash = [System.BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
                finally { $stream.Dispose() }
                if ($actualHash -cne $expectedHashes[$entryName]) { throw "ZIP verification failed; SHA256 mismatch: $entryName" }
            }
        }
        finally { $sha.Dispose() }
    }
    finally { $archive.Dispose() }
    "Created and verified: $outputPath"
    "Selected build: $selectedBuild"
    "Packaged files: $($files.Count + 1)"
}
finally {
    if (Test-Path -LiteralPath $temp) {
        $resolvedTemp = (Resolve-Path -LiteralPath $temp).Path
        if (-not $resolvedTemp.StartsWith($stagingPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing cleanup outside the verified package staging directory.'
        }
        Remove-Item -LiteralPath $resolvedTemp -Force -Recurse
    }
}
