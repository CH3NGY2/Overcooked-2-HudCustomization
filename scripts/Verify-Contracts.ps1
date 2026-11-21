param([string]$GameRoot = 'E:\SteamLibrary\steamapps\common\Overcooked! 2')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$managed = Join-Path $GameRoot 'Overcooked2_Data\Managed'
$core = Join-Path $GameRoot 'BepInEx\core'
$artifact = Join-Path $projectRoot 'bin\Release\OC2HudCustomization.dll'
Add-Type -Path (Join-Path $core 'Mono.Cecil.dll')
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managed)
$resolver.AddSearchDirectory($core)
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'Assembly-CSharp.dll'), $parameters)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($artifact, $parameters)
try {
    $targetSpecs = @(
        @('RecipeFlowGUI', 'AddElement', 'OrderDefinitionNode', 'System.Single', 'VoidGeneric`1<RecipeFlowGUI/ElementToken>'),
        @('RecipeFlowGUI', 'LayoutWidgets'), @('RecipeFlowGUI', 'OnDestroy'),
        @('RecipeWidgetUIController', 'OnRefreshSubObjectProperties', 'UnityEngine.GameObject'),
        @('RecipeWidgetUIController', 'OnDestroy'),
        @('ClientIngredientContentGUI', 'OnOrderChanged', 'AssembledDefinitionNode'),
        @('ClientIngredientContentGUI', 'OnDisable'), @('ClientIngredientContentGUI', 'OnDestroy'),
        @('IngredientContentsUIContainer', 'RefreshIcons', 'UnityEngine.GameObject'),
        @('UISubElementContainer', 'RefreshSubElements'),
        @('ClientWorkableItem', 'StartSynchronising', 'UnityEngine.Component'),
        @('ClientCookingHandler', 'StartSynchronising', 'UnityEngine.Component'),
        @('ClientMixingHandler', 'StartSynchronising', 'UnityEngine.Component'),
        @('ClientHeatedStationGUI', 'OnHeatValueChanged', 'System.Single'),
        @('ClientWashingStation', 'Awake'), @('ClientWashable', 'StartSynchronising', 'UnityEngine.Component'),
        @('ClientFlammable', 'Ignite'), @('ScoreUIController', 'Awake'), @('LevelTimerUIController', 'Awake'),
        @('RectTransformExtension', 'UpdateAspectRatio'), @('UI_Move', 'UpdateGraphics'),
        @('ScoreUIController', 'ScoreUpdate', 'TeamID', 'TeamMonitor/TeamScoreStats'),
        @('GameUtils', 'InstantiateUIControllerOnScalingHUDCanvas', 'UnityEngine.GameObject'),
        @('PlayerControls', 'Awake'), @('T17EventSystem', 'Awake'), @('PauseMenuManager', 'UpdateWhileClosed'),
        @('FrontendMenuBehaviour', 'Show', 'GamepadUser', 'BaseMenuBehaviour', 'UnityEngine.GameObject', 'System.Boolean')
    )
    $targets = foreach ($spec in $targetSpecs) {
        $type = $game.MainModule.Types | Where-Object FullName -eq $spec[0]
        $expected = @($spec | Select-Object -Skip 2)
        $matches = @($type.Methods | Where-Object { $_.Name -eq $spec[1] -and (@($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join '|') -eq ($expected -join '|') })
        if ($matches.Count -ne 1) { throw "Expected one method: $($spec -join ', '); found $($matches.Count)" }
        [pscustomobject]@{ Signature=$matches[0].FullName; Access=if($matches[0].IsPublic){'public'}elseif($matches[0].IsFamily){'protected'}else{'private'} }
    }
    $fields = foreach ($spec in @(
        @('RecipeFlowGUI','m_widgets'), @('RecipeFlowGUI','m_dyingWidgets'), @('RecipeFlowGUI','m_ordererWidgets'),
        @('RecipeFlowGUI','m_distanceBetweenOrders'), @('RecipeFlowGUI','m_distanceFromEndOfScreen'),
        @('RecipeWidgetUIController','m_recipeTiles'), @('ClientIngredientContentGUI','m_uiInstance'),
        @('UISubElementContainer','m_container'), @('RecipeWidgetTile','m_backgroundTop'), @('IngredientContentsUIContainer','m_icons'),
        @('IngredientContentsUIContainer','m_emptySprite'),
        @('ClientWorkableItem','m_guibar'), @('ClientCookingHandler','m_gui'), @('ClientMixingHandler','m_progressUI'),
        @('ClientHeatedStationGUI','m_uiInstance'), @('ClientWashingStation','m_progressUI'),
        @('ClientWashable','m_progressBar'), @('ClientFlammable','m_fireGUIBar'),
        @('ProgressUIController','m_progressBar'), @('CookingUIController','m_progressBar'), @('HeatValueUIController','m_progressBar'),
        @('TimeManager','m_arbitrationSupressors')
    )) {
        $type = $game.MainModule.Types | Where-Object FullName -eq $spec[0]
        $field = @($type.Fields | Where-Object Name -eq $spec[1])
        if ($field.Count -ne 1) { throw "Missing field $($spec -join '.')" }
        [pscustomobject]@{ Signature=$field[0].FullName; Access=if($field[0].IsPublic){'public'}elseif($field[0].IsFamily){'protected'}else{'private'} }
    }
    $violations = [Collections.Generic.List[string]]::new()
    $gameReferenceCount = 0
    $pendingTypes = [Collections.Generic.Queue[Mono.Cecil.TypeDefinition]]::new()
    foreach($type in $plugin.MainModule.Types) { $pendingTypes.Enqueue($type) }
    while($pendingTypes.Count -gt 0) {
        $type = $pendingTypes.Dequeue()
        foreach($nested in $type.NestedTypes) { $pendingTypes.Enqueue($nested) }
        foreach($method in $type.Methods) {
            if (!$method.HasBody) { continue }
            foreach($instruction in $method.Body.Instructions) {
                $member = $instruction.Operand
                if (($member -is [Mono.Cecil.MethodReference] -or $member -is [Mono.Cecil.FieldReference]) -and $member.DeclaringType.Scope.Name -eq 'Assembly-CSharp') {
                    $gameReferenceCount++
                    $resolved = $member.Resolve()
                    if (!$resolved.IsPublic) { $violations.Add("$($method.FullName) -> $($member.FullName)") }
                }
            }
        }
    }
    if ($violations.Count) { throw "Non-public direct access: $($violations -join '; ')" }
    $references = @($plugin.MainModule.AssemblyReferences | ForEach-Object { $_.Name })
    if (@($references | Where-Object { $_ -match 'HostUtilities|RecipeViewer|ModSettings|Tests' }).Count) { throw 'Unexpected third-party or test dependency.' }
    $identity = $plugin.MainModule.Types | Where-Object FullName -eq 'OC2HudCustomization.Plugin'
    $metadata = $identity.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
    if ($metadata.ConstructorArguments[0].Value -ne 'com.ch3ngyz.plugin.overcooked2.hudcustomization') {
        throw 'Production GUID differs from the user-confirmed identity.'
    }
    $serviceGuid = 'com.ch3ngyz.plugin.overcooked2.modsettings'
    $optionalService = @($identity.CustomAttributes | Where-Object {
        $_.AttributeType.FullName -eq 'BepInEx.BepInDependency' -and $_.ConstructorArguments[0].Value -eq $serviceGuid
    })
    if ($optionalService.Count -ne 1 -or [int]$optionalService[0].ConstructorArguments[1].Value -ne 2) {
        throw 'ModSettings must be an optional soft dependency.'
    }
    $languageMembers = foreach ($spec in @(@('SteamPlayerManager','get_Initialized'), @('Localization','GetLanguage'))) {
        $languageType = $game.MainModule.Types | Where-Object FullName -eq $spec[0]
        $member = @($languageType.Methods | Where-Object { $_.Name -eq $spec[1] -and $_.Parameters.Count -eq 0 })
        if ($member.Count -ne 1 -or !$member[0].IsPublic -or !$member[0].IsStatic) { throw "Language contract mismatch: $($spec -join '.')" }
        [pscustomobject]@{ Signature=$member[0].FullName; PublicStatic=$true }
    }
    $gameLanguageType = $game.MainModule.Types | Where-Object FullName -eq 'Localization'
    $gameLanguageReader = $gameLanguageType.Methods | Where-Object Name -eq 'GetLanguage'
    $languageCalls = @($gameLanguageReader.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } | ForEach-Object { $_.Operand.FullName })
    if (!($languageCalls -match 'SteamPlayerManager::get_Initialized') -or !($languageCalls -match 'SteamApps::GetCurrentGameLanguage')) {
        throw 'Recheck game language readiness and selected-language source against the installed DLL.'
    }
    $refresh = $identity.Methods | Where-Object Name -eq 'RefreshSettingsDisplay'
    $refreshCalls = @($refresh.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } | ForEach-Object { $_.Operand.FullName })
    if (!($refreshCalls -match 'ModSettingsClient::Connect') -or !($refreshCalls -match 'ModSettingsClient::Refresh')) { throw 'Missing SDK refresh path.' }
    $editor = $plugin.MainModule.Types | Where-Object FullName -eq 'OC2HudCustomization.HudEditor'
    $editorUpdate = $editor.Methods | Where-Object Name -eq 'Update'
    $keyCalls = @($editorUpdate.Body.Instructions | Where-Object {
        $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.DeclaringType.FullName -eq 'UnityEngine.Input'
    })
    if ($keyCalls.Count -ne 1 -or $keyCalls[0].Operand.Name -ne 'GetKeyDown' -or [int]$keyCalls[0].Previous.Operand -ne 96) {
        throw 'Editor exit must read BackQuote only, not all-key state.'
    }
    $providerPath = Join-Path $GameRoot 'BepInEx\plugins\OC2ModSettings\OC2ModSettings.dll'
    $provider = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($providerPath, $parameters)
    try {
        $shortcut = $provider.MainModule.Types | Where-Object FullName -eq 'OC2ModSettings.NativeShortcut'
        $shortcutTick = @($shortcut.Methods | Where-Object { $_.Name -eq 'Tick' -and $_.ReturnType.FullName -eq 'System.Void' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'System.Boolean' })
        if ($shortcutTick.Count -ne 1) { throw 'Installed ModSettings shortcut coordination contract changed.' }
        $shortcutSignature = $shortcutTick[0].FullName
        $providerVersion = $provider.Name.Version.ToString()
    }
    finally { $provider.Dispose() }
    $result = [ordered]@{
        checkedAt=(Get-Date -Format o); level='static-metadata-and-compiled-IL; no Unity UI execution'
        gameAssembly=$game.MainModule.FileName; gameSha256=(Get-FileHash -LiteralPath $game.MainModule.FileName -Algorithm SHA256).Hash
        gameMvid=$game.MainModule.Mvid.ToString(); gameRuntime=$game.MainModule.RuntimeVersion
        pluginPath=$artifact; pluginSha256=(Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash
        pluginGuid=$metadata.ConstructorArguments[0].Value; pluginVersion=$metadata.ConstructorArguments[2].Value
        assemblyVersion=$plugin.Name.Version.ToString(); targetRuntime=$plugin.MainModule.RuntimeVersion
        targets=@($targets); fields=@($fields); directGameMemberReferences=$gameReferenceCount
        nonPublicDirectAccess=@($violations); dependencies=$references
        languageContract=@($languageMembers); gameLanguageCalls=$languageCalls
        modSettingsSoftDependency=$serviceGuid; modSettingsRefreshCalls=$refreshCalls
        editorExitKey='BackQuote (96); one Input.GetKeyDown call'; editorShortcutContract=$shortcutSignature
        installedModSettingsVersion=$providerVersion; installedModSettingsSha256=(Get-FileHash -LiteralPath $providerPath -Algorithm SHA256).Hash
        runtimeFiles=@(Get-ChildItem -LiteralPath (Split-Path -Parent $artifact) -Filter '*.dll' | ForEach-Object Name)
    }
    $output = Join-Path $projectRoot 'docs\evidence\implementation-verification.json'
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $output -Encoding UTF8
    Write-Output "PASS $($targets.Count) patch signatures, $($fields.Count) reflected fields, $gameReferenceCount direct public member references; 0 private direct accesses. $output"
}
finally { $game.Dispose(); $plugin.Dispose(); $resolver.Dispose() }
