using System.Collections.Generic;
using BepInEx.Configuration;
using OC2ModSettings.SDK;

namespace OC2HudCustomization
{
    internal sealed partial class HudSettings
    {
        private readonly List<LocalizedSetting> _localized = new List<LocalizedSetting>();
        internal HudLanguage DisplayLanguage { get; private set; }

        private ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T initial,
            SettingsMessage name, SettingsMessage description, SettingsMessage category,
            int categoryOrder, int order, AcceptableValueBase range)
        {
            LocalizedSetting display = new LocalizedSetting(name, description, category, categoryOrder, order);
            display.Apply(HudLanguage.English);
            ConfigEntry<T> entry = config.Bind(section, key, initial,
                new ConfigDescription(description.Get(HudLanguage.English), range, display.Tag));
            _localized.Add(display);
            return entry;
        }

        internal bool SetDisplayLanguage(HudLanguage language)
        {
            if ((int)language < 0 || (int)language >= 12) language = HudLanguage.English;
            if (DisplayLanguage == language) return false;
            // Mutate the existing tag objects as one batch; ConfigEntry, cfg keys and values stay put.
            for (int i = 0; i < _localized.Count; i++) _localized[i].Apply(language);
            DisplayLanguage = language;
            return true;
        }

        private sealed class LocalizedSetting
        {
            internal readonly ModSettingsAttributes Tag;
            private readonly SettingsMessage _name, _description, _category;
            internal LocalizedSetting(SettingsMessage name, SettingsMessage description, SettingsMessage category, int categoryOrder, int order)
            {
                _name = name; _description = description; _category = category;
                Tag = new ModSettingsAttributes { CategoryOrder = categoryOrder, Order = order };
            }
            internal void Apply(HudLanguage language)
            {
                Tag.DispName = _name.Get(language);
                Tag.Description = _description.Get(language);
                Tag.Category = _category.Get(language);
            }
        }
    }
}
