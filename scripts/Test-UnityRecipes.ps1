param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2',
    [string]$MSBuild = 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe',
    [switch]$KeepProductionBuild,
    [switch]$Editor
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue) { throw 'Player game running; no deployment performed.' }
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Build.ps1') -GameRoot $GameRoot -MSBuild $MSBuild
& $MSBuild (Join-Path $root 'tests\UnityRecipeTests.csproj') /t:Rebuild "/p:GameRoot=$GameRoot" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Unity test plugin build failed.' }
$run = Join-Path $root ('.validation\unity-recipes-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$backup = Join-Path $run 'backup'
New-Item -ItemType Directory -Path $backup -Force | Out-Null
function Hash([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
    return '-'
}
$plugins = Join-Path $GameRoot 'BepInEx\plugins'
$configRoot = Join-Path $GameRoot 'BepInEx\config'
$legacyMigrationExpected = (Test-Path -LiteralPath (Join-Path $configRoot 'com.oc2.hudcustomization.cfg')) -and
    !(Test-Path -LiteralPath (Join-Path $configRoot 'com.ch3ngyz.plugin.overcooked2.hudcustomization.cfg'))
$production = Join-Path $plugins 'OC2HudCustomization.dll'
$test = Join-Path $plugins 'OC2HudCustomization.UnityRecipeTests.dll'
$log = Join-Path $GameRoot 'BepInEx\LogOutput.log'
$manifest = [Collections.Generic.List[object]]::new()
foreach ($target in @($production, $test, $log) + @(Get-ChildItem -LiteralPath $configRoot -Filter '*.cfg' -File -Recurse | ForEach-Object FullName)) {
    $saved = Join-Path $backup ($manifest.Count.ToString('D3') + '-' + [IO.Path]::GetFileName($target))
    $original = Hash $target
    if ($original -ne '-') { Copy-Item -LiteralPath $target -Destination $saved }
    $manifest.Add([pscustomobject]@{ Target=$target; Backup=$saved; Original=$original; Expected=$original })
}
$known = @{}; foreach ($item in $manifest) { $known[$item.Target] = $item }
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'manifest.json') -Encoding UTF8
$failure = $null; $seen = $false; $restoreErrors = [Collections.Generic.List[string]]::new()
try {
    if (Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue) { throw 'Player game started during preparation.' }
    foreach ($item in $manifest) { if ((Hash $item.Target) -ne $item.Original) { throw "Concurrent file change before deployment: $($item.Target)" } }
    Copy-Item -LiteralPath (Join-Path $root 'bin\Release\OC2HudCustomization.dll') -Destination $production -Force
    $known[$production].Expected = Hash $production
    Copy-Item -LiteralPath (Join-Path $root 'bin\UnityTests\OC2HudCustomization.UnityRecipeTests.dll') -Destination $test -Force
    $known[$test].Expected = Hash $test
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'manifest.json') -Encoding UTF8
    $steam = (Get-Process -Name steam -ErrorAction SilentlyContinue | Select-Object -First 1).Path
    if (!$steam) { $steam = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam').SteamExe }
    $arguments = @('-applaunch','728880','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080',
        '-logFile',('"' + (Join-Path $run 'player.log') + '"'),'-hudRecipeTest',('"' + $run + '"'))
    if ($legacyMigrationExpected) { $arguments += '-hudRecipeLegacyMigration' }
    if ($Editor) { $arguments += '-hudEditorTest' }
    Start-Process -FilePath $steam -ArgumentList $arguments -WindowStyle Hidden | Out-Null
    Write-Output "Unity recipe run: $run"
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) {
        $running = @(Get-CimInstance Win32_Process -Filter "Name='Overcooked2.exe'")
        $owned = @($running | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($run) })
        if ($owned.Count -gt 0) { $seen = $true }
        if ($seen -and $owned.Count -eq 0) { break }
        if ($running.Count -gt $owned.Count) { throw 'An unowned game process appeared; preserving its files.' }
        Start-Sleep -Milliseconds 500
    }
    if (!$seen) { throw 'No game process with this run marker appeared.' }
    if ($owned.Count -gt 0) { throw 'Unity recipe scenario timed out.' }
    $resultPath = Join-Path $run 'result.txt'
    if (!(Test-Path -LiteralPath $resultPath)) { throw 'Unity recipe result missing.' }
    $result = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8
    Write-Output $result
    if (!$result.StartsWith('PASS:')) { throw 'Unity recipe assertions failed.' }
}
catch { $failure = $_.ToString(); $failure | Set-Content -LiteralPath (Join-Path $run 'runner-error.txt') -Encoding UTF8; Write-Warning $failure }
finally {
    foreach ($process in @(Get-CimInstance Win32_Process -Filter "Name='Overcooked2.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($run) })) {
        Stop-Process -Id $process.ProcessId
        Wait-Process -Id $process.ProcessId -Timeout 15 -ErrorAction SilentlyContinue
    }
    if (Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue) {
        $restoreErrors.Add('Player game running; preserved installation and backups without restoration.')
    }
    else {
        if (Test-Path -LiteralPath $log) { Copy-Item -LiteralPath $log -Destination (Join-Path $run 'bepinex.log'); $known[$log].Expected = Hash $log }
        $attestation = Join-Path $run 'configs-final.tsv'
        if (Test-Path -LiteralPath $attestation) {
            foreach ($line in Get-Content -LiteralPath $attestation -Encoding UTF8) {
                $parts = $line.Split("`t"); $path = [IO.Path]::GetFullPath($parts[1])
                $safeRoot = [IO.Path]::GetFullPath($configRoot).TrimEnd('\') + '\'
                if (!$path.StartsWith($safeRoot, [StringComparison]::OrdinalIgnoreCase)) { $restoreErrors.Add("Attested config outside config root: $path"); continue }
                if (!$known.ContainsKey($path)) {
                    $item = [pscustomobject]@{ Target=$path; Backup=''; Original='-'; Expected=$parts[0] }
                    $manifest.Add($item); $known[$path] = $item
                }
                else { $known[$path].Expected = $parts[0] }
            }
        }
        $saveManifest = Join-Path $run 'saves.tsv'
        if (!(Test-Path -LiteralPath $saveManifest) -or (Get-Item -LiteralPath $saveManifest).Length -eq 0) { $restoreErrors.Add('Non-empty original-save manifest missing.') }
        else {
            foreach ($line in Get-Content -LiteralPath $saveManifest -Encoding UTF8) {
                $parts = $line.Split("`t"); if ((Hash $parts[1]) -ne $parts[0]) { $restoreErrors.Add("Original save changed: $($parts[1])") }
            }
        }
        foreach ($item in $manifest) {
            try {
                $current = Hash $item.Target
                if ($current -ne $item.Expected -and $current -ne $item.Original) { throw "Unexpected file state preserved: $($item.Target)" }
                if ($item.Target -eq $production -and $KeepProductionBuild) {
                    if ($current -ne $item.Expected) { throw 'Kept production build hash mismatch.' }
                    continue
                }
                if ($current -eq $item.Original) { continue }
                if ($item.Original -eq '-') { Remove-Item -LiteralPath $item.Target }
                else { Copy-Item -LiteralPath $item.Backup -Destination $item.Target -Force }
                if ((Hash $item.Target) -ne $item.Original) { throw "Restore hash mismatch: $($item.Target)" }
            }
            catch { $restoreErrors.Add($_.ToString()) }
        }
        foreach ($cfg in Get-ChildItem -LiteralPath $configRoot -Filter '*.cfg' -File -Recurse) {
            if (!$known.ContainsKey($cfg.FullName)) { $restoreErrors.Add("Unattested new config preserved: $($cfg.FullName)") }
        }
    }
    [ordered]@{
        run=$run; scenarioError=$failure; restorationErrors=@($restoreErrors); keepProduction=[bool]$KeepProductionBuild
        productionPath=$production; productionSha256=(Hash $production); testDllRemoved=!(Test-Path -LiteralPath $test)
        files=@($manifest | ForEach-Object { [pscustomobject]@{ Path=$_.Target; Original=$_.Original; Expected=$_.Expected; Final=(Hash $_.Target) } })
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'verification.json') -Encoding UTF8
    Write-Output "Evidence and restoration: $run"
}
if ($restoreErrors.Count -gt 0) { throw ($restoreErrors -join "`n") }
if ($failure) { throw $failure }
Write-Output 'PASS Unity scenario, original saves, configuration and temporary-plugin restoration.'
