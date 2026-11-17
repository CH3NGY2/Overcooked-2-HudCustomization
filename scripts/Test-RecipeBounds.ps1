param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2',
    [string]$MSBuild = 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe',
    [switch]$Entrance
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -GameRoot $GameRoot -MSBuild $MSBuild
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root ('.validation\recipe-bounds-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$managed = Join-Path $GameRoot 'Overcooked2_Data\Managed'
$core = Join-Path $GameRoot 'BepInEx\core'
$compiler = Join-Path (Split-Path -Parent $MSBuild) 'Roslyn\csc.exe'
$test = Join-Path $output 'RecipeBoundsBoundaryTests.exe'
& $compiler /noconfig /nologo /target:exe /langversion:3 /warnaserror+ /nostdlib+ "/out:$test" "/r:$managed\mscorlib.dll" "/r:$managed\System.dll" "/r:$managed\System.Core.dll" "/r:$managed\Assembly-CSharp.dll" "/r:$managed\UnityEngine.dll" "/r:$managed\UnityEngine.CoreModule.dll" "/r:$managed\UnityEngine.AnimationModule.dll" "/r:$managed\UnityEngine.UI.dll" (Join-Path $root 'tests\RecipeBoundsBoundaryTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Recipe bounds test compilation failed.' }
Add-Type -Path (Join-Path $core 'Mono.Cecil.dll')
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
foreach ($directory in @($managed, $core, $output)) { $resolver.AddSearchDirectory($directory) }
$parameters = [Mono.Cecil.ReaderParameters]::new(); $parameters.AssemblyResolver = $resolver
$harness = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($test, $parameters)
$artifact = Join-Path $root 'bin\Release\OC2HudCustomization.dll'
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($artifact, $parameters)
try {
    $boundary = $harness.MainModule.GetType('RecipeBoundsBoundaryTests')
    $rewrites = @()
    $cardType = $plugin.MainModule.GetType('OC2HudCustomization.RecipeCardState')
    $cardTypes = @($cardType) + @($cardType.NestedTypes)
    foreach ($method in @($cardTypes | ForEach-Object { $_.Methods })) {
        if (!$method.HasBody) { continue }
        foreach ($instruction in $method.Body.Instructions) {
            $member = $instruction.Operand
            if ($member -isnot [Mono.Cecil.MethodReference]) { continue }
            $replacement = switch ($member.FullName) {
                'System.Boolean UnityEngine.Object::op_Implicit(UnityEngine.Object)' { 'Alive' }
                'UnityEngine.Transform UnityEngine.Component::get_transform()' { 'TransformOf' }
                'UnityEngine.GameObject UnityEngine.Component::get_gameObject()' { 'ObjectOf' }
                'System.Boolean UnityEngine.GameObject::get_activeInHierarchy()' { 'Active' }
                'System.Boolean UnityEngine.GameObject::get_activeSelf()' { 'Active' }
                'System.Boolean UnityEngine.Behaviour::get_isActiveAndEnabled()' { 'GraphicActive' }
                'UnityEngine.RectTransform UnityEngine.UI.Graphic::get_rectTransform()' { 'GraphicRect' }
                'System.Int32 UnityEngine.Animator::GetLayerIndex(System.String)' { 'AnimationLayer' }
                'UnityEngine.AnimatorStateInfo UnityEngine.Animator::GetCurrentAnimatorStateInfo(System.Int32)' { 'AnimationState' }
                'System.Void UnityEngine.GameObject::SetActive(System.Boolean)' { 'SetActive' }
                'UnityEngine.Vector3 UnityEngine.Transform::InverseTransformPoint(UnityEngine.Vector3)' { 'ToLocal' }
                'System.Void UnityEngine.RectTransform::GetWorldCorners(UnityEngine.Vector3[])' { 'Corners' }
                'UnityEngine.Vector2 RectTransformExtension::get_PixelOffset()' { 'GetOffset' }
                'System.Void RectTransformExtension::set_PixelOffset(UnityEngine.Vector2)' { 'SetOffset' }
                default { if ($member.DeclaringType.FullName -eq 'UnityEngine.GameObject' -and $member.Name -eq 'GetComponent') { 'ComponentOf' }
                    elseif ($member.DeclaringType.FullName -eq 'UnityEngine.GameObject' -and $member.Name -eq 'GetComponentsInChildren' -and $member.Parameters.Count -eq 2) { 'Collect' } }
            }
            if ($replacement) {
                $hook = $boundary.Methods | Where-Object Name -eq $replacement
                $rewrites += [pscustomobject]@{ Method=$method.FullName; NativeBoundary=$member.FullName; Fixture=$replacement }
                $instruction.OpCode = [Mono.Cecil.Cil.OpCodes]::Call
                if ($replacement -eq 'ComponentOf') {
                    $generic = [Mono.Cecil.GenericInstanceMethod]::new($plugin.MainModule.ImportReference($hook))
                    $generic.GenericArguments.Add($plugin.MainModule.ImportReference($member.GenericArguments[0]))
                    $instruction.Operand = $generic
                } else { $instruction.Operand = $plugin.MainModule.ImportReference($hook) }
            }
        }
    }
    $patched = Join-Path $output 'OC2HudCustomization.dll'
    $plugin.Write($patched)
    $rewrites | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'native-boundaries.json') -Encoding UTF8
}
finally { $plugin.Dispose(); $harness.Dispose(); $resolver.Dispose() }
foreach ($dependency in @('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.AnimationModule.dll','UnityEngine.UI.dll','UnityEngine.UIModule.dll','UnityEngine.SharedInternalsModule.dll')) {
    Copy-Item -LiteralPath (Join-Path $managed $dependency) -Destination $output
}
foreach ($dependency in @('BepInEx.dll','0Harmony.dll')) { Copy-Item -LiteralPath (Join-Path $core $dependency) -Destination $output }
$scenario = if ($Entrance) { 'entrance' } else { 'bounds' }
& $test $patched $scenario 2>&1 | Tee-Object -FilePath (Join-Path $output 'result.txt')
$testExit = $LASTEXITCODE
Write-Output "Evidence: $output"
if ($testExit -ne 0) { throw "Recipe bounds regression failed: $testExit" }
