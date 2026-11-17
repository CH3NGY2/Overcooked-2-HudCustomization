using System;
using BepInEx.Configuration;
using OC2ModSettings.SDK;

namespace OC2HudCustomization
{
    internal sealed class HudEditorConfig
    {
        private readonly ConfigFile _config;
        private readonly Entries[] _entries = new Entries[(int)HudTargetKind.Count];
        internal HudEditorConfig(ConfigFile config)
        {
            _config = config;
            bool automatic = config.SaveOnConfigSet;
            try
            {
                config.SaveOnConfigSet = false;
                for (int i = 0; i < _entries.Length; i++) _entries[i] = new Entries(config, (HudTargetKind)i);
            }
            finally { config.SaveOnConfigSet = automatic; }
        }
        internal HudGeometry[] Read()
        {
            HudGeometry[] result = new HudGeometry[_entries.Length];
            for (int i = 0; i < result.Length; i++) result[i] = _entries[i].Read();
            return result;
        }
        internal void Save(HudGeometry[] values) { Save(values, null, null); }
        internal void Save(HudGeometry[] values, HudSettings settings, HudOptions options)
        {
            if (values.Length != _entries.Length) throw new ArgumentException("Incomplete HUD configuration");
            HudGeometry[] previous = Read(); bool automatic = _config.SaveOnConfigSet;
            HudOptions previousOptions = settings == null ? null : settings.Snapshot();
            bool changed = options != null && !options.Same(previousOptions);
            for (int i = 0; i < values.Length; i++)
            {
                HudGeometry value = values[i].Copy(); value.Validate();
                if (!value.Same(previous[i])) changed = true;
            }
            if (!changed) return;
            try
            {
                _config.SaveOnConfigSet = false;
                for (int i = 0; i < values.Length; i++) _entries[i].Write(values[i]);
                if (settings != null && options != null) settings.Apply(options);
                _config.Save();
            }
            catch
            {
                for (int i = 0; i < previous.Length; i++) _entries[i].Write(previous[i]);
                if (settings != null && previousOptions != null) settings.Apply(previousOptions);
                throw;
            }
            finally { _config.SaveOnConfigSet = automatic; }
        }
        private sealed class Entries
        {
            private readonly ConfigEntry<float> _x, _y, _scale, _width, _height, _opacity;
            private readonly ConfigEntry<bool> _allowed;
            internal Entries(ConfigFile config, HudTargetKind kind)
            {
                string section = "HudEditor." + kind;
                _x = Bind(config, section, "X", 0f, -1f, 1f);
                _y = Bind(config, section, "Y", 0f, -1f, 1f);
                _scale = Bind(config, section, "Scale", 1f, .2f, 3f);
                _width = Bind(config, section, "Width", 1f, .25f, 4f);
                _height = Bind(config, section, "Height", 1f, .25f, 4f);
                _opacity = Bind(config, section, "Opacity", 1f, 0f, 1f);
                _allowed = config.Bind(section, "Editable", true,
                    new ConfigDescription("Allow picking this category in the HUD editor.", null, new ModSettingsAttributes { Browsable = false }));
            }
            private static ConfigEntry<float> Bind(ConfigFile config, string section, string key, float value, float min, float max)
            { return config.Bind(section, key, value, new ConfigDescription("HUD editor value; X/Y are screen fractions (Y up).", new AcceptableValueRange<float>(min, max), new ModSettingsAttributes { Browsable = false })); }
            internal HudGeometry Read()
            {
                HudGeometry value = new HudGeometry { X = _x.Value, Y = _y.Value, Scale = _scale.Value,
                    Width = _width.Value, Height = _height.Value, Opacity = _opacity.Value, Allowed = _allowed.Value };
                value.Validate(); return value;
            }
            internal void Write(HudGeometry value)
            {
                value = value.Copy(); value.Validate();
                _x.Value = value.X; _y.Value = value.Y; _scale.Value = value.Scale;
                _width.Value = value.Width; _height.Value = value.Height; _opacity.Value = value.Opacity; _allowed.Value = value.Allowed;
            }
        }
    }
}
