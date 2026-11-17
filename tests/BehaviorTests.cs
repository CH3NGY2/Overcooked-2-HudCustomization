using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using OC2HudCustomization;

internal static partial class BehaviorTests
{
    private static int _checks;
    private static void Check(bool condition, string label)
    {
        _checks++;
        if (!condition) throw new Exception(label);
    }

    public static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
            {
                string name = new AssemblyName(request.Name).Name + ".dll";
                foreach (string directory in new string[] { Path.Combine(args[1], @"Overcooked2_Data\Managed"), Path.Combine(args[1], @"BepInEx\core") })
                {
                    string dependency = Path.Combine(directory, name);
                    if (File.Exists(dependency)) return Assembly.LoadFrom(dependency);
                }
                return null;
            };
            // ConfigFile normally receives these paths from the chainloader. Point its real
            // desktop config implementation at test-owned paths before its static initializer.
            typeof(Paths).GetProperty("BepInExConfigPath").GetSetMethod(true).Invoke(null, new object[] { Path.Combine(args[0], "BepInEx.cfg") });
            typeof(Paths).GetProperty("ConfigPath").GetSetMethod(true).Invoke(null, new object[] { args[0] });
            Layout(); Ownership(); Motion(); Config(args[0]);
            Localization(args[0]); LanguageRefresh(); ConfigMigrationChecks(args[0]);
            Console.WriteLine("PASS " + _checks + " production-rule/config assertions (desktop; no Unity UI).");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void Layout()
    {
        Check(!HudLayoutMath.NeedsWrap(5, 0, 600f, 300f, 8f, 400f), "0 keeps native single row");
        Check(!HudLayoutMath.NeedsWrap(0, 2, 0f, 900f, 8f, 400f), "oversize first card cannot create an empty row");
        Check(HudLayoutMath.NeedsWrap(2, 2, 180f, 50f, 8f, 400f), "column upper limit");
        Check(HudLayoutMath.NeedsWrap(1, 5, 250f, 160f, 8f, 400f), "safe-area early wrap");
        Check(!HudLayoutMath.NeedsWrap(1, 5, 250f, 142f, 8f, 400f), "exact safe-area fit");
        Check(HudLayoutMath.Columns(0, 4) == 0 && HudLayoutMath.Rows(0, 4) == 0, "empty contents");
        Check(HudLayoutMath.Columns(1, 4) == 1 && HudLayoutMath.Rows(1, 1) == 1, "single icon");
        Check(HudLayoutMath.Columns(7, 3) == 3 && HudLayoutMath.Rows(7, 3) == 3, "partial final row");
        Check(HudLayoutMath.Columns(7, 99) == 7, "avoid unused fixed columns");
        Check(HudLayoutMath.Extent(4, 50f, 2f, 0f) == 206f, "more columns retain individual icon size");
        Check(HudLayoutMath.Extent(4, 100f, 4f, 0f) == 412f, "scale independent of column count");
        Check(HudLayoutMath.Extent(0, 50f, 2f, 4f) == 0f, "zero count no negative gap");
        HudRowLayout layout = new HudRowLayout(5f, 0f, 2, 5f, 8f, 500f);
        float left, top;
        layout.Place(100f, 50f, out left, out top); Check(left == 5f && top == 0f, "native first-card origin");
        layout.Place(200f, 150f, out left, out top); Check(left == 110f && top == 0f, "variable card widths plus native gap");
        layout.Place(80f, 60f, out left, out top); Check(left == 5f && top == -158f, "new row below maximum full-card height");
        layout.Place(450f, 40f, out left, out top); Check(left == 5f && top == -226f, "safe-width wrap before column upper limit");
    }

