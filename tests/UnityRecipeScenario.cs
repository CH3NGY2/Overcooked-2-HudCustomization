using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

public sealed partial class UnityRecipeTests
{
    private readonly Vector3[] corners = new Vector3[4];
    private Rect Bounds(RecipeWidgetUIController widget, Transform space)
    {
        Rect result = new Rect(); bool found = false;
        // Independently measure rendered native graphics, rather than reusing the production cache.
        foreach (UnityEngine.UI.Graphic graphic in widget.GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {
            if (!graphic.isActiveAndEnabled) continue;
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = space.InverseTransformPoint(corner);
                if (!found) { result = new Rect(point.x, point.y, 0, 0); found = true; }
                else result = Rect.MinMaxRect(Mathf.Min(result.xMin, point.x), Mathf.Min(result.yMin, point.y), Mathf.Max(result.xMax, point.x), Mathf.Max(result.yMax, point.y));
            }
        }
        if (!found) throw new InvalidOperationException("Native card has no rendered graphic geometry");
        return result;
    }
    private Rect HeadBounds(RecipeWidgetUIController widget, Transform space)
    {
        TopRecipeWidgetTile head = widget.GetComponentInChildren<TopRecipeWidgetTile>(true);
        Rect result = new Rect(); bool found = false;
        foreach (UnityEngine.UI.Graphic graphic in head.GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {
            if (!graphic.isActiveAndEnabled) continue;
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = space.InverseTransformPoint(corner);
                if (!found) { result = new Rect(point.x, point.y, 0, 0); found = true; }
                else result = Rect.MinMaxRect(Mathf.Min(result.xMin, point.x), Mathf.Min(result.yMin, point.y), Mathf.Max(result.xMax, point.x), Mathf.Max(result.yMax, point.y));
            }
        }
        if (!found) throw new InvalidOperationException("Native head is not generated yet");
        return result;
    }
    private static float BranchOpacity(RecipeWidgetUIController widget)
    {
        float result = 1f;
        foreach (RecipeWidgetTile tile in widget.GetComponentsInChildren<RecipeWidgetTile>(true))
        {
            if (tile is TopRecipeWidgetTile) continue;
            foreach (CanvasGroup group in tile.GetComponentsInChildren<CanvasGroup>(true)) result = Mathf.Min(result, group.alpha);
        }
        return result;
    }
    private static bool HasCookware(OrderDefinitionNode recipe)
    {
        if (recipe.m_orderGuiDescription == null || recipe.m_orderGuiDescription.Length < 2) return false;
        foreach (RecipeWidgetUIController.RecipeTileData tile in recipe.m_orderGuiDescription)
            if (tile.m_tileDefinition != null && tile.m_tileDefinition.m_modifierPictures != null && tile.m_tileDefinition.m_modifierPictures.Count > 0) return true;
        return false;
    }
    private IEnumerator Scenario(RecipeFlowGUI source)
    {
        Phase("native recipe generation fixture");
        BaseUnityPlugin hud = Chainloader.PluginInfos["com.ch3ngyz.plugin.overcooked2.hudcustomization"].Instance;
        Check(hud != null, "production HUD loaded by actual Chainloader");
        Check(hud.Info.Metadata.GUID == "com.ch3ngyz.plugin.overcooked2.hudcustomization"
            && !Chainloader.PluginInfos.ContainsKey("com.oc2.hudcustomization"), "new HUD identity loaded without a duplicate legacy plugin");
        Check(Path.GetFileName(hud.Config.ConfigFilePath) == hud.Info.Metadata.GUID + ".cfg", "native ConfigFile uses the confirmed new GUID");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hudRecipeLegacyMigration") >= 0)
        {
            BepInEx.Configuration.ConfigFile legacy = new BepInEx.Configuration.ConfigFile(
                Path.Combine(Paths.ConfigPath, "com.oc2.hudcustomization.cfg"), false);
            legacy.SaveOnConfigSet = false;
            MethodInfo bind = null;
            foreach (MethodInfo candidate in typeof(BepInEx.Configuration.ConfigFile).GetMethods())
                if (candidate.Name == "Bind" && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 3
                    && candidate.GetParameters()[0].ParameterType == typeof(BepInEx.Configuration.ConfigDefinition)) bind = candidate;
            foreach (KeyValuePair<BepInEx.Configuration.ConfigDefinition, BepInEx.Configuration.ConfigEntryBase> entry in hud.Config)
            {
                BepInEx.Configuration.ConfigEntryBase old = (BepInEx.Configuration.ConfigEntryBase)bind.MakeGenericMethod(entry.Value.SettingType)
                    .Invoke(legacy, new object[] { entry.Key, entry.Value.DefaultValue, entry.Value.Description });
                Check(object.Equals(old.BoxedValue, entry.Value.BoxedValue), "real startup migration retains " + entry.Key.Section + "." + entry.Key.Key);
                File.AppendAllText(Path.Combine(output, "guid-migration.tsv"), entry.Key + "\t" + entry.Value.GetSerializedValue() + Environment.NewLine);
            }
        }
        hud.Config.SaveOnConfigSet = false;
        File.WriteAllText(Path.Combine(output, "loaded.txt"), hud.Info.Metadata.GUID + " " + hud.Info.Metadata.Version + " " + Hash(hud.GetType().Assembly.Location) + "\nScreen=" + Screen.width + "x" + Screen.height);
        OrderDefinitionNode recipe = null;
        foreach (OrderDefinitionNode candidate in Resources.FindObjectsOfTypeAll<OrderDefinitionNode>())
            if (HasCookware(candidate) && (recipe == null || candidate.name.ToLowerInvariant().Contains("burger"))) recipe = candidate;
        Check(recipe != null, "loaded stock recipe with cooking modifier available");
        File.AppendAllText(Path.Combine(output, "loaded.txt"), "\nNativeRecipe=" + recipe.name);
        RectTransform nativeRect = source.transform as RectTransform;
        GameObject fixture = new GameObject("HudRecipeRegression", typeof(RectTransform)); fixture.SetActive(false);
        RectTransform rect = fixture.transform as RectTransform; rect.SetParent(nativeRect.parent, false);
        rect.anchorMin = nativeRect.anchorMin; rect.anchorMax = nativeRect.anchorMax; rect.pivot = nativeRect.pivot;
        rect.sizeDelta = nativeRect.sizeDelta; rect.anchoredPosition = nativeRect.anchoredPosition; rect.localScale = nativeRect.localScale; rect.localRotation = nativeRect.localRotation;
        RecipeFlowGUI flow = fixture.AddComponent<RecipeFlowGUI>();
        AccessTools.Field(typeof(RecipeFlowGUI), "m_recipeWidgetPrefab").SetValue(flow, AccessTools.Field(typeof(RecipeFlowGUI), "m_recipeWidgetPrefab").GetValue(source));
        fixture.SetActive(true);
        // Leave the real business flow running: deactivation would break its later AddElement
        // coroutines. Only hide that flow while the independent display fixture is captured.
        source.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
        List<RecipeFlowGUI.ElementToken> tokens = new List<RecipeFlowGUI.ElementToken>();
        for (int i = 0; i < 4; i++) tokens.Add(flow.AddElement(recipe, 180f, delegate(RecipeFlowGUI.ElementToken token) { }));
        List<RecipeWidgetUIController> widgets = new List<RecipeWidgetUIController>();
        foreach (RecipeFlowGUI.ElementToken token in tokens) widgets.Add(flow.GetData(token).m_widget);
        hud.Config["General", "Enabled"].BoxedValue = true;
        hud.Config["Recipes", "RecipesPerRow"].BoxedValue = 2;
        hud.Config["Recipes", "RecipeRowSpacing"].BoxedValue = 20.77099f;
        hud.Config["Recipes", "RecipeScale"].BoxedValue = .5f;
        yield return Settle(widgets, rect, "scale-05-initial");
        VerifyRows(widgets, rect, "scale-05-initial");
        RecipeFlowGUI.ElementToken incomingToken = flow.AddElement(recipe, 180f, delegate(RecipeFlowGUI.ElementToken token) { });
        RecipeWidgetUIController incoming = flow.GetData(incomingToken).m_widget;
        widgets.Add(incoming);
        yield return CaptureEntrance(widgets, rect, incoming, "entrance", 2);
        yield return Settle(widgets, rect, "scale-05");
        VerifyRows(widgets, rect, "scale-05");
        Check(PresentationPreserved(widgets), "custom layout hides only the upper paper and keeps one active progress bar per card");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "scale-05.png")); yield return new WaitForEndOfFrame(); yield return null;
        // Rebuild the same native widget objects, exercising the production cache invalidation hook.
        foreach (RecipeWidgetUIController widget in widgets) widget.RefreshSubElements();
        yield return Settle(widgets, rect, "rebuilt-scale-05");
        VerifyRows(widgets, rect, "rebuilt-scale-05");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "rebuilt-scale-05.png")); yield return new WaitForEndOfFrame(); yield return null;
        foreach (float scale in new float[] { 1f, 2f })
        {
            hud.Config["Recipes", "RecipeScale"].BoxedValue = scale;
            yield return Settle(widgets, rect, "scale-" + scale);
            VerifyRows(widgets, rect, "scale-" + scale);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "scale-" + scale + ".png")); yield return new WaitForEndOfFrame(); yield return null;
        }
        hud.Config["Recipes", "RecipeScale"].BoxedValue = .5f;
        hud.Config["Recipes", "RecipesPerRow"].BoxedValue = 1;
        yield return Settle(widgets, rect, "single-column");
        VerifyColumn(widgets, rect, "single-column");
        RecipeFlowGUI.ElementToken singleToken = flow.AddElement(recipe, 180f, delegate(RecipeFlowGUI.ElementToken token) { });
        RecipeWidgetUIController singleIncoming = flow.GetData(singleToken).m_widget;
        widgets.Add(singleIncoming);
        yield return CaptureEntrance(widgets, rect, singleIncoming, "single-entrance", 1);
        yield return Settle(widgets, rect, "single-column-added");
        VerifyColumn(widgets, rect, "single-column-added");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "single-column.png")); yield return new WaitForEndOfFrame(); yield return null;
        hud.Config["Recipes", "RecipesPerRow"].BoxedValue = 2;
        yield return Settle(widgets, rect, "column-change");
        VerifyRows(widgets, rect, "column-change");
        Check(PresentationPreserved(widgets), "changing columns preserves trimmed head, one progress bar and visible branches");
        hud.Config["General", "Enabled"].BoxedValue = false;
        yield return Settle(widgets, rect, "disabled-native");
        Rect first = Bounds(widgets[0], rect), second = Bounds(widgets[1], rect);
        Check(Mathf.Abs(first.yMax - second.yMax) < .1f, "disable returns cards to native single row");
        Check(PresentationRestored(widgets), "disabling restores the native upper paper and progress bar");
        Phase("completed UI geometry and restore checks");
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private bool PresentationPreserved(List<RecipeWidgetUIController> widgets)
    {
        for (int i = 0; i < widgets.Count; i++)
        {
            TopRecipeWidgetTile head = widgets[i].GetComponentInChildren<TopRecipeWidgetTile>(true);
            Transform paper = head == null ? null : FindNamed(head.transform, "RecipeWidgetTile_BackgroundTop");
            ProgressBarUI[] bars = widgets[i].GetComponentsInChildren<ProgressBarUI>(true);
            File.AppendAllText(Path.Combine(output, "presentation.tsv"), "custom\t" + i + "\t"
                + (head != null) + "\t" + (paper == null ? "missing" : paper.gameObject.activeSelf.ToString())
                + "\t" + bars.Length + "\t" + (bars.Length == 1 && bars[0].gameObject.activeInHierarchy) + Environment.NewLine);
            if (paper == null || paper.gameObject.activeSelf || bars.Length != 1 || !bars[0].gameObject.activeInHierarchy) return false;
        }
        return true;
    }

    private bool PresentationRestored(List<RecipeWidgetUIController> widgets)
    {
        for (int i = 0; i < widgets.Count; i++)
        {
            TopRecipeWidgetTile head = widgets[i].GetComponentInChildren<TopRecipeWidgetTile>(true);
            Transform paper = head == null ? null : FindNamed(head.transform, "RecipeWidgetTile_BackgroundTop");
            ProgressBarUI[] bars = widgets[i].GetComponentsInChildren<ProgressBarUI>(true);
            File.AppendAllText(Path.Combine(output, "presentation.tsv"), "native\t" + i + "\t"
                + (head != null) + "\t" + (paper == null ? "missing" : paper.gameObject.activeSelf.ToString())
                + "\t" + bars.Length + "\t" + (bars.Length == 1 && bars[0].gameObject.activeInHierarchy) + Environment.NewLine);
            if (paper == null || !paper.gameObject.activeSelf || bars.Length != 1 || !bars[0].gameObject.activeInHierarchy) return false;
        }
        return true;
    }

    private IEnumerator CaptureEntrance(List<RecipeWidgetUIController> widgets, Transform space,
        RecipeWidgetUIController incoming, string label, int columns)
    {
        string path = Path.Combine(output, label + ".tsv");
        File.WriteAllText(path, "frame\tseconds\tyMax\txMin\tshaderOffsetX\tshaderOffsetY\tstate\tnormalizedTime\tlength\tinTransition\n");
        UI_Move mover = null;
        Animator animator = null;
        bool first = false, descending = false, horizontal = false, monotonic = true, held = true, middle = false;
        float previousY = float.MaxValue, begin = Time.realtimeSinceStartup;
        bool nativeSeen = false, completed = false; float completionError = float.MaxValue;
        float deadline = begin + 4f; int frame = 0, stable = 0;
        float horizontalExtent = 0f, landingTime = -1f, descentTime = -1f, revealTime = -1f;
        bool branchHeld = true, revealAtDestination = true;
        int targetRow = (widgets.Count - 1) / columns;
        float targetHeadTop = HeadBounds(widgets[0], space).yMax - targetRow * (Bounds(widgets[0], space).height + 20.77099f);
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            Rect current;
            try { current = HeadBounds(incoming, space); }
            catch (InvalidOperationException) { continue; }
            float firstTop = HeadBounds(widgets[0], space).yMax;
            if (mover == null) mover = incoming.GetComponentInChildren<UI_Move>();
            if (animator == null) animator = incoming.GetComponentInChildren<Animator>();
            Vector2 offset = mover == null ? Vector2.zero : mover.Offset;
            horizontalExtent = Mathf.Max(horizontalExtent, offset.magnitude);
            float remaining = horizontalExtent > .01f ? offset.magnitude / horizontalExtent : 0f;
            float branch = BranchOpacity(incoming);
            if (branch > .01f && Mathf.Abs(current.yMax - targetHeadTop) > .5f) revealAtDestination = false;
            if (branch > .01f && revealTime < 0f) revealTime = Time.realtimeSinceStartup - begin;
            if (current.yMax < firstTop - 1f && descentTime < 0f) descentTime = Time.realtimeSinceStartup - begin;
            if (descentTime >= 0f && Mathf.Abs(current.yMax - targetHeadTop) < .5f && landingTime < 0f) landingTime = Time.realtimeSinceStartup - begin;
            if (descentTime >= 0f && landingTime < 0f && branch > .01f) branchHeld = false;
            AnimatorStateInfo state = animator == null ? new AnimatorStateInfo() : animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Override"));
            bool unfolding = state.IsName("Override.RecipeTransitionIn");
            if (unfolding) nativeSeen = true;
            if (nativeSeen && (!unfolding || state.normalizedTime >= 1f) && !completed)
            {
                completed = true;
                float expectedTop = targetHeadTop;
                completionError = Mathf.Abs(current.yMax - expectedTop);
                File.WriteAllText(Path.Combine(output, label + "-completion.txt"), "frame=" + frame + " error=" + completionError + " expectedTop=" + expectedTop + " actualTop=" + current.yMax + " normalized=" + state.normalizedTime);
            }
            File.AppendAllText(path, frame + "\t" + (Time.realtimeSinceStartup - begin) + "\t" + current.yMax + "\t" + current.xMin + "\t" + offset.x + "\t" + offset.y
                + "\t" + state.shortNameHash + "\t" + state.normalizedTime + "\t" + state.length + "\t" + (animator != null && animator.IsInTransition(0)) + Environment.NewLine);
            if (frame == 1 && animator != null)
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                    File.AppendAllText(Path.Combine(output, "native-clips.tsv"), clip.name + "\t" + clip.length + Environment.NewLine);
                for (int layer = 0; layer < animator.layerCount; layer++)
                    File.AppendAllText(Path.Combine(output, "native-layers.tsv"), layer + "\t" + animator.GetLayerName(layer) + Environment.NewLine);
            }
            if (animator != null)
                for (int layer = 1; layer < animator.layerCount; layer++)
                {
                    AnimatorStateInfo other = animator.GetCurrentAnimatorStateInfo(layer);
                    File.AppendAllText(Path.Combine(output, "native-layer-progress.tsv"), frame + "\t" + (Time.realtimeSinceStartup - begin)
                        + "\t" + layer + "\t" + other.shortNameHash + "\t" + other.normalizedTime + "\t" + other.length
                        + "\t" + animator.IsInTransition(layer) + Environment.NewLine);
                }
            if (offset.sqrMagnitude > .0001f)
            {
                if (!horizontal) ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-horizontal.png"));
                horizontal = true;
                if (remaining > .2f && current.yMax < firstTop - .5f) held = false;
            }
            if (!first)
            {
                Check(Mathf.Abs(current.yMax - firstTop) < .5f, "incoming card first visible frame shares first-row top");
                first = true;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-first-row.png"));
            }
            else if (current.yMax < firstTop - 1f)
            {
                descending = true;
                if (!middle && current.yMax < firstTop - 40f)
                { middle = true; ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-descending.png")); }
                if (current.yMax > previousY + .5f) monotonic = false;
            }
            stable = descending && Mathf.Abs(current.yMax - previousY) < .01f ? stable + 1 : 0;
            previousY = current.yMax;
            frame++;
            if (completed && stable >= 10) break;
        }
        Check(first, "incoming card became measurable in the native generated UI");
        Check(descending, "incoming card moves from first row toward lower target row");
        Check(horizontal && held, "first eighty percent of horizontal shader entrance stays in first row");
        Check(monotonic && middle, "descent has intermediate positions without upward jumps");
        Check(completed && completionError < .1f, "card stays at destination through native unfold completion");
        File.WriteAllText(Path.Combine(output, label + "-choreography.txt"), "descent=" + descentTime + " landing=" + landingTime + " reveal=" + revealTime + " duration=" + (landingTime - descentTime) + " target=" + targetHeadTop);
        Check(branchHeld && revealAtDestination && landingTime > descentTime && revealTime >= landingTime,
            "ingredients and cooking steps reveal only after quick landing");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-settled.png"));
    }
    private IEnumerator Settle(List<RecipeWidgetUIController> widgets, Transform space, string label)
    {
        Phase("settling " + label);
        float deadline = Time.realtimeSinceStartup + 12f, earliest = Time.realtimeSinceStartup + 2f;
        Rect[] previous = new Rect[widgets.Count]; int stable = 0;
        Vector2[] previousOffsets = new Vector2[widgets.Count];
        UI_Move[] movers = new UI_Move[widgets.Count];
        for (int i = 0; i < widgets.Count; i++) movers[i] = widgets[i].GetComponentInChildren<UI_Move>();
        Canvas.ForceUpdateCanvases();
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            bool unchanged = Time.realtimeSinceStartup >= earliest;
            for (int i = 0; i < widgets.Count; i++)
            {
                Rect current = Bounds(widgets[i], space);
                Vector2 offset = movers[i] == null ? Vector2.zero : movers[i].Offset;
                Rect before = previous[i];
                if (Mathf.Abs(current.xMin - before.xMin) > .01f || Mathf.Abs(current.yMin - before.yMin) > .01f
                    || Mathf.Abs(current.xMax - before.xMax) > .01f || Mathf.Abs(current.yMax - before.yMax) > .01f
                    || (offset - previousOffsets[i]).sqrMagnitude > .0001f) unchanged = false;
                previous[i] = current; previousOffsets[i] = offset;
            }
            stable = unchanged ? stable + 1 : 0;
            if (stable >= 20) break;
            yield return null;
        }
        Check(stable >= 20, label + ": graphic bounds and shader offset stable for 20 rendered frames");
        for (int i = 0; i < widgets.Count; i++)
            File.AppendAllText(Path.Combine(output, "shader-offsets.tsv"), label + "\t" + i + "\t" + previousOffsets[i].x + "\t" + previousOffsets[i].y + Environment.NewLine);
    }
    private void VerifyRows(List<RecipeWidgetUIController> widgets, RectTransform space, string label)
    {
        List<Rect> bounds = new List<Rect>();
        for (int i = 0; i < widgets.Count; i++)
        {
            Rect rect = Bounds(widgets[i], space); bounds.Add(rect);
            File.AppendAllText(Path.Combine(output, "geometry.tsv"), label + "\t" + i + "\t" + rect.xMin + "\t" + rect.yMin + "\t" + rect.xMax + "\t" + rect.yMax + Environment.NewLine);
        }
        Check(Mathf.Abs(bounds[0].yMax - bounds[1].yMax) < .1f, label + ": first two cards share row top");
        Check(bounds[2].yMax <= Mathf.Min(bounds[0].yMin, bounds[1].yMin) - 20.77099f + .1f,
            label + ": second row paper clears first row cookware by configured gap");
        if (bounds.Count > 4) Check(bounds[4].yMax <= Mathf.Min(bounds[2].yMin, bounds[3].yMin) - 20.77099f + .1f,
            label + ": third row clears tallest card in previous row");
    }

    private void VerifyColumn(List<RecipeWidgetUIController> widgets, Transform space, string label)
    {
        Rect previous = Bounds(widgets[0], space);
        float left = previous.xMin;
        for (int i = 0; i < widgets.Count; i++)
        {
            Rect current = Bounds(widgets[i], space);
            File.AppendAllText(Path.Combine(output, "geometry.tsv"), label + "\t" + i + "\t" + current.xMin + "\t" + current.yMin + "\t" + current.xMax + "\t" + current.yMax + Environment.NewLine);
            Check(Mathf.Abs(current.xMin - left) < .1f && (i == 0 || current.yMax <= previous.yMin - 20.77099f + .1f),
                label + ": card " + i + " stays in one column with cookware clearance");
            previous = current;
        }
    }
}
