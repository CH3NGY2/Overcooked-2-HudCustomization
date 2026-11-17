using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

// Reads both independently built production assemblies. No copied metadata parser or provider stub.
internal static class ModSettingsConsumerTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks;
    private static void Check(bool condition, string label)
    { checks++; if (!condition) throw new Exception(label); }
    private static object Field(object target, string name)
    { return target.GetType().GetField(name, Members).GetValue(target); }
    private static void Set(object target, string name, object value)
    { target.GetType().GetField(name, Members).SetValue(target, value); }

    private static void Run(string[] args)
    {
        typeof(Paths).GetProperty("BepInExConfigPath").GetSetMethod(true).Invoke(null, new object[] { Path.Combine(args[0], "BepInEx.cfg") });
        typeof(Paths).GetProperty("ConfigPath").GetSetMethod(true).Invoke(null, new object[] { args[0] });
        Assembly consumer = Assembly.LoadFrom(args[1]), provider = Assembly.LoadFrom(args[2]);
        foreach (AssemblyName reference in consumer.GetReferencedAssemblies())
            Check(reference.Name != provider.GetName().Name && reference.Name != "ConfigurationManager", "no optional provider dependency");
        Type settingsType = consumer.GetType("OC2HudCustomization.HudSettings", true);
        Type languageType = consumer.GetType("OC2HudCustomization.HudLanguage", true);
        Type rowType = provider.GetType("OC2ModSettings.SettingRow", true);
        Type metadataType = provider.GetType("OC2ModSettings.EntryMetadata", true);
        MethodInfo read = metadataType.GetMethod("Read", Members);
        MethodInfo translate = settingsType.GetMethod("SetDisplayLanguage", Members);
        MethodInfo sort = provider.GetType("OC2ModSettings.CategorySort", true).GetMethod("Sort", Members);
        Type listType = typeof(List<>).MakeGenericType(rowType);
        ConfigFile config = new ConfigFile(Path.Combine(args[0], "provider-consumer.cfg"), false);
        config.SaveOnConfigSet = false;
        object settings = Activator.CreateInstance(settingsType, Members, null, new object[] { config }, null);
        Check(config.Count == 9, "built HUD binds nine entries");
        string[] sections = { "General", "Recipes", "Recipes", "Recipes", "Contents", "Contents", "Opacity", "Opacity", "Opacity" };
        string[] keys = { "Enabled", "RecipeScale", "RecipesPerRow", "RecipeRowSpacing", "ContentsScale", "ContentsPerRow", "RecipeOpacity", "EmptySlotOpacity", "IngredientOpacity" };
        int[] categoryOrders = { 400, 300, 300, 300, 200, 200, 100, 100, 100 };
        int[] orders = { 100, 100, 90, 80, 100, 90, 100, 90, 80 };
        object original = read.Invoke(null, new object[] { config["General", "Enabled"], null });
        Check((string)Field(original, "Name") == "Enable HUD customization" && (string)Field(original, "Category") == "General", "provider sees English before game readiness");
        config["Recipes", "RecipeScale"].BoxedValue = 1.5f;
        config.Save(); string disk = File.ReadAllText(config.ConfigFilePath);
        int changes = 0; config.SettingChanged += delegate { changes++; };
        foreach (object language in Enum.GetValues(languageType))
        {
            translate.Invoke(settings, new object[] { language });
            IList rows = (IList)Activator.CreateInstance(listType);
            for (int i = 0; i < keys.Length; i++)
            {
                ConfigEntryBase entry = config[sections[i], keys[i]];
                object tag = entry.Description.Tags[0];
                object metadata = read.Invoke(null, new object[] { entry, null });
                Check(tag.GetType().Assembly == consumer && metadata.GetType().Assembly == provider, "independent SDK and provider type identities");
                Check((string)Field(metadata, "Name") == (string)Field(tag, "DispName")
                    && (string)Field(metadata, "Description") == (string)Field(tag, "Description")
                    && (string)Field(metadata, "Category") == (string)Field(tag, "Category"), "provider reads localized label/help/category: " + language + "/" + keys[i]);
                Check((int)Field(metadata, "CategoryOrder") == categoryOrders[i] && (int)Field(metadata, "Order") == orders[i], "provider reads stable sort priority");
                Check(!(bool)Field(metadata, "Hidden") && !(bool)Field(metadata, "ReadOnly"), "translated row stays visible and editable");
                object row = Activator.CreateInstance(rowType, true);
                Set(row, "Owner", "com.ch3ngyz.plugin.overcooked2.hudcustomization"); Set(row, "Entry", entry); Set(row, "Metadata", metadata);
                rows.Insert(0, row);
                if (language.ToString() == "Chinese" && i == 0)
                    Check((string)Field(metadata, "Name") == "启用 HUD 自定义" && (bool)rowType.GetMethod("Matches", Members).Invoke(row, new object[] { "本机玩家", false }), "actual provider Chinese name and description search");
            }
            sort.Invoke(null, new object[] { rows });
            for (int i = 0; i < keys.Length; i++)
                Check(((ConfigEntryBase)Field(rows[i], "Entry")).Definition.Key == keys[i], "same display order in " + language + ": " + keys[i]);
        }
        Check((string)Field(original, "Name") == "Enable HUD customization", "previous provider snapshots remain immutable");
        Check(changes == 0 && config.Count == 9 && Equals(config["Recipes", "RecipeScale"].BoxedValue, 1.5f)
            && File.ReadAllText(config.ConfigFilePath) == disk, "built consumer translations preserve real cfg and values");
        Console.WriteLine("PASS " + checks + " actual-provider/compiled-HUD assertions; 9 entries x 12 languages (desktop; no Unity UI).");
    }

    public static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
            {
                string name = new AssemblyName(request.Name).Name + ".dll";
                foreach (string directory in new string[] { Path.Combine(args[3], @"Overcooked2_Data\Managed"), Path.Combine(args[3], @"BepInEx\core") })
                { string path = Path.Combine(directory, name); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            Run(args); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
