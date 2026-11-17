using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.ch3ngyz.plugin.overcooked2.hudcustomization.tests", "HUD Recipe Unity Tests", "1.0.0")]
[BepInDependency("com.ch3ngyz.plugin.overcooked2.hudcustomization")]
public sealed partial class UnityRecipeTests : BaseUnityPlugin
{
    private static string output;
    private static bool profileReady;
    private static readonly Dictionary<string, string> saves = new Dictionary<string, string>();
    private Harmony harmony;
    private int assertions;
    private void Awake()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == "-hudRecipeTest") output = arguments[i + 1];
        if (string.IsNullOrEmpty(output)) { enabled = false; return; }
        Directory.CreateDirectory(Path.Combine(output, "saves"));
        harmony = new Harmony("com.ch3ngyz.plugin.overcooked2.hudcustomization.tests");
        harmony.Patch(AccessTools.Method(typeof(PCSaveManager), "GetFileAddress"), null, new HarmonyMethod(typeof(UnityRecipeTests), "Redirect"));
        Type diy = AccessTools.TypeByName("OC2DIYLevel.DIYLevelSaveManager");
        if (diy != null) harmony.Patch(AccessTools.Method(diy, "GetFileAddress"), null, new HarmonyMethod(typeof(UnityRecipeTests), "Redirect"));
        harmony.Patch(AccessTools.Method(typeof(PCSaveManager), "LoadProfile"), null, new HarmonyMethod(typeof(UnityRecipeTests), "Loaded"));
        StartCoroutine(Protected());
    }
    private static string Hash(string path)
    {
        if (!File.Exists(path)) return "-";
        using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
    private static void Redirect(ref string __result)
    {
        string target;
        if (!saves.TryGetValue(__result, out target))
        {
            using (SHA256 sha = SHA256.Create())
                target = Path.Combine(Path.Combine(output, "saves"), BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(__result))).Replace("-", "") + ".save").Replace('\\', '/');
            if (File.Exists(__result)) File.Copy(__result, target);
            File.AppendAllText(Path.Combine(output, "saves.tsv"), Hash(__result) + "\t" + __result + Environment.NewLine);
            saves.Add(__result, target);
        }
        __result = target;
    }
    private static void Loaded(ref IEnumerator<SaveLoadResult?> __result) { profileReady = false; __result = ObserveProfile(__result); }
    private static IEnumerator<SaveLoadResult?> ObserveProfile(IEnumerator<SaveLoadResult?> original)
    {
        using (original) while (original.MoveNext())
        {
            SaveLoadResult? result = original.Current;
            if (result == SaveLoadResult.Exists || result == SaveLoadResult.NotExist) profileReady = true;
            yield return result;
        }
    }
    private void Check(bool passed, string label)
    {
        assertions++;
        File.AppendAllText(Path.Combine(output, "assertions.txt"), (passed ? "PASS " : "FAIL ") + label + Environment.NewLine);
        if (!passed) throw new InvalidOperationException(label);
    }
    private void Phase(string name) { File.WriteAllText(Path.Combine(output, "phase.txt"), name); Logger.LogInfo("Recipe test: " + name); }
    private void KnownNotice()
    {
        foreach (T17DialogBox dialog in Resources.FindObjectsOfTypeAll<T17DialogBox>())
        {
            if (!dialog.IsActive || dialog.m_Title == null || dialog.m_Message == null) continue;
            string title = dialog.m_Title.text, message = dialog.m_Message.text;
            if ((title.Contains("FastInit") || title.Contains("快速载入") || message.Contains("FastInit v1.9")) && dialog.m_ConfirmButton != null) dialog.m_ConfirmButton.onClick.Invoke();
            if ((title == "MOD 更新" || title == "MOD Update") && dialog.m_DeclineButton != null) dialog.m_DeclineButton.onClick.Invoke();
        }
    }
    private IEnumerator Protected()
    {
        Stack<IEnumerator> stack = new Stack<IEnumerator>(); stack.Push(Run()); Exception error = null;
        while (stack.Count > 0 && error == null)
        {
            IEnumerator top = stack.Peek(); bool next = false; object value = null;
            try { KnownNotice(); next = top.MoveNext(); if (next) value = top.Current; }
            catch (Exception exception) { error = exception; }
            if (error != null) break;
            if (!next) { stack.Pop(); continue; }
            IEnumerator child = value as IEnumerator;
            if (child != null) stack.Push(child); else yield return value;
        }
        if (error != null)
        {
            Logger.LogError(error);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "failure.png"));
            File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL after " + assertions + " assertions\n" + error);
        }
        else File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity "
            + (Array.IndexOf(Environment.GetCommandLineArgs(), "-hudEditorTest") >= 0 ? "HUD editor" : "recipe")
            + " assertions; native UI fixture in kitchen, scripted startup/input.");
        foreach (string config in Directory.GetFiles(Paths.ConfigPath, "*.cfg", SearchOption.AllDirectories))
            File.AppendAllText(Path.Combine(output, "configs-final.tsv"), Hash(config) + "\t" + config + Environment.NewLine);
        yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(.5f);
        Application.Quit();
    }
    private IEnumerator Run()
    {
        Phase("native engagement and profile"); bool engaged = false;
        FieldInfo checking = AccessTools.Field(typeof(StartScreenFlow), "m_bCheckingForEngagement");
        MethodInfo completed = AccessTools.Method(typeof(StartScreenFlow), "OnEngagementFinished");
        FrontendRootMenu root = null; float deadline = Time.realtimeSinceStartup + 100f;
        while (Time.realtimeSinceStartup < deadline)
        {
            StartScreenFlow flow = StartScreenFlow.Instance; PlayerManager player = GameUtils.RequestManager<PlayerManager>();
            if (!engaged && flow != null && player != null && !player.HasPlayer() && SteamPlayerManager.Initialized && !LoadingScreenFlow.IsLoading && (bool)checking.GetValue(null))
            {
                engaged = true; checking.SetValue(null, false);
                player.StartGameownerEngagement(ControlPadInput.PadNum.One, null, delegate(GamepadUser user) { completed.Invoke(flow, new object[] { user }); });
            }
            root = T17FrontendFlow.Instance == null ? null : T17FrontendFlow.Instance.m_Rootmenu;
            if (profileReady && root != null && root.isActiveAndEnabled && root.CurrentGamepadUser != null && root.CachedEventSystem != null
                && !root.CachedEventSystem.IsDisabled() && !LoadingScreenFlow.IsLoading && T17DialogBoxManager.Instance != null
                && !T17DialogBoxManager.HasAnyOpenDialogs() && !T17DialogBoxManager.HasGlobalDialogs()) break;
            yield return null;
        }
        Check(profileReady && root != null && root.CurrentGamepadUser != null && !LoadingScreenFlow.IsLoading, "native profile/front end ready with isolated saves");
        Phase("native couch arcade");
        FrontendCoopTabOptions arcade = root.GetComponentInChildren<FrontendCoopTabOptions>(true);
        Check(arcade != null, "native arcade entry exists"); arcade.OnCouchPlayClicked();
        deadline = Time.realtimeSinceStartup + 65f;
        while (Time.realtimeSinceStartup < deadline && (LobbyFlowController.Instance == null || LoadingScreenFlow.IsLoading
            || LobbyFlowController.Instance.m_themeSelectMenu.CurrentGamepadUser == null)) yield return null;
        Check(LobbyFlowController.Instance != null && !LoadingScreenFlow.IsLoading, "native couch lobby loaded");
        ThemeSelectRootMenu themes = LobbyFlowController.Instance.m_themeSelectMenu;
        bool heatTest = Array.IndexOf(Environment.GetCommandLineArgs(), "-hudHeatTest") >= 0;
        SceneDirectoryData.LevelTheme theme = heatTest ? SceneDirectoryData.LevelTheme.Beach : SceneDirectoryData.LevelTheme.Sushi;
        CarouselButton chosen = themes.GetButtonForTheme(theme);
        Check(chosen != null, "native ordinary " + theme + " theme available (Horde excluded)");
        themes.OnButtonSelected(chosen);
        deadline = Time.realtimeSinceStartup + 8f;
        while (themes.GetCurrentButton() != chosen && Time.realtimeSinceStartup < deadline) yield return null;
        yield return new WaitForSecondsRealtime(.7f);
        Check(themes.GetCurrentButton() == chosen, "native carousel focused ordinary kitchen theme before submission");
        UnityEngine.EventSystems.ExecuteEvents.Execute(chosen.Button.gameObject, new UnityEngine.EventSystems.BaseEventData(themes.CachedEventSystem), UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        Phase("native kitchen load"); deadline = Time.realtimeSinceStartup + 100f;
        RecipeFlowGUI source = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            source = UnityEngine.Object.FindObjectOfType<RecipeFlowGUI>();
            if (!LoadingScreenFlow.IsLoading && GameUtils.GetLevelConfig() is GameModes.Horde.HordeLevelConfig)
                throw new InvalidOperationException("Horde is excluded; ordinary native theme selection failed.");
            bool inLevel = Team17.Online.ClientUserSystem.m_Users.Count > 0
                && Team17.Online.ClientUserSystem.m_Users._items[0].GameState == GameState.InLevel;
            if (inLevel && source != null && !LoadingScreenFlow.IsLoading && UnityEngine.Object.FindObjectOfType<ServerKitchenFlowControllerBase>() != null) break;
            yield return null;
        }
        Check(source != null && !LoadingScreenFlow.IsLoading, "actual kitchen and native recipe flow loaded");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hudEditorTest") >= 0) yield return EditorScenario();
        else yield return Scenario(source);
    }
}
