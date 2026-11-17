param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2',
    [string]$OutputDirectory = '',
    [string]$ExpectedSha256 = 'ECC1D3CEAC22000C0BEE2F1A544DAE7A07A8957318E598FBE24A14F118AB8FF5'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifact = Join-Path $projectRoot 'bin\Release\OC2HudCustomization.dll'
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'artifacts' }
if (!(Test-Path -LiteralPath $artifact)) { throw 'Build and verify Release before packaging.' }
$beforeHash = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash
if ($beforeHash -ne $ExpectedSha256) { throw 'The build differs from the expected tested artifact. Re-verify it before supplying a new -ExpectedSha256.' }

Add-Type -Path (Join-Path $GameRoot 'BepInEx\core\Mono.Cecil.dll')
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($artifact)
try {
    $plugin = @($assembly.MainModule.Types | Where-Object FullName -eq 'OC2HudCustomization.Plugin')
    if ($plugin.Count -ne 1) { throw 'Expected exactly one production plugin type.' }
    $metadata = @($plugin[0].CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' })
    if ($metadata.Count -ne 1) { throw 'Missing plugin metadata.' }
    $guid = [string]$metadata[0].ConstructorArguments[0].Value
    $name = [string]$metadata[0].ConstructorArguments[1].Value
    $version = [string]$metadata[0].ConstructorArguments[2].Value
    if ($guid -ne 'com.ch3ngyz.plugin.overcooked2.hudcustomization') { throw 'Unexpected plugin GUID.' }
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'This package entry point expects a stable three-part version.' }
    $numericVersion = [version]($version + '.0')
    if ($numericVersion -ne $assembly.Name.Version) { throw 'Assembly/plugin version mismatch.' }
    $fileVersion = @($assembly.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute' })
    if ($fileVersion.Count -ne 1 -or [string]$fileVersion[0].ConstructorArguments[0].Value -notin @($version, $numericVersion.ToString())) { throw 'File/plugin version mismatch.' }
    if ($assembly.MainModule.RuntimeVersion -ne 'v2.0.50727') { throw 'Expected the shipped legacy Mono profile.' }
    $references = @($assembly.MainModule.AssemblyReferences | ForEach-Object Name)
    if ($references -match 'Tests|TestRunner|nunit|xunit|OC2ModSettings|ConfigurationManager') { throw 'Test or hard settings-provider dependency found.' }
    $pending = [Collections.Generic.Queue[Mono.Cecil.TypeDefinition]]::new()
    foreach ($type in $assembly.MainModule.Types) { $pending.Enqueue($type) }
    while ($pending.Count -gt 0) {
        $type = $pending.Dequeue()
        if ($type.FullName -match 'UnityRecipeTests|BehaviorTests|BoundaryTests|TestRunner|HttpListener') { throw "Unexpected production type: $($type.FullName)" }
        foreach ($nested in $type.NestedTypes) { $pending.Enqueue($nested) }
    }
    if ($assembly.MainModule.Resources.Count -ne 0) { throw 'Recheck newly embedded resources before packaging.' }
    $runtime = $assembly.MainModule.RuntimeVersion
    $assemblyVersion = $assembly.Name.Version.ToString()
}
finally { $assembly.Dispose() }

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$zipPath = Join-Path $output ("OC2HudCustomization-v$version.zip")
if (Test-Path -LiteralPath $zipPath) { throw "Package exists; use a fresh output directory: $zipPath" }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $artifact, 'OC2HudCustomization.dll', [IO.Compression.CompressionLevel]::Optimal) | Out-Null }
finally { $archive.Dispose() }

$extract = Join-Path $output ('verify-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $extract | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($archive.Entries.Count -ne 1 -or $archive.Entries[0].FullName -cne 'OC2HudCustomization.dll') { throw 'Archive is not the single runtime-DLL allowlist.' }
    if ($archive.Entries[0].Length -ne (Get-Item -LiteralPath $artifact).Length) { throw 'Archive runtime size mismatch.' }
    $entries = @($archive.Entries | ForEach-Object { [ordered]@{ name=$_.FullName; bytes=$_.Length } })
}
finally { $archive.Dispose() }
[IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extract)
$extractedHash = (Get-FileHash -LiteralPath (Join-Path $extract 'OC2HudCustomization.dll') -Algorithm SHA256).Hash
if ($extractedHash -ne $beforeHash -or (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash -ne $beforeHash) { throw 'Runtime bytes changed during packaging.' }
$sourceCommit = (& git -C $projectRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0) { $sourceCommit = $null }
$record = [ordered]@{
    checkedAt=(Get-Date -Format o); sourceCommit=$sourceCommit; tag="v$version"; guid=$guid; name=$name
    pluginVersion=$version; assemblyVersion=$assemblyVersion; targetRuntime=$runtime; references=$references
    dllPath=$artifact; dllBytes=(Get-Item -LiteralPath $artifact).Length; dllSha256=$beforeHash
    zipPath=$zipPath; zipBytes=(Get-Item -LiteralPath $zipPath).Length
    zipSha256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash; entries=$entries
    extractedPath=$extract; extractedDllSha256=$extractedHash; rebuiltDuringPackaging=$false
}
$record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'package-verification.json') -Encoding UTF8
Write-Output "PASS $($entries.Count) runtime file; extracted SHA256 equals tested DLL: $beforeHash"
Get-Item -LiteralPath $zipPath | Select-Object FullName,Length
