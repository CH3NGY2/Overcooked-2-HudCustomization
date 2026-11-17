param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2',
    [string]$MSBuild = '',
    [string]$ModSettingsDll = (Join-Path $PSScriptRoot '..\..\Overcooked-2-ModSettings\bin\Release\OC2ModSettings.dll')
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $ModSettingsDll)) { throw 'Build the ModSettings provider first, or pass -ModSettingsDll. No provider files are modified.' }
& (Join-Path $PSScriptRoot 'Build.ps1') -GameRoot $GameRoot -MSBuild $MSBuild
if (!$MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $MSBuild = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1
}
$compiler = Join-Path (Split-Path -Parent $MSBuild) 'Roslyn\csc.exe'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root ('.validation\modsettings-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$bepInEx = Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'
$managed = Join-Path $GameRoot 'Overcooked2_Data\Managed'
& $compiler /noconfig /nologo /target:exe /langversion:3 /warnaserror+ /nostdlib+ "/out:$output\ModSettingsConsumerTests.exe" "/r:$managed\mscorlib.dll" "/r:$managed\System.dll" "/r:$managed\System.Core.dll" "/r:$bepInEx" (Join-Path $root 'tests\ModSettingsConsumerTests.cs')
$compileExit = $LASTEXITCODE
if ($compileExit -ne 0) { throw "ModSettings consumer test compile failed: $compileExit" }
Copy-Item -LiteralPath $bepInEx -Destination $output
$consumer = Join-Path $root 'bin\Release\OC2HudCustomization.dll'
$provider = (Resolve-Path -LiteralPath $ModSettingsDll).Path
& (Join-Path $output 'ModSettingsConsumerTests.exe') $output $consumer $provider $GameRoot 2>&1 | Tee-Object -FilePath (Join-Path $output 'result.txt')
$testExit = $LASTEXITCODE
if ($testExit -ne 0) { throw "ModSettings consumer tests failed: $testExit; evidence: $output" }
[ordered]@{
    checkedAt = (Get-Date -Format o)
    level = 'desktop built-consumer and actual-provider metadata/sort; no Unity UI or Chainloader service execution'
    consumer = $consumer; consumerSha256 = (Get-FileHash -LiteralPath $consumer -Algorithm SHA256).Hash
    provider = $provider; providerSha256 = (Get-FileHash -LiteralPath $provider -Algorithm SHA256).Hash
    result = @(Get-Content -LiteralPath (Join-Path $output 'result.txt') -Encoding UTF8)
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'docs\evidence\localization-verification.json') -Encoding UTF8
Write-Output "Evidence: $output"