    private static void Ownership()
    {
        float value = 0.8f; int writes = 0, conflicts = 0;
        OwnedValue<float> property = new OwnedValue<float>(delegate { return value; },
            delegate(float next) { value = next; writes++; }, delegate { conflicts++; });
        Check(property.Apply(0.4f) && value == 0.4f, "first apply");
        property.Apply(0.4f); Check(value == 0.4f && writes == 1, "repeat not cumulative/no redundant write");
        property.Apply(0.2f); property.Restore(); Check(value == 0.8f, "restore first original after changing settings");
        value = 0.6f; property.Apply(0.3f); property.Restore(); Check(value == 0.6f, "new ownership cycle captures new native value");
        property.Apply(0.3f); value = 0.9f;
        Check(!property.CanApply() && value == 0.9f && conflicts == 1, "atomic geometry guard detects foreign owner before mutation");
        Check(!property.Apply(0.1f) && value == 0.9f && conflicts == 1, "foreign owner preserved");
        property.Restore(); Check(value == 0.9f && conflicts == 1, "conflicting restore does not overwrite or spam");
        Check(!property.Apply(0.1f), "foreign property not reacquired each frame");
        float rounded = 1f;
        OwnedValue<float> approximate = new OwnedValue<float>(delegate { return rounded; },
            delegate(float next) { rounded = next; }, delegate { throw new Exception("float noise falsely transferred ownership"); },
            delegate(float a, float b) { return Math.Abs(a - b) < 0.001f; });
        approximate.Apply(2f); rounded = 2.00001f; approximate.Restore();
        Check(rounded == 1f, "float-noise tolerance");
    }

    private static void Config(string root)
    {
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "hud.cfg");
        ConfigFile config = new ConfigFile(path, false); config.SaveOnConfigSet = false;
        HudSettings settings = new HudSettings(config);
        Check(config.Count == 9, "nine discoverable ConfigEntry values");
        HudOptions defaults = settings.Snapshot();
        Check(defaults.Enabled && defaults.RecipeScale == 1f && defaults.ContentsScale == 1f
            && defaults.RecipesPerRow == 0 && defaults.ContentsPerRow == 0, "native geometry defaults");
        Check(defaults.RecipeOpacity == 1f && defaults.EmptySlotOpacity == 1f && defaults.IngredientOpacity == 1f, "native alpha defaults");
        int changed = 0; config.SettingChanged += delegate { changed++; };
        config["Recipes", "RecipeScale"].BoxedValue = 1.5f;
        Check(changed == 1 && settings.Snapshot().RecipeScale == 1.5f, "external ModSettings-style boxed write raises event");
        settings.RecipesPerRow.Value = 3;
        settings.ContentsPerRow.Value = 4;
        settings.RecipeOpacity.Value = 50f;
        settings.EmptySlotOpacity.Value = 0f;
        Check(settings.Snapshot().RecipeOpacity == 0.5f && settings.Snapshot().EmptySlotOpacity == 0f
            && settings.Snapshot().IngredientOpacity == 1f, "independent alpha multipliers");
        settings.ContentsScale.Value = 99f;
        Check(settings.ContentsScale.Value == 2f && settings.ContentsPerRow.Value == 4, "bounded size retains independent columns");
        settings.ContentsScale.Value = 1f;
        settings.IngredientOpacity.Value = -10f; Check(settings.IngredientOpacity.Value == 0f, "bounded percent");
        config.Save();
        ConfigFile reloaded = new ConfigFile(path, true); reloaded.SaveOnConfigSet = false;
        HudSettings persisted = new HudSettings(reloaded);
        Check(persisted.RecipeScale.Value == 1.5f && persisted.RecipesPerRow.Value == 3
            && persisted.EmptySlotOpacity.Value == 0f, "native cfg persistence");
        persisted.Enabled.Value = false; Check(!persisted.Snapshot().Enabled, "disable option");
        persisted.RecipeScale.Value = float.NaN;
        persisted.EmptySlotOpacity.Value = float.NaN;
        HudOptions safe = persisted.Snapshot();
        Check(!float.IsNaN(safe.RecipeScale) && !float.IsInfinity(safe.RecipeScale) && safe.RecipeScale >= 0.5f && safe.RecipeScale <= 2f
            && !float.IsNaN(safe.EmptySlotOpacity) && safe.EmptySlotOpacity >= 0f && safe.EmptySlotOpacity <= 1f,
            "non-finite cfg cannot poison Unity geometry or alpha");
    }
}
