using System;
using System.IO;
using BepInEx.Configuration;
using OC2HudCustomization;

internal static partial class BehaviorTests
{
    private static void ConfigMigrationChecks(string root)
    {
        string directory = Path.Combine(root, "guid-migration");
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, PluginInfo.LegacyGuid + ".cfg");
        string target = Path.Combine(directory, PluginInfo.Guid + ".cfg");
        Check(PluginInfo.Guid == "com.ch3ngyz.plugin.overcooked2.hudcustomization", "confirmed production GUID");
        ConfigFile config = new ConfigFile(target, false);
        Check(!ConfigMigration.ImportLegacy(config) && !File.Exists(target), "fresh installation has no synthetic migration file");
        const string original = "# user comment\n[General]\nEnabled = false\n[Recipes]\nRecipeScale = 1.25\nRecipesPerRow = 3\nRecipeRowSpacing = 17\n[Contents]\nContentsScale = 0.75\nContentsPerRow = 4\n[Opacity]\nRecipeOpacity = 60\nEmptySlotOpacity = 30\nIngredientOpacity = 80\n[Future]\nPreserve = user value\n";
        File.WriteAllText(source, original);
        Check(ConfigMigration.ImportLegacy(config), "legacy config is imported before any Bind");
        Check(File.ReadAllText(target) == original && File.ReadAllText(source) == original,
            "migration copies comments and unknown keys without rewriting the original");
        Check(Directory.GetFiles(directory, "*.migration").Length == 0, "successful migration leaves no temporary file");
        config.SaveOnConfigSet = false;
        HudSettings settings = new HudSettings(config);
        Check(config.Count == 14 && settings.RecipeMaxRows.Value == 0, "old config gets unlimited recipe rows by default");
        Check(!settings.Enabled.Value && settings.RecipeScale.Value == 1.25f && settings.RecipesPerRow.Value == 3
            && settings.RecipeRowSpacing.Value == 17f, "legacy general and recipe values load through real ConfigFile");
        Check(settings.ContentsScale.Value == .75f && settings.ContentsPerRow.Value == 4, "legacy contents values are retained");
        Check(settings.RecipeOpacity.Value == 60f && settings.EmptySlotOpacity.Value == 30f
            && settings.IngredientOpacity.Value == 80f, "all three legacy opacity values are retained");
        settings.RecipesPerRow.Value = 2;
        config.Save();
        string saved = File.ReadAllText(target);
        Check(saved.Contains("Preserve = user value"), "native save retains unrecognized future config entries");
        File.WriteAllText(source, "[Recipes]\nRecipesPerRow = 19\n");
        Check(!ConfigMigration.ImportLegacy(config) && File.ReadAllText(target) == saved,
            "existing new config wins on repeated startup even if legacy config changes");
        ConfigFile reloaded = new ConfigFile(target, false); reloaded.SaveOnConfigSet = false;
        Check(new HudSettings(reloaded).RecipesPerRow.Value == 2, "later startup reads the new identity config");
        string withoutLegacy = Path.Combine(directory, "new-only"); Directory.CreateDirectory(withoutLegacy);
        string independent = Path.Combine(withoutLegacy, PluginInfo.Guid + ".cfg");
        File.WriteAllText(independent, "[Recipes]\nRecipesPerRow = 6\n");
        Check(!ConfigMigration.ImportLegacy(new ConfigFile(independent, false))
            && File.ReadAllText(independent).Contains("RecipesPerRow = 6"), "new config works without a legacy file");
        string blocked = Path.Combine(directory, "blocked"); Directory.CreateDirectory(blocked);
        File.WriteAllText(Path.Combine(blocked, PluginInfo.LegacyGuid + ".cfg"), original);
        string blockedTarget = Path.Combine(blocked, PluginInfo.Guid + ".cfg"); Directory.CreateDirectory(blockedTarget);
        bool failed = false;
        try { ConfigMigration.ImportLegacy(new ConfigFile(blockedTarget, false)); }
        catch (IOException) { failed = true; }
        Check(failed && Directory.Exists(blockedTarget), "migration reports an obstructed target without replacing it");
        Check(Directory.GetFiles(blocked, "*.migration").Length == 0
            && File.ReadAllText(Path.Combine(blocked, PluginInfo.LegacyGuid + ".cfg")) == original,
            "failed migration cleans only its own temporary file and preserves legacy bytes");
    }
}
