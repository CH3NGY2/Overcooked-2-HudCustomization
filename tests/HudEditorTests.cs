using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using OC2HudCustomization;

internal static partial class BehaviorTests
{
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .0001f; }
    private static void EditorRules(string output)
    {
        HudGeometry original = new HudGeometry();
        HudGeometry moved = HudDragMath.Drag(original, HudHandle.Move, 192f, -108f, 200f, 80f, 1920f, 1080f);
        Check(Near(moved.X, .1f) && Near(moved.Y, .1f) && original.X == 0f, "drag is normalized screen XY and leaves original draft intact");
        HudGeometry right = HudDragMath.Drag(original, HudHandle.Right, 100f, 0f, 200f, 80f, 1000f, 1000f);
        Check(Near(right.Width, 1.5f) && Near(right.Height, 1f) && Near(right.X, .05f), "right edge changes width and pins left edge");
        HudGeometry top = HudDragMath.Drag(original, HudHandle.Top, 0f, -40f, 200f, 80f, 1000f, 1000f);
        Check(Near(top.Height, 1.5f) && Near(top.Width, 1f) && Near(top.Y, .02f), "top edge height and upward coordinate keep opposite edge pinned");
        HudGeometry corner = HudDragMath.Drag(original, HudHandle.Right | HudHandle.Bottom, 100f, 40f, 200f, 80f, 1000f, 1000f);
        Check(Near(corner.Scale, 1.5f) && corner.Width == 1f && corner.Height == 1f, "corner proportionally scales both axes");
        Check(Near(corner.X, .05f) && Near(corner.Y, -.02f), "corner pins opposite corner");
        HudGeometry bounded = HudDragMath.Drag(original, HudHandle.Right, -1000f, 0f, 200f, 80f, 1000f, 1000f);
        Check(bounded.Width == .25f && Near(bounded.X, -.075f), "resize clamp still pins opposite edge");
        HudGeometry bad = new HudGeometry { X = float.NaN, Y = float.PositiveInfinity, Scale = -8f, Width = 99f, Height = float.NaN, Opacity = 3f };
        bad.Validate(); Check(bad.X == 0f && bad.Y == 0f && bad.Scale == .2f && bad.Width == 4f && bad.Height == 1f && bad.Opacity == 1f, "nonfinite and out-of-range hidden values never reach Unity");
        ConfigFile config = new ConfigFile(Path.Combine(output, "hud-editor.cfg"), false); config.SaveOnConfigSet = true;
        HudEditorConfig editor = new HudEditorConfig(config);
        Check(config.Count == 56 && config.SaveOnConfigSet, "eight categories with seven hidden entries preserve autosave policy");
        foreach (System.Collections.Generic.KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in config)
        {
            ConfigEntryBase entry = pair.Value;
            object tag = entry.Description.Tags[0];
            Check((bool)tag.GetType().GetField("Browsable").GetValue(tag) == false, "every editor entry hidden in actual SDK: " + entry.Definition);
        }
        HudGeometry[] values = editor.Read(), copy = HudGeometry.CopyAll(values);
        copy[(int)HudTargetKind.Chopping].X = .12f; copy[(int)HudTargetKind.Chopping].Allowed = false;
        Check(values[(int)HudTargetKind.Chopping].X == 0f && values[(int)HudTargetKind.Chopping].Allowed, "draft edit and whitelist do not mutate persisted snapshot");
        int changes = 0; config.SettingChanged += delegate { changes++; };
        Check(changes == 0 && !File.Exists(config.ConfigFilePath), "draft previews produce no configuration writes");
        editor.Save(copy); Check(config.SaveOnConfigSet && File.Exists(config.ConfigFilePath) && changes == 2, "one commit writes only changed values and restores automatic-save flag");
        ConfigFile reload = new ConfigFile(config.ConfigFilePath, false); reload.SaveOnConfigSet = false;
        HudGeometry restored = new HudEditorConfig(reload).Read()[(int)HudTargetKind.Chopping];
        Check(Near(restored.X, .12f) && !restored.Allowed, "geometry and pick whitelist survive real ConfigFile reload");
        int before = changes; editor.Save(copy); Check(changes == before, "unchanged save does not fire entry changes");
        HudSettings settings = new HudSettings(config); HudOptions saved = settings.Snapshot();
        HudOptions layout = new HudOptions(saved.Enabled, 1.5f, 3, 17f, .8f, 4, .75f, .35f, .6f, .14f, -.06f, 1.5f, .7f);
        copy[0].X = .22f;
        editor.Save(copy, settings, layout);
        Check(settings.Snapshot().Same(layout) && config.SaveOnConfigSet && Near(editor.Read()[0].X, .22f), "geometry and original layout commit through same ConfigFile transaction");
        ConfigFile combined = new ConfigFile(config.ConfigFilePath, false); combined.SaveOnConfigSet = false;
        Check(new HudSettings(combined).Snapshot().Same(layout) && Near(new HudEditorConfig(combined).Read()[0].X, .22f), "combined saved geometry and layout survive actual reload");
        string file = config.ConfigFilePath; string stable = File.ReadAllText(file);
        File.Delete(file); Directory.CreateDirectory(file);
        HudOptions rejected = new HudOptions(true, .6f, 7, 90, 1.8f, 9, .1f, .2f, .3f, -.3f, .2f, .6f, 2f);
        HudGeometry[] rejectedGeometry = HudGeometry.CopyAll(copy); rejectedGeometry[0].X = -.6f;
        bool failed = false;
        try { editor.Save(rejectedGeometry, settings, rejected); } catch (IOException) { failed = true; } catch (UnauthorizedAccessException) { failed = true; }
        Check(failed && config.SaveOnConfigSet && settings.Snapshot().Same(layout) && Near(editor.Read()[0].X, .22f), "failed shared save restores original settings and hidden geometry in memory");
        Directory.Delete(file); File.WriteAllText(file, stable);
        foreach (FieldInfo field in typeof(EditorText).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            SettingsMessage message = field.GetValue(null) as SettingsMessage;
            if (message == null) continue;
            foreach (HudLanguage language in Enum.GetValues(typeof(HudLanguage)))
                Check(!string.IsNullOrEmpty(message.Get(language)), "editor text complete: " + field.Name + "/" + language);
        }
        Check(EditorText.Kinds.Length == (int)HudTargetKind.Count, "every category has a localized whitelist label");
        foreach (SettingsMessage message in EditorText.Kinds)
            foreach (HudLanguage language in Enum.GetValues(typeof(HudLanguage))) Check(!string.IsNullOrEmpty(message.Get(language)), "category label translation");
    }
}
