param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2',
    [string]$MSBuild = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$MSBuild) {
    $found = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($found) { $MSBuild = $found.Source }
    else {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $MSBuild = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1
        }
    }
}
if (!$MSBuild -or !(Test-Path -LiteralPath $MSBuild)) { throw 'MSBuild not found; pass -MSBuild with the Visual Studio MSBuild.exe path.' }
& $MSBuild (Join-Path $projectRoot 'OC2HudCustomization.csproj') /t:Rebuild /p:Configuration=Release "/p:GameRoot=$GameRoot" /nologo /v:minimal
$buildExit = $LASTEXITCODE
if ($buildExit -ne 0) { throw "Build failed: $buildExit" }
Get-Item -LiteralPath (Join-Path $projectRoot 'bin\Release\OC2HudCustomization.dll') | Select-Object FullName,Length
