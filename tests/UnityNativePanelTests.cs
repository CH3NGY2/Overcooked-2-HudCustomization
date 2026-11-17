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
    private static GameObject NativeRoot(object editor)
    {
        object panel = ReadField(editor, "_panel");
        return (GameObject)AccessTools.Property(panel.GetType(), "Root").GetValue(panel, null);
    }
    private IEnumerator NativeClick(object editor, string name)
    {
        yield return new WaitForEndOfFrame(); yield return null;
        GameObject root = NativeRoot(editor); EventSystem domain = root.GetComponent<EventSystem>();
        Transform popup = root.transform.Find("HudEditorPanel/CategoryPickerPopup");
        if (name.StartsWith("Category_", StringComparison.Ordinal) && !popup.gameObject.activeSelf)
            yield return NativeClick(editor, "CategoryPicker");
        Transform target = null;
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) if (button.name == name) { target = button.transform; break; }
        Check(target != null && target.gameObject.activeInHierarchy, "native editor button available: " + name);
        Canvas.ForceUpdateCanvases(); RectTransform rect = target as RectTransform;
        PointerEventData pointer = new PointerEventData(domain) {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); domain.RaycastAll(pointer, hits);
        GameObject first = null;
        foreach (RaycastResult hit in hits) { first = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (first != null) break; }
        File.AppendAllText(Path.Combine(output, "editor-input.tsv"), "native-click\t" + name + "\t" + pointer.position
            + "\tfirst=" + (first ? first.name : "none") + "\thits=" + hits.Count + "\n");
        Check(first == target.gameObject, "actual raycast reaches native editor button: " + name);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
    }
    private IEnumerator NativePanelScenario(object editor, BaseUnityPlugin hud)
    {
        GameObject root = NativeRoot(editor); object panel = ReadField(editor, "_panel");
        EventSystem domain = root.GetComponent<EventSystem>();
        Check(root.GetComponent<Canvas>() != null && root.GetComponent<GraphicRaycaster>() != null
            && root.GetComponent<StandaloneInputModule>() != null && domain.isActiveAndEnabled,
            "production floating panel uses native Canvas and active input module");
        Check(root.GetComponentsInChildren<Button>(true).Length == 15 && root.GetComponentsInChildren<InputField>(true).Length == 8,
            "one dropdown with ten items and dismissal, three actions, eight number fields without subitem tabs");
        Image paper = root.transform.Find("HudEditorPanel").GetComponent<Image>();
        Check(paper.sprite == null && paper.color.a > .95f && paper.transform.Find("PaperInset") == null,
            "one opaque panel background has no native decorative side strips or inset");
        Check(root.transform.Find("HudEditorPanel/Help") == null && root.transform.Find("HudEditorPanel/Selection") == null,
            "top click-category help and duplicate category heading removed");
        Transform popup = root.transform.Find("HudEditorPanel/CategoryPickerPopup");
        Check(!popup.gameObject.activeSelf && !root.transform.Find("HudEditorPanel/Category_0"),
            "ten categories stay collapsed behind one compact selector");
        yield return NativeClick(editor, "CategoryPicker");
        Check(popup.gameObject.activeSelf && (bool)AccessTools.Property(panel.GetType(), "BlocksHudInput").GetValue(panel, null),
            "opening category popup blocks underlying HUD dragging");
        RectTransform list = (RectTransform)popup.Find("CategoryList");
        Vector3[] popupCorners = new Vector3[4]; list.GetWorldCorners(popupCorners);
        Check(popupCorners[0].x >= 0 && popupCorners[2].x <= Screen.width
            && popupCorners[0].y >= 0 && popupCorners[2].y <= Screen.height,
            "all ten dropdown items fit inside the screen without clipping");
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "category-popup-ui\t" + list.rect.size
            + "\treusable-number-fields=8\told-panel-height=836\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-category-dropdown.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        for (int i = 0; i < 10; i++)
        {
            Button category = popup.Find("CategoryList/Category_" + i).GetComponent<Button>();
            bool available = (bool)Call(editor, "CategoryAvailable", i);
            Check(category.interactable == available, "category button reflects real level availability " + i);
            if (!available)
            {
                int previous = (int)ReadField(editor, "_editingKind");
                yield return NativeClick(editor, "Category_" + i);
                Check((int)ReadField(editor, "_editingKind") == previous, "disabled category ignores real pointer click " + i);
                Call(editor, "ToggleCategory", i);
                Check((int)ReadField(editor, "_editingKind") == previous, "missing category cannot be enabled by callback bypass " + i);
                Check(popup.gameObject.activeSelf, "disabled dropdown item keeps picker open " + i);
            }
        }
        yield return NativeClick(editor, "CategoryPickerDismiss");
        Check(!popup.gameObject.activeSelf && !(bool)AccessTools.Property(panel.GetType(), "BlocksHudInput").GetValue(panel, null),
            "outside pointer dismisses category list and releases HUD input blocking");
        Text title = root.transform.Find("HudEditorPanel/DragHeader/Title").GetComponent<Text>();
        Check(title.font != null && title.font.name == "Header_Font", "native header font survives frontend bundle unloading");
        RectTransform header = (RectTransform)ReadField(panel, "Header");
        Vector2 start = (Vector2)AccessTools.Property(panel.GetType(), "Position").GetValue(panel, null);
        float scale = (float)AccessTools.Property(panel.GetType(), "Scale").GetValue(panel, null);
        PointerEventData pointer = new PointerEventData(domain) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, header.TransformPoint(header.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); domain.RaycastAll(pointer, hits);
        GameObject drag = null;
        foreach (RaycastResult hit in hits) { drag = ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject); if (drag) break; }
        Check(drag == header.gameObject, "header raycast routes to panel drag handler");
        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.beginDragHandler);
        pointer.position += new Vector2(-120, -40) * scale;
        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.endDragHandler); yield return null;
        Vector2 moved = (Vector2)AccessTools.Property(panel.GetType(), "Position").GetValue(panel, null);
        Check((moved - start - new Vector2(-120, 40)).magnitude < 1f && ReadField(editor, "_selected") == null,
            "drag header moves native panel without selecting or moving HUD");
        AccessTools.Property(panel.GetType(), "Position").SetValue(panel, new Vector2(-9999, -9999), null);
        Check((Vector2)AccessTools.Property(panel.GetType(), "Position").GetValue(panel, null) == Vector2.zero, "panel clamps to screen top-left");
        AccessTools.Property(panel.GetType(), "Position").SetValue(panel, start, null);
        yield return NativeClick(editor, "Category_8");
        Check((int)ReadField(editor, "_editingKind") == 8 && root.GetComponentsInChildren<Slider>().Length == 4,
            "recipe category exposes original layout parameters inside native editor");
        Check(!popup.gameObject.activeSelf && root.transform.Find("HudEditorPanel/CategoryPicker/Label").GetComponent<Text>().text
            == (string)Call(editor, "CategoryName", 8), "choosing dropdown category closes list and updates localized caption");
        string disk = File.ReadAllText(hud.Config.ConfigFilePath);
        Slider slider = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_0/Slider").GetComponent<Slider>();
        float savedScale = (float)hud.Config["Recipes", "RecipeScale"].BoxedValue;
        slider.value = savedScale > 1.5f ? .75f : 1.5f; yield return null;
        object runtime = AccessTools.Field(hud.GetType(), "Runtime").GetValue(null);
        Check(Mathf.Abs((float)ReadField(ReadField(runtime, "Options"), "RecipeScale") - slider.value) < .001f
            && (float)hud.Config["Recipes", "RecipeScale"].BoxedValue == savedScale,
            "native slider previews real recipe adapter without changing ConfigEntry");
        InputField field = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Value").GetComponent<InputField>();
        Check(field.onValidateInput != null && field.onValidateInput("2", 1, '`') == '\0'
            && field.onValidateInput("2", 1, '~') == '\0' && field.onValidateInput("2", 1, '3') == '3',
            "save shortcut never inserts a backquote into native numeric text");
        field.text = "-";
        Call(editor, "End", true); Check(IsEditing(editor), "invalid native numeric draft blocks save without dropping editor");
        field.text = "2"; yield return null;
        Check(File.ReadAllText(hud.Config.ConfigFilePath) == disk, "native number preview leaves configuration bytes untouched");
        yield return RecipeEditorScenario(editor, hud);
        yield return ContentsEditorScenario(editor, hud);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-native-panel.png")); yield return new WaitForEndOfFrame(); yield return null;
        Rect bounds = (Rect)AccessTools.Property(panel.GetType(), "Bounds").GetValue(panel, null);
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "native-panel-ui\t" + bounds + "\tscale=" + scale + "\tdrag=" + (moved-start) + "\n");
        Call(editor, "End", false); yield return null;
        Check(File.ReadAllText(hud.Config.ConfigFilePath) == disk && Mathf.Abs((float)ReadField(ReadField(runtime, "Options"), "RecipeScale") - savedScale) < .001f,
            "cancel restores existing layout options and keeps saved bytes");
        Call(editor, "Begin"); yield return null;
        Check(IsEditing(editor) && NativeRoot(editor) == root, "native panel reused safely on second entry");
        yield return NativeClick(editor, "Category_8");
        field = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Value").GetComponent<InputField>();
        int savedColumns = (int)hud.Config["Recipes", "RecipesPerRow"].BoxedValue;
        field.text = savedColumns == 3 ? "2" : "3";
        yield return NativeClick(editor, "Save");
        Check(!IsEditing(editor) && (int)hud.Config["Recipes", "RecipesPerRow"].BoxedValue == (savedColumns == 3 ? 2 : 3),
            "native Save button commits original visible layout settings through real ConfigEntry");
        Call(editor, "Begin"); yield return null;
        yield return NativeClick(editor, "Category_8");
        field = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Value").GetComponent<InputField>();
        field.text = savedColumns.ToString();
        yield return NativeClick(editor, "Save");
        Call(editor, "Begin"); yield return null;
        AccessTools.Property(panel.GetType(), "Position").SetValue(panel, new Vector2(Screen.width / scale * .4f, 18f), null);
    }
}
