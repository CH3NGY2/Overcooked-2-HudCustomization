using System;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using OC2ModSettings.SDK;
using UnityEngine;

namespace OC2HudCustomization
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency(ModSettingsClient.ServiceGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static HudRuntime Runtime;
        internal static HudEditor Editor;
        private HudEditorConfig _editorConfig;
        private HudSettings _settings;
        private HudLog _log;
        private int _settingsDirty = 1;
        private bool _started;
        private readonly ModSettingsClient _modSettings = new ModSettingsClient();
        private SettingsLanguageSync _language;
        private float _nextLanguageCheck;

        private void Awake()
        {
            _log = new HudLog(Logger);
            try
            {
                if (ConfigMigration.ImportLegacy(Config)) _log.Info("Imported previous HUD configuration into " + PluginInfo.Guid + ".cfg; original file preserved.");
            }
            catch (Exception exception) { _log.ErrorOnce("HUD configuration migration", exception); }
            _settings = new HudSettings(Config);
            _editorConfig = new HudEditorConfig(Config);
            _language = new SettingsLanguageSync(GameLanguage.Read, _settings.SetDisplayLanguage,
                RefreshSettingsDisplay, _log.ErrorOnce);
            Config.SettingChanged += SettingChanged;
        }

        private void Start() { _started = true; StartRuntime(); }
        private void OnEnable()
        {
            if (_language != null) _language.Invalidate();
            _nextLanguageCheck = 0f;
            if (_started) StartRuntime();
        }

        private bool RefreshSettingsDisplay()
        {
            if (!_modSettings.Connect()) return false;
            _modSettings.Refresh();
            return true;
        }

        private void StartRuntime()
        {
            if (Runtime != null) return;
            Runtime = new HudRuntime(_settings.Snapshot(), _log);
            Runtime.Start();
            try
            {
                Editor = new HudEditor(this, _settings, _editorConfig, _modSettings, _log);
                Editor.Start();
            }
            catch (Exception e)
            {
                _log.ErrorOnce("HUD editor initialization", e);
                if (Editor != null) Editor.Dispose();
                Editor = null;
            }
        }

        private void SettingChanged(object sender, SettingChangedEventArgs args)
        {
            // ModSettings/config editors may raise this outside the Unity thread.
            Interlocked.Exchange(ref _settingsDirty, 1);
        }

        private void Update()
        {
            if (Editor != null) Editor.Update();
            if (_language != null && Time.realtimeSinceStartup >= _nextLanguageCheck)
            {
                _nextLanguageCheck = Time.realtimeSinceStartup + 1f;
                _language.Poll();
            }
            if (Runtime != null && Interlocked.Exchange(ref _settingsDirty, 0) != 0)
                Runtime.SetOptions(Editor != null && Editor.Active ? Editor.DraftOptions : _settings.Snapshot());
        }

        private void LateUpdate() { if (Runtime != null) Runtime.Tick(); if (Editor != null) Editor.Tick(); }
        private void OnGUI() { if (Editor != null) Editor.OnGUI(); }

        private void OnDisable()
        {
            if (Editor != null) { Editor.Dispose(); Editor = null; }
            if (Runtime == null) return;
            HudRuntime runtime = Runtime;
            Runtime = null;
            runtime.Dispose();
        }

        private void OnDestroy()
        {
            OnDisable();
            if (_settings != null) Config.SettingChanged -= SettingChanged;
        }
    }
}
