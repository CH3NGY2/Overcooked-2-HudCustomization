param([string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2', [string]$MSBuild = '')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -GameRoot $GameRoot -MSBuild $MSBuild
if (!$MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $MSBuild = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1
}
$compiler = Join-Path (Split-Path -Parent $MSBuild) 'Roslyn\csc.exe'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root ('.validation\logic-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$bepInEx = Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'
$mono = Join-Path $GameRoot 'Overcooked2_Data\Managed'
$sources = @('tests\BehaviorTests.cs', 'tests\LocalizationTests.cs', 'tests\RecipeMotionTests.cs', 'tests\ConfigMigrationTests.cs',
    'src\PluginInfo.cs', 'src\ConfigMigration.cs', 'src\HudSettings.cs',
    'src\Localization\HudSettings.Display.cs', 'src\Localization\SettingsText.cs',
    'src\Localization\SettingsLanguageSync.cs', 'src\Localization\GameLanguage.cs',
    'src\ModSettingsSdk\ModSettingsAttributes.cs', 'src\Layout\HudLayoutMath.cs', 'src\Presentation\OwnedValue.cs', 'src\Recipes\RecipeMotion.cs') |
    ForEach-Object { Join-Path $root $_ }
& $compiler /noconfig /nologo /target:exe /langversion:3 /warnaserror+ /nostdlib+ "/out:$output\BehaviorTests.exe" "/r:$mono\mscorlib.dll" "/r:$mono\System.dll" "/r:$mono\System.Core.dll" "/r:$mono\Assembly-CSharp.dll" "/r:$mono\UnityEngine.dll" "/r:$mono\UnityEngine.CoreModule.dll" "/r:$bepInEx" @sources
$compileExit = $LASTEXITCODE
if ($compileExit -ne 0) { throw "Behavior test compile failed: $compileExit" }
Copy-Item -LiteralPath $bepInEx -Destination $output
& (Join-Path $output 'BehaviorTests.exe') $output $GameRoot 2>&1 | Tee-Object -FilePath (Join-Path $output 'result.txt')
$testExit = $LASTEXITCODE
if ($testExit -ne 0) { throw "Behavior tests failed: $testExit; evidence: $output" }
Write-Output "Evidence: $output"
