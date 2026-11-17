using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using OC2ModSettings.SDK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OC2HudCustomization
{
    internal sealed partial class HudEditor : IDisposable
    {
        internal readonly HudTargetRegistry Targets;
        private readonly HudEditorConfig _config;
        private readonly HudSettings _settings;
        private readonly Plugin _plugin;
        private readonly HudLog _log;
        private readonly ModSettingsClient _client;
        private readonly Harmony _harmony = new Harmony(PluginInfo.Guid + ".editor.shortcut");
        private IDisposable _action;
        private object _service;
        private HudLanguage _actionLanguage;
        private bool _shortcutReady;
        private bool _refreshPending;
        private HudEditorLease _lease;
        private HudGeometry[] _saved;
        internal HudOptions DraftOptions { get; private set; }
        private readonly HudNativeAssets _assets = new HudNativeAssets();
        private HudEditorPanel _panel;
        private float _nextServiceCheck;
        private static int _exitFrame = -1;
        internal bool Active { get { return _lease != null; } }
        internal bool AllowsRecipePreview { get { return Active && _lease.AllowsRecipePreview; } }
        internal static bool BlocksShortcut { get { return (Plugin.Editor != null && Plugin.Editor.Active) || Time.frameCount == _exitFrame; } }
        internal HudEditor(Plugin plugin, HudSettings settings, HudEditorConfig config, ModSettingsClient client, HudLog log)
        {
            _plugin = plugin; _settings = settings; _config = config; _client = client; _log = log;
            Targets = new HudTargetRegistry(config.Read(), log);
        }
        internal void Start()
        {
            Targets.Start(); SceneManager.sceneUnloaded += SceneUnloaded;
            _harmony.Patch(Required.Method(typeof(PlayerControls), "Awake"), null, new HarmonyMethod(typeof(HudEditor), "PlayerCreated"));
            _harmony.Patch(Required.Method(typeof(T17EventSystem), "Awake"), null, new HarmonyMethod(typeof(HudEditor), "DomainEnabled"));
            _harmony.Patch(Required.Method(typeof(PauseMenuManager), "UpdateWhileClosed"), new HarmonyMethod(typeof(HudEditor), "FilterPause"));
            _harmony.Patch(Required.Method(typeof(FrontendMenuBehaviour), "Show", typeof(GamepadUser), typeof(BaseMenuBehaviour), typeof(GameObject), typeof(bool)), null, new HarmonyMethod(typeof(HudEditor), "MenuShown"));
            foreach (FrontendRootMenu root in UnityEngine.Object.FindObjectsOfType<FrontendRootMenu>()) _assets.Capture(root);
            _log.Info("HUD editor input lifecycle hooks: PlayerControls.Awake / T17EventSystem.Awake Postfix.");
            try { PollService(); } catch (Exception e) { _log.ErrorOnce("HUD editor settings service", e); }
        }
        internal void Update()
        {
            try
            {
                Targets.Enabled = _settings.Enabled.Value && IsKitchen();
                if (Time.realtimeSinceStartup >= _nextServiceCheck)
                {
                    _nextServiceCheck = Time.realtimeSinceStartup + 1f;
                    if (!_assets.Ready && T17FrontendFlow.Instance != null)
                        try { _assets.Capture(T17FrontendFlow.Instance.m_Rootmenu); }
                        catch (Exception e) { _log.ErrorOnce("HUD editor native GameOptions assets", e); }
                    try { PollService(); } catch (Exception e) { _log.ErrorOnce("HUD editor settings service", e); }
                    if (!Active) Targets.Values = _config.Read();
                }
                if (!Active) return;
                if (!CanEditLevel()) { End(false); return; }
                _lease.Maintain();
                _panel.Tick();
                if (Input.GetKeyDown(KeyCode.BackQuote)) { _panel.FinishText(); End(true); }
            }
            catch (Exception e) { _log.ErrorOnce("HUD editor Update", e); End(false); }
        }
        internal void Tick()
        {
            try { Targets.Tick(); }
            catch (Exception e) { _log.ErrorOnce("HUD editor presentation", e); End(false); Targets.Enabled = false; }
        }
        private bool CanEditLevel()
        {
            if (!_settings.Enabled.Value || LoadingScreenFlow.IsLoading || Team17.Online.UserSystemUtils.AnyRemoteUsers()) return false;
            return IsKitchen();
        }
        private static bool IsKitchen()
        {
            if (LoadingScreenFlow.IsLoading) return false;
            bool inLevel = false;
            for (int i = 0; i < Team17.Online.ClientUserSystem.m_Users.Count; i++)
                if (Team17.Online.ClientUserSystem.m_Users._items[i].GameState == GameState.InLevel) { inLevel = true; break; }
            return inLevel && GameUtils.GetLevelConfig() is KitchenLevelConfigBase;
        }
        internal bool CanEnter() { return !Active && _shortcutReady && _assets.Ready && CanEditLevel(); }
        internal void Begin()
        {
            if (!CanEnter()) return;
            try
            {
                Targets.Discover(); Targets.ClearPreviews(); _saved = _config.Read(); Targets.Values = HudGeometry.CopyAll(_saved);
                DraftOptions = _settings.Snapshot();
                _lease = new HudEditorLease(_plugin, _log); _lease.Acquire();
                _selected = null; _editingKind = -1; _dragging = false; _error = false; _invalid.Clear();
                if (_panel == null) _panel = new HudEditorPanel(this, _assets);
                _panel.Show();
                _log.Info("HUD editor entered; native Main/UI/Camera pause owned; press BackQuote to save and exit.");
            }
            catch (Exception e) { _log.ErrorOnce("HUD editor enter", e); End(false); }
        }
        internal void End(bool save)
        {
            if (!Active) return;
            if (save)
            {
                if (_invalid.Count != 0) { _error = true; return; }
                try { _config.Save(Targets.Values, _settings, DraftOptions); }
                catch (Exception e) { _log.ErrorOnce("HUD editor save", e); _error = true; return; }
            }
            HudEditorLease lease = _lease; _lease = null;
            _exitFrame = Time.frameCount; _selected = null; _editingKind = -1; _dragging = false;
            if (!save && _saved != null) Targets.Values = _saved;
            try { if (_panel != null) _panel.Hide(); Targets.ClearPreviews(); }
            finally
            {
                // Restore display-only animation ownership before releasing the native pause.
                try { if (Plugin.Runtime != null && Plugin.Runtime.Recipes != null) Plugin.Runtime.Recipes.StopPreview(); }
                finally { lease.Dispose(); if (Plugin.Runtime != null) Plugin.Runtime.SetOptions(_settings.Snapshot()); }
            }
            _log.Info(save ? "HUD editor saved and exited." : "HUD editor cancelled; draft discarded.");
        }
        private void SceneUnloaded(Scene scene) { if (Active) End(false); }
        private void PollService()
        {
            if (!_client.Connect() || !_client.SupportsActions) return;
            BepInEx.PluginInfo service;
            if (!Chainloader.PluginInfos.TryGetValue(ModSettingsClient.ServiceGuid, out service)) return;
            if (!_shortcutReady || !ReferenceEquals(_service, service.Instance))
            {
                Type shortcut = service.Instance.GetType().Assembly.GetType("OC2ModSettings.NativeShortcut");
                if (shortcut == null) throw new MissingMemberException("ModSettings NativeShortcut");
                MethodInfo tick = Required.Method(shortcut, "Tick", typeof(bool));
                _harmony.Patch(tick, new HarmonyMethod(typeof(HudEditor), "FilterShortcut"));
                _log.Info("HUD editor shortcut coordination: " + tick.DeclaringType.FullName + ".Tick Prefix (pressed argument only).");
                _shortcutReady = true; _service = service.Instance;
                if (_action != null) { _action.Dispose(); _action = null; }
            }
            HudLanguage language = _settings.DisplayLanguage;
            if (_action != null && language == _actionLanguage)
            { if (_refreshPending) { _client.Refresh(); _refreshPending = false; } return; }
            IDisposable next = _client.Action(PluginInfo.Guid, "edit-hud", new Dictionary<string, object> {
                { "Version", 1 }, { "DisplayName", EditorText.Enter.Get(language) },
                { "Description", EditorText.EntryDescription.Get(language) },
                { "Category", SettingsText.General.Get(language) }, { "CategoryOrder", 400 }, { "Order", 90 },
                { "ClosePage", true }, { "Execute", (Action)Begin }, { "CanExecute", (Func<bool>)CanEnter }
            });
            IDisposable previous = _action; _action = next; _actionLanguage = language;
            _refreshPending = true;
            if (previous != null) previous.Dispose();
            _client.Refresh(); _refreshPending = false;
        }
        private static void FilterShortcut(ref bool pressed) { if (BlocksShortcut) pressed = false; }
        private static bool FilterPause()
        {
            HudEditor editor = Plugin.Editor;
            if (editor != null && editor.Active
                && (TimeManager.IsPaused(TimeManager.PauseLayer.System) || TimeManager.IsPaused(TimeManager.PauseLayer.Network)))
            { editor.End(false); return true; }
            return !BlocksShortcut;
        }
        private static void PlayerCreated(PlayerControls __instance)
        {
            HudEditor editor = Plugin.Editor;
            if (editor == null || !editor.Active) return;
            try { editor._lease.Track(__instance); } catch (Exception e) { editor._log.ErrorOnce("HUD editor PlayerControls.Awake", e); editor.End(false); }
        }
        private static void DomainEnabled(T17EventSystem __instance)
        {
            HudEditor editor = Plugin.Editor;
            if (editor == null || !editor.Active) return;
            try { editor._lease.Track(__instance); } catch (Exception e) { editor._log.ErrorOnce("HUD editor T17EventSystem.Awake", e); editor.End(false); }
        }
        private static void MenuShown(FrontendMenuBehaviour __instance)
        {
            HudEditor editor = Plugin.Editor; FrontendRootMenu root = __instance.GetComponentInParent<FrontendRootMenu>();
            if (editor == null || !root) return;
            try { editor._assets.Capture(root); }
            catch (Exception e) { editor._log.ErrorOnce("HUD editor native GameOptions assets", e); }
        }
        public void Dispose()
        {
            try { End(false); } catch (Exception e) { _log.ErrorOnce("HUD editor cancel on disposal", e); }
            SceneManager.sceneUnloaded -= SceneUnloaded;
            IDisposable action = _action; _action = null;
            try { if (action != null) action.Dispose(); } catch (Exception e) { _log.ErrorOnce("HUD editor action release", e); }
            try { _harmony.UnpatchSelf(); } catch (Exception e) { _log.ErrorOnce("HUD editor input unpatch", e); }
            Targets.Dispose();
            if (_panel != null) _panel.Dispose(); _panel = null;
            _assets.Dispose();
        }
    }
}
