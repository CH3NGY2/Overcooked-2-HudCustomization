using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using OC2HudCustomization;
using OC2ModSettings.SDK;

internal static partial class BehaviorTests
{
    private static void Localization(string root)
    {
        FieldInfo[] messages = typeof(SettingsText).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        Array languages = Enum.GetValues(typeof(HudLanguage));
        Check(languages.Length == 12 && messages.Length == 22, "twelve game languages and 22 display messages");
        foreach (FieldInfo message in messages)
        {
            SettingsMessage text = (SettingsMessage)message.GetValue(null);
            foreach (HudLanguage language in languages)
                Check(!string.IsNullOrEmpty(text.Get(language)), message.Name + ": nonempty " + language);
            Check(text.Get((HudLanguage)(-1)) == text.Get(HudLanguage.English)
                && text.Get((HudLanguage)999) == text.Get(HudLanguage.English), message.Name + ": unknown uses English");
        }
        foreach (SupportedLanguages language in Enum.GetValues(typeof(SupportedLanguages)))
            Check(GameLanguage.FromGame(language).ToString() == language.ToString(), "actual game language mapping: " + language);
        Check(GameLanguage.FromGame((SupportedLanguages)(-1)) == HudLanguage.English
            && GameLanguage.FromGame((SupportedLanguages)999) == HudLanguage.English, "unknown game language uses English");

        ConfigFile config = new ConfigFile(Path.Combine(root, "localized.cfg"), false);
        config.SaveOnConfigSet = false;
        HudSettings settings = new HudSettings(config);
        Check(settings.DisplayLanguage == HudLanguage.English && !settings.SetDisplayLanguage(HudLanguage.English), "English startup without a redundant change");
        ConfigDefinition[] definitions = new ConfigDefinition[config.Count]; config.Keys.CopyTo(definitions, 0);
        ConfigEntryBase[] entries = new ConfigEntryBase[definitions.Length];
        ModSettingsAttributes[] tags = new ModSettingsAttributes[definitions.Length];
        int[] priorities = new int[definitions.Length], orders = new int[definitions.Length];
        string[] descriptions = new string[definitions.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = config[definitions[i]]; tags[i] = (ModSettingsAttributes)entries[i].Description.Tags[0];
            priorities[i] = tags[i].CategoryOrder.Value; orders[i] = tags[i].Order.Value;
            descriptions[i] = entries[i].Description.Description;
            Check(!string.IsNullOrEmpty(tags[i].DispName) && tags[i].Description == descriptions[i], "English metadata bound: " + definitions[i]);
        }
        settings.RecipeScale.Value = 1.5f; settings.RecipesPerRow.Value = 3; settings.EmptySlotOpacity.Value = 25f;
        object[] values = new object[entries.Length];
        for (int i = 0; i < entries.Length; i++) values[i] = entries[i].BoxedValue;
        config.Save(); string disk = File.ReadAllText(config.ConfigFilePath);
        config.SaveOnConfigSet = true;
        int changes = 0; config.SettingChanged += delegate { changes++; };
        foreach (HudLanguage language in languages)
        {
            settings.SetDisplayLanguage(language);
            Check(!settings.SetDisplayLanguage(language), "same language is a no-op: " + language);
            for (int i = 0; i < entries.Length; i++)
            {
                Check(ReferenceEquals(config[definitions[i]], entries[i]) && ReferenceEquals(entries[i].Description.Tags[0], tags[i])
                    && Equals(entries[i].BoxedValue, values[i]), "stable entry/tag/key/value: " + language + "/" + definitions[i]);
                Check(tags[i].CategoryOrder == priorities[i] && tags[i].Order == orders[i]
                    && entries[i].Description.Description == descriptions[i], "stable priorities and cfg fallback: " + language + "/" + definitions[i]);
                string key = definitions[i].Key;
                SettingsMessage name = (SettingsMessage)typeof(SettingsText).GetField(key, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                SettingsMessage help = (SettingsMessage)typeof(SettingsText).GetField(key + "Description", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                SettingsMessage category = (SettingsMessage)typeof(SettingsText).GetField(definitions[i].Section, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Check(tags[i].DispName == name.Get(language) && tags[i].Description == help.Get(language)
                    && tags[i].Category == category.Get(language), "all visible metadata updated: " + language + "/" + definitions[i]);
            }
        }
        settings.SetDisplayLanguage((HudLanguage)999);
        Check(settings.DisplayLanguage == HudLanguage.English && tags[0].DispName != "", "invalid display language falls back");
        Check(changes == 0 && config.SaveOnConfigSet && File.ReadAllText(config.ConfigFilePath) == disk,
            "language-only refresh never changes values, emits SettingChanged or writes cfg");
    }

    private static void LanguageRefresh()
    {
        HudLanguage? language = null;
        HudLanguage applied = HudLanguage.English;
        bool ready = false, failRead = false, failRefresh = false;
        int notifications = 0, reports = 0, appliedChanges = 0;
        SettingsLanguageSync sync = new SettingsLanguageSync(
            delegate { if (failRead) throw new InvalidOperationException("test language lookup"); return language; },
            delegate(HudLanguage next) { if (next == applied) return false; applied = next; appliedChanges++; return true; },
            delegate { notifications++; if (failRefresh) throw new InvalidOperationException("test refresh"); return ready; },
            delegate { reports++; });
        sync.Poll(); sync.Poll();
        Check(applied == HudLanguage.English && appliedChanges == 0 && notifications == 2, "unready language keeps English and absent service stays pending");
        language = HudLanguage.Chinese; sync.Poll();
        Check(applied == HudLanguage.Chinese && appliedChanges == 1 && notifications == 3, "language updates even when ModSettings is absent");
        language = HudLanguage.Japanese; ready = true; sync.Poll();
        Check(applied == HudLanguage.Japanese && appliedChanges == 2 && notifications == 4, "late service receives latest complete batch");
        for (int i = 0; i < 20; i++) sync.Poll();
        Check(notifications == 4 && appliedChanges == 2, "steady language does not rebuild settings page");
        language = HudLanguage.Korean; sync.Poll();
        Check(applied == HudLanguage.Korean && notifications == 5, "runtime language change notifies once");
        language = null; sync.Poll();
        Check(applied == HudLanguage.Korean && notifications == 5, "temporary unreadiness retains last valid language");
        sync.Invalidate(); sync.Poll(); sync.Poll();
        Check(notifications == 6, "reenable invalidation refreshes once");
        language = HudLanguage.ChineseTraditional; failRefresh = true; sync.Poll();
        Check(reports == 1 && applied == HudLanguage.ChineseTraditional && notifications == 7, "refresh exception keeps translations and pending state");
        failRefresh = false; sync.Poll(); sync.Poll();
        Check(notifications == 8, "failed optional refresh retries then stops");
        failRead = true; sync.Poll();
        Check(reports == 2 && applied == HudLanguage.ChineseTraditional && notifications == 8, "lookup failure does not escape or reset language");
        failRead = false; language = HudLanguage.English; sync.Poll();
        Check(applied == HudLanguage.English && notifications == 9, "language lookup recovers");
    }
}
