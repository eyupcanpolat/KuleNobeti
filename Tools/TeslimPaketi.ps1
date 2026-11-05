param([string]$Output = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Output) { $Output = Join-Path (Split-Path $root -Parent) 'KuleNobeti_YazLab_Teslim.zip' }
if (Test-Path $Output) { throw 'Output already exists. Use another filename or remove the old package.' }
$build = Join-Path $root 'Builds/Windows'
foreach ($required in @('KuleNobeti.exe', 'UnityPlayer.dll', 'KuleNobeti_Data')) {
    if (-not (Test-Path (Join-Path $build $required))) { throw "Build missing: $required" }
}
if (-not (Test-Path (Join-Path $root 'Assets/Scenes/KuleNobeti.unity'))) { throw 'Game scene not generated.' }
$readme = Join-Path $root 'README.md'
if (Select-String -Path $readme -Pattern '\[DOLDUR\]' -Quiet) { throw 'Complete the report and actual test results in README.md before packaging.' }
foreach ($image in @('01-navmesh.png','02-nobetci.png','03-bitis.png')) {
    if (-not (Test-Path (Join-Path $root ("docs/" + $image)))) { throw "Screenshot missing: $image" }
}
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('KuleNobeti-' + [Guid]::NewGuid().ToString('N'))
try {
    $src = New-Item -Type Directory -Path (Join-Path $temp 'Kaynak')
    foreach ($name in @('Assets','Packages','ProjectSettings','docs','Tools','README.md','.gitignore','ollama.example.json')) {
        Copy-Item (Join-Path $root $name) $src.FullName -Recurse -Force
    }
    Copy-Item $build (Join-Path $temp 'WindowsBuild') -Recurse
    $outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($temp, $outputPath)
    "Created: $outputPath"
}
finally {
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Force -Recurse }
}
