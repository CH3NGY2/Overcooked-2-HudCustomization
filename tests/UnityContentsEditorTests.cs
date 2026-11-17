using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private IEnumerator ContentsEditorScenario(object editor, BaseUnityPlugin hud)
    {
        Phase("contents drag/resize and simultaneous independent opacity controls; native display fixtures");
        IngredientContentsUIContainer prefab = null;
        foreach (IngredientContentGUI definition in Resources.FindObjectsOfTypeAll<IngredientContentGUI>())
            if (definition.m_ingredientContentUIPrefab) { prefab = definition.m_ingredientContentUIPrefab; break; }
        Check(prefab != null, "native contents prefab loaded for display-only fixture");
        IngredientOrderNode ingredient = null;
        foreach (IngredientOrderNode node in Resources.FindObjectsOfTypeAll<IngredientOrderNode>())
            if (node.m_iconSprite) { ingredient = node; break; }
        Check(ingredient != null, "real ingredient sprite for native contents classification");
        GameObject fixture = GameUtils.InstantiateUIController(prefab.gameObject, "HoverIconCanvas");
        fixture.name = "HudContentsEditorFixture";
        IngredientContentsUIContainer ui = fixture.GetComponent<IngredientContentsUIContainer>();
        HoverIconUIController hover = fixture.GetComponent<HoverIconUIController>();
        hover.SetFollowTransform(null, new Vector3(.27f, .48f));
        ui.SetMinimumSize(5); ui.SetOrder(new IngredientAssembledNode(ingredient));
        GameObject ownerObject = new GameObject("HudContentsEditorFixtureOwner"); ownerObject.SetActive(false);
        ClientIngredientContentGUI owner = ownerObject.AddComponent<ClientIngredientContentGUI>();
        AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance").SetValue(owner, ui);
        object runtime = AccessTools.Field(hud.GetType(), "Runtime").GetValue(null);
        object adapter = ReadField(runtime, "Contents");
        Call(adapter, "Observe", owner, ReadField(runtime, "Options"));
        yield return null;
        Check((bool)Call(editor, "CategoryAvailable", 9), "real registered contents instance enables category");
        yield return NativeClick(editor, "Category_9");
        GameObject root = NativeRoot(editor);
        for (int i = 0; i < 8; i++) Call(editor, "SetNumber", i, i == 2 || i == 3 || i == 4 ? 1f : i >= 6 ? 100f : 0f);
        Check(root.transform.Find("HudEditorPanel/ContentsPart_0") == null
            && root.transform.Find("HudEditorPanel/ContentsPart_1") == null
            && !root.transform.Find("HudEditorPanel/PreviewStatus").gameObject.activeSelf,
            "contents has neither opacity tabs nor shared-layout explanation");
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        GameObject generated = (GameObject)AccessTools.Field(typeof(UISubElementContainer), "m_container").GetValue(ui);
        RectTransform region = (RectTransform)generated.transform;
        AnchorGridLayoutGroup grid = generated.GetComponent<AnchorGridLayoutGroup>();
        List<Image> icons = (List<Image>)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_icons").GetValue(ui);
        Sprite empty = (Sprite)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_emptySprite").GetValue(ui);
        Vector2 min = region.offsetMin, max = region.offsetMax, nativeSize = region.rect.size, cell = grid.cellSize;
        Vector2 nativeAnchor = hover.GetOffset(); Transform follower = hover.GetFollowTransform();
        Slider x = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_0/Slider").GetComponent<Slider>();
        Slider y = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Slider").GetComponent<Slider>();
        x.value = 7; y.value = -4;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_2/Slider").GetComponent<Slider>().value = 1.2f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_3/Slider").GetComponent<Slider>().value = 1.5f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_4/Slider").GetComponent<Slider>().value = .75f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_5/Value").GetComponent<InputField>().text = "2";
        Slider ingredientAlpha = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_6/Slider").GetComponent<Slider>();
        Slider plusAlpha = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_7/Slider").GetComponent<Slider>();
        ingredientAlpha.value = 65; plusAlpha.value = 35;
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        Check((int)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_iconsInUse").GetValue(ui) == 5
            && grid.constraintCount == 2, "two-column display layout retains five native icons and capacity");
        Vector2 pixels = Vector2.Scale(region.rect.size, grid.cellSize);
        Vector2 expected = Vector2.Scale(Vector2.Scale(nativeSize, cell), new Vector2(1.8f, .9f));
        Check((pixels - expected).magnitude < .05f, "independent width and height preserve individual icon size when wrapping");
        Canvas canvas = region.GetComponentInParent<Canvas>(); RectTransform canvasRect = (RectTransform)canvas.rootCanvas.transform;
        Vector3 measured = canvasRect.InverseTransformVector(region.parent.TransformVector((region.offsetMin + region.offsetMax - min - max) * .5f));
        Check(Mathf.Abs(measured.x / canvasRect.rect.width - .07f) < .0001f
            && Mathf.Abs(measured.y / canvasRect.rect.height + .04f) < .0001f, "shared X/Y correspond to normalized root Canvas position");
        foreach (Image icon in icons)
            if (icon && icon.gameObject.activeSelf) Check(icon.GetComponent<CanvasGroup>() != null
                && Mathf.Abs(icon.GetComponent<CanvasGroup>().alpha - (icon.sprite == empty ? .35f : .65f)) < .001f,
                "plus and ingredient opacity apply independently to native icon sprite class");
        Check(hover.GetOffset() == nativeAnchor && hover.GetFollowTransform() == follower,
            "contents offsets preserve native follow target, anchor and shader controller");
        Vector2 appliedMin = region.offsetMin, appliedMax = region.offsetMax;
        for (int i = 0; i < 4; i++) { ui.SetMinimumSize(5); yield return null; yield return new WaitForEndOfFrame(); }
        Check((region.offsetMin - appliedMin).magnitude < .05f && (region.offsetMax - appliedMax).magnitude < .05f,
            "repeated native refresh restores baseline before applying shared geometry without drift");
        ui.SetOrder(AssembledDefinitionNode.NullNode); yield return null; yield return new WaitForEndOfFrame(); yield return null;
        foreach (Image icon in icons) if (icon && icon.gameObject.activeSelf)
            Check(Mathf.Abs(icon.GetComponent<CanvasGroup>().alpha - .35f) < .001f, "reused ingredient becomes plus with correct opacity");
        ui.SetOrder(new IngredientAssembledNode(ingredient)); yield return null;
        ScrollRect scroll = root.transform.Find("HudEditorPanel/ParameterViewport").GetComponent<ScrollRect>();
        Vector3[] clip = new Vector3[4]; scroll.viewport.GetWorldCorners(clip);
        foreach (Slider opacity in new Slider[] { ingredientAlpha, plusAlpha })
        {
            Vector3[] edge = new Vector3[4]; opacity.GetComponent<RectTransform>().GetWorldCorners(edge);
            Check(opacity.gameObject.activeInHierarchy && edge[0].y >= clip[0].y - 1 && edge[1].y <= clip[1].y + 1,
                "both opacity sliders visible together inside real masked viewport: " + opacity.transform.parent.name);
        }
        Check(root.GetComponentsInChildren<Slider>().Length == 8,
            "all eight contents parameters simultaneously available without switching tabs");
        File.AppendAllText(Path.Combine(output, "editor-contents-geometry.tsv"), "native-size=" + nativeSize + "\tcell=" + cell
            + "\tcustom-size=" + region.rect.size + "\ticon=" + pixels + "\toffset=" + measured + "\tcolumns=" + grid.constraintCount + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-contents-opacity.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        object state = ((IDictionary)ReadField(adapter, "_states"))[ui];
        GameObject other = GameUtils.InstantiateUIController(prefab.gameObject, "HoverIconCanvas");
        other.name = "HudContentsEditorOtherFixture";
        IngredientContentsUIContainer otherUi = other.GetComponent<IngredientContentsUIContainer>();
        other.GetComponent<HoverIconUIController>().SetFollowTransform(null, new Vector3(.43f, .48f));
        otherUi.SetMinimumSize(5); otherUi.SetOrder(new IngredientAssembledNode(ingredient));
        GameObject otherOwnerObject = new GameObject("HudContentsEditorOtherOwner"); otherOwnerObject.SetActive(false);
        ClientIngredientContentGUI otherOwner = otherOwnerObject.AddComponent<ClientIngredientContentGUI>();
        AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance").SetValue(otherOwner, otherUi);
        Call(adapter, "Observe", otherOwner, ReadField(runtime, "Options"));
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        object otherState = ((IDictionary)ReadField(adapter, "_states"))[otherUi];
        yield return StableContentsBounds(state, otherState);
        Rect before = TargetBounds(state), otherBefore = TargetBounds(otherState);
        Check(Mathf.Abs(otherBefore.center.x-before.center.x-Screen.width*.16f) < 1f
            && Mathf.Abs(otherBefore.center.y-before.center.y) < 1f,
            "new native instance inherits saved draft geometry after its initial zero parent scale settles");
        Vector2 delta = new Vector2(60f, -35f);
        yield return GuiInput(EventType.MouseDown, before.center);
        Check(ReferenceEquals(ReadField(editor, "_selected"), state), "actual displayed contents group selected through production pointer path");
        yield return GuiInput(EventType.MouseDrag, before.center + delta);
        yield return GuiInput(EventType.MouseUp, before.center + delta);
        yield return new WaitForEndOfFrame(); yield return null;
        Rect moved = TargetBounds(state), otherMoved = TargetBounds(otherState);
        File.AppendAllText(Path.Combine(output, "editor-contents-geometry.tsv"), "drag-before=" + before + "\tdrag-after=" + moved
            + "\tother-before=" + otherBefore + "\tother-after=" + otherMoved + "\tdelta=" + delta + "\n");
        Check((moved.center - before.center - delta).magnitude < 1f
            && (otherMoved.center - otherBefore.center - delta).magnitude < 1f,
            "dragging one contents group moves both global instances by the same screen offset");
        Check(Mathf.Abs((float)Call(editor, "GetNumber", 0) - (7f + delta.x * 100f / Screen.width)) < .01f
            && Mathf.Abs((float)Call(editor, "GetNumber", 1) - (-4f - delta.y * 100f / Screen.height)) < .01f,
            "contents mouse movement updates the same normalized X/Y parameters");
        Vector2 edgePoint = new Vector2(moved.xMax, moved.center.y);
        yield return GuiInput(EventType.MouseDown, edgePoint);
        yield return GuiInput(EventType.MouseDrag, edgePoint + new Vector2(30, 0));
        yield return GuiInput(EventType.MouseUp, edgePoint + new Vector2(30, 0));
        yield return new WaitForEndOfFrame(); yield return null;
        Rect resized = TargetBounds(state);
        Check(Mathf.Abs(resized.xMin - moved.xMin) < 1f && Mathf.Abs(resized.width - moved.width - 30f) < 1f
            && Mathf.Abs(resized.height - moved.height) < 1f, "contents right edge resizes width and keeps opposite edge fixed");
        Vector2 corner = new Vector2(resized.xMax, resized.yMax), cornerDelta = resized.size * .1f;
        yield return GuiInput(EventType.MouseDown, corner);
        yield return GuiInput(EventType.MouseDrag, corner + cornerDelta);
        yield return GuiInput(EventType.MouseUp, corner + cornerDelta);
        yield return new WaitForEndOfFrame(); yield return null;
        Rect scaled = TargetBounds(state);
        Check(Mathf.Abs(scaled.xMin - resized.xMin) < 1f && Mathf.Abs(scaled.yMin - resized.yMin) < 1f
            && Mathf.Abs(scaled.width / resized.width - 1.1f) < .01f && Mathf.Abs(scaled.height / resized.height - 1.1f) < .01f,
            "contents corner scales proportionally while pinning opposite corner");
        ingredientAlpha.value = 0; plusAlpha.value = 0;
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        Rect transparent = TargetBounds(state);
        yield return GuiInput(EventType.MouseDown, transparent.center);
        Check(ReferenceEquals(ReadField(editor, "_selected"), state), "fully transparent contents remain selectable by displayed geometry");
        yield return GuiInput(EventType.MouseUp, transparent.center);
        ingredientAlpha.value = 65; plusAlpha.value = 35; yield return null;
        Check(Mathf.Abs((float)Call(editor, "GetNumber", 6) - 65f) < .01f
            && Mathf.Abs((float)Call(editor, "GetNumber", 7) - 35f) < .01f,
            "drag and resize preserve two independent opacity values");
        File.AppendAllText(Path.Combine(output, "editor-contents-geometry.tsv"), "drag-before=" + before + "\tdrag-after=" + moved
            + "\tother-before=" + otherBefore + "\tother-after=" + otherMoved + "\tresized=" + resized + "\tscaled=" + scaled + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-contents-dragged.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        // Restore exact saved state with the session's production cancellation path.
        string disk = File.ReadAllText(hud.Config.ConfigFilePath);
        Call(editor, "End", false); yield return null; yield return new WaitForEndOfFrame(); yield return null;
        Check(File.ReadAllText(hud.Config.ConfigFilePath) == disk, "contents drag, resize and both opacity changes do not persist after cancel");
        object originalOptions = ReadField(runtime, "Options");
        Dictionary<BepInEx.Configuration.ConfigDefinition, object> originalValues = new Dictionary<BepInEx.Configuration.ConfigDefinition, object>();
        foreach (KeyValuePair<BepInEx.Configuration.ConfigDefinition, BepInEx.Configuration.ConfigEntryBase> entry in hud.Config)
            originalValues.Add(entry.Key, entry.Value.BoxedValue);
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_9");
        Call(editor, "SetNumber", 0, 8f); Call(editor, "SetNumber", 6, 61f); Call(editor, "SetNumber", 7, 29f);
        yield return NativeClick(editor, "Save");
        Check(!IsEditing(editor) && Mathf.Abs((float)hud.Config["HudEditor.Contents", "X"].BoxedValue - .08f) < .001f
            && Mathf.Abs((float)hud.Config["Opacity", "IngredientOpacity"].BoxedValue - 61f) < .001f
            && Mathf.Abs((float)hud.Config["Opacity", "EmptySlotOpacity"].BoxedValue - 29f) < .001f,
            "native Save commits contents position and both independent opacity entries");
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_9");
        Check(Mathf.Abs((float)Call(editor, "GetNumber", 0)-8f) < .001f
            && Mathf.Abs((float)Call(editor, "GetNumber", 6)-61f) < .001f
            && Mathf.Abs((float)Call(editor, "GetNumber", 7)-29f) < .001f,
            "contents geometry and both opacity controls reload after editor reentry");
        Call(editor, "ResetCategory");
        Check((float)Call(editor, "GetNumber", 0) == 0f && (float)Call(editor, "GetNumber", 6) == 100f
            && (float)Call(editor, "GetNumber", 7) == 100f, "contents reset restores shared geometry and both opacity values");
        AccessTools.Property(editor.GetType(), "DraftOptions").SetValue(editor, originalOptions, null);
        yield return NativeClick(editor, "Save");
        foreach (KeyValuePair<BepInEx.Configuration.ConfigDefinition, object> entry in originalValues)
            Check(object.Equals(hud.Config[entry.Key].BoxedValue, entry.Value), "save test restores original entry value: " + entry.Key);
        // The outer editor scenario reset hidden values only in memory. A real Save
        // serializes those plus generated comments; restore this isolated test snapshot
        // separately instead of expecting BepInEx serialization to preserve old bytes.
        File.WriteAllText(hud.Config.ConfigFilePath, disk);
        Call(adapter, "Suspend", owner, true);
        Call(adapter, "Suspend", otherOwner, true);
        object[] releasedBounds = new object[] { new Rect() };
        Check(!(bool)Call(state, "TryBounds", releasedBounds), "released contents cannot remain selected while display object is awaiting destruction");
        Check((region.offsetMin - min).magnitude < .05f && (region.offsetMax - max).magnitude < .05f,
            "contents adapter release restores native geometry");
        AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance").SetValue(owner, null);
        AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance").SetValue(otherOwner, null);
        UnityEngine.Object.Destroy(fixture); UnityEngine.Object.Destroy(ownerObject);
        UnityEngine.Object.Destroy(other); UnityEngine.Object.Destroy(otherOwnerObject); yield return null;
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_8");
    }
    private IEnumerator StableContentsBounds(object first, object second)
    {
        Rect a = TargetBounds(first), b = TargetBounds(second); int frames = 0;
        float deadline = Time.realtimeSinceStartup + 4f;
        while (frames < 5 && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame(); yield return null;
            Rect nextA = TargetBounds(first), nextB = TargetBounds(second);
            frames = (nextA.center-a.center).magnitude < .05f && (nextB.center-b.center).magnitude < .05f
                && (nextA.size-a.size).magnitude < .05f && (nextB.size-b.size).magnitude < .05f ? frames + 1 : 0;
            a = nextA; b = nextB;
        }
        Check(frames == 5, "both native contents groups settled over five rendered frames before pointer input");
    }
}
