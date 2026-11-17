using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private IEnumerator ContentsEditorScenario(object editor, BaseUnityPlugin hud)
    {
        Phase("contents shared geometry and independent subitem opacity; native display fixture");
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
        for (int i = 0; i < 7; i++) Call(editor, "SetNumber", i, i == 2 || i == 3 || i == 4 ? 1f : i == 6 ? 100f : 0f);
        Call(editor, "SelectContentsPart", 0); Call(editor, "SetNumber", 6, 100f);
        Call(editor, "SelectContentsPart", 1); Call(editor, "SetNumber", 6, 100f);
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        GameObject generated = (GameObject)AccessTools.Field(typeof(UISubElementContainer), "m_container").GetValue(ui);
        RectTransform region = (RectTransform)generated.transform;
        AnchorGridLayoutGroup grid = generated.GetComponent<AnchorGridLayoutGroup>();
        List<Image> icons = (List<Image>)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_icons").GetValue(ui);
        Sprite empty = (Sprite)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_emptySprite").GetValue(ui);
        Vector2 min = region.offsetMin, max = region.offsetMax, nativeSize = region.rect.size, cell = grid.cellSize;
        Vector2 nativeAnchor = hover.GetOffset(); Transform follower = hover.GetFollowTransform();
        Call(editor, "SelectContentsPart", 0);
        Slider x = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_0/Slider").GetComponent<Slider>();
        Slider y = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Slider").GetComponent<Slider>();
        x.value = 7; y.value = -4;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_2/Slider").GetComponent<Slider>().value = 1.2f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_3/Slider").GetComponent<Slider>().value = 1.5f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_4/Slider").GetComponent<Slider>().value = .75f;
        root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_5/Value").GetComponent<InputField>().text = "2";
        Slider alpha = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_6/Slider").GetComponent<Slider>();
        alpha.value = 35;
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
        yield return NativeClick(editor, "ContentsPart_1");
        Check(Mathf.Abs((float)Call(editor, "GetNumber", 0) - 7f) < .001f
            && Mathf.Abs((float)Call(editor, "GetNumber", 1) + 4f) < .001f, "ingredient subitem shares plus position");
        alpha.value = 65; yield return null; yield return new WaitForEndOfFrame(); yield return null;
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
        EventSystem domain = root.GetComponent<EventSystem>();
        ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(domain) { scrollDelta = new Vector2(0, -20) }, ExecuteEvents.scrollHandler);
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        Vector3[] edge = new Vector3[4]; alpha.GetComponent<RectTransform>().GetWorldCorners(edge);
        Vector3[] clip = new Vector3[4]; scroll.viewport.GetWorldCorners(clip);
        Check(edge[0].y >= clip[0].y - 1 && edge[1].y <= clip[1].y + 1,
            "scroll handler exposes final opacity control inside real masked viewport");
        Button part = root.transform.Find("HudEditorPanel/ContentsPart_0").GetComponent<Button>();
        Check(part.transform.parent == root.transform.Find("HudEditorPanel") && part.gameObject.activeInHierarchy,
            "contents subitem buttons stay outside scrolling parameter viewport");
        File.AppendAllText(Path.Combine(output, "editor-contents-geometry.tsv"), "native-size=" + nativeSize + "\tcell=" + cell
            + "\tcustom-size=" + region.rect.size + "\ticon=" + pixels + "\toffset=" + measured + "\tcolumns=" + grid.constraintCount + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-contents-subitems.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        // Restore exact saved state with the session's production cancellation path.
        string disk = File.ReadAllText(hud.Config.ConfigFilePath);
        Call(editor, "End", false); yield return null; yield return new WaitForEndOfFrame(); yield return null;
        Check(File.ReadAllText(hud.Config.ConfigFilePath) == disk, "contents draft and subitem changes do not persist after cancel");
        Call(adapter, "Suspend", owner, true);
        Check((region.offsetMin - min).magnitude < .05f && (region.offsetMax - max).magnitude < .05f,
            "contents adapter release restores native geometry");
        AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance").SetValue(owner, null);
        UnityEngine.Object.Destroy(fixture); UnityEngine.Object.Destroy(ownerObject); yield return null;
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_8");
    }
}
