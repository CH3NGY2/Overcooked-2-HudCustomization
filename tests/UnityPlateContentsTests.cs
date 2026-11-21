using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private IEnumerator PlateContentsScenario(BaseUnityPlugin hud)
    {
        Phase("native plate: first visible burger and added ingredient");
        ClientIngredientContentGUI owner = null;
        ClientIngredientContainer contents = null;
        IngredientContentsUIContainer prefab = null;
        var instance = AccessTools.Field(typeof(ClientIngredientContentGUI), "m_uiInstance");
        foreach (ClientPlate plate in FindObjectsOfType<ClientPlate>())
        {
            var candidate = plate.GetComponent<ClientIngredientContentGUI>();
            var definition = plate.GetComponent<IngredientContentGUI>();
            var container = plate.GetComponent<ClientIngredientContainer>();
            if (!candidate || !definition || !definition.m_ingredientContentUIPrefab || definition.m_displayEmptyElements
                || !container || container.HasContents() || instance.GetValue(candidate) != null) continue;
            owner = candidate; contents = container; prefab = definition.m_ingredientContentUIPrefab; break;
        }
        Check(owner && contents && prefab, "native empty plate has a contents owner and has never created its HUD");
        var defaultOrder = (OrderDefinitionNode)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_orderDefinitionNode").GetValue(prefab);
        Check(defaultOrder && defaultOrder.name == "BeefBurger", "stock contents prefab starts with the same beef burger shown by its prebuilt icons");
        AssembledDefinitionNode burger = defaultOrder.Simpilfy();
        IngredientOrderNode topping = null;
        foreach (IngredientOrderNode ingredient in Resources.FindObjectsOfTypeAll<IngredientOrderNode>())
        {
            if (!ingredient.m_iconSprite) continue;
            bool used = false;
            foreach (AssembledDefinitionNode node in burger)
            {
                var part = node as IngredientAssembledNode;
                if (part != null && part.m_ingriedientOrderNode == ingredient) used = true;
            }
            if (!used && (!topping || ingredient.name.ToLowerInvariant().Contains("cheese"))) topping = ingredient;
        }
        Check(topping, "a real third ingredient sprite distinct from bun and beef is loaded");
        hud.Config["General", "Enabled"].BoxedValue = true;
        hud.Config["Contents", "ContentsScale"].BoxedValue = .5f;
        hud.Config["Contents", "ContentsPerRow"].BoxedValue = 1;
        hud.Config["HudEditor.Contents", "X"].BoxedValue = 0f;
        hud.Config["HudEditor.Contents", "Y"].BoxedValue = 0f;
        hud.Config["HudEditor.Contents", "Width"].BoxedValue = 1f;
        hud.Config["HudEditor.Contents", "Height"].BoxedValue = 1f;
        hud.Config["Opacity", "IngredientOpacity"].BoxedValue = 64f;
        yield return null;

        UpdatePlate(contents, burger);
        var ui = (IngredientContentsUIContainer)instance.GetValue(owner);
        Check(ui && ui.gameObject.activeInHierarchy, "client contents update creates and activates the native plate HUD");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "plate-first.png"));
        yield return new WaitForEndOfFrame();
        Vector2 first = MeasurePlateIcons(ui, "first visible beef burger", 2);
        bool firstAlpha = Mathf.Abs(PlateAlpha(PlateIcons(ui)[0]) - .64f) < .01f;
        int firstColumns = ui.GetComponentInChildren<AnchorGridLayoutGroup>().constraintCount;

        yield return null;
        var withTopping = new CompositeAssembledNode { m_composition = new AssembledDefinitionNode[] { burger, new IngredientAssembledNode(topping) } };
        UpdatePlate(contents, withTopping);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "plate-added.png"));
        yield return new WaitForEndOfFrame();
        Vector2 added = MeasurePlateIcons(ui, "first frame after " + topping.name, 3);
        Check(Mathf.Abs(PlateAlpha(PlateIcons(ui)[0]) - .64f) < .01f,
            "native plate refresh keeps the configured ingredient opacity");

        hud.Config["General", "Enabled"].BoxedValue = false;
        yield return null; yield return new WaitForEndOfFrame();
        Vector2 native = MeasurePlateIcons(ui, "disabled native", 3);
        Check((first - native * .5f).magnitude < .1f && (first - added).magnitude < .1f,
            "first rendered beef burger matches configured half-size before and after adding a third ingredient");
        Check(firstAlpha && firstColumns == 1, "first rendered plate also uses configured opacity and one-column layout");
        Check(Mathf.Approximately(PlateAlpha(PlateIcons(ui)[0]), 1f), "disable restores native plate opacity");
        UpdatePlate(contents);
        Check(!ui.gameObject.activeSelf && !contents.HasContents(), "native empty state still hides the plate display");
    }

    private static void UpdatePlate(ClientIngredientContainer plate, params AssembledDefinitionNode[] contents)
    {
        // This is an isolated couch game. Invoke the real client handler locally with a
        // display fixture payload and let ClientPlate raise the normal order callback.
        var message = new IngredientContainerMessage();
        message.Initialise(contents);
        plate.ApplyServerEvent(message);
    }

    private static List<Image> PlateIcons(IngredientContentsUIContainer ui)
    { return (List<Image>)AccessTools.Field(typeof(IngredientContentsUIContainer), "m_icons").GetValue(ui); }

    private static float PlateAlpha(Image icon)
    { CanvasGroup group = icon.GetComponent<CanvasGroup>(); return group ? group.alpha : 1f; }

    private Vector2 MeasurePlateIcons(IngredientContentsUIContainer ui, string label, int count)
    {
        List<Image> icons = PlateIcons(ui);
        int active = 0;
        foreach (Image icon in icons) if (icon.isActiveAndEnabled && !icon.canvasRenderer.cull && icon.sprite) active++;
        Check(active == count, label + ": expected active stock icons are rendered");
        Image first = icons[0];
        first.rectTransform.GetWorldCorners(corners);
        Canvas canvas = first.canvas;
        GraphicRaycaster raycaster = first.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster ? raycaster.eventCamera : canvas.worldCamera;
        Vector3 offset = canvas.transform.TransformVector((Vector3)ui.GetComponent<UI_Move>().Offset);
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0] + offset);
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2] + offset);
        Vector2 size = upper - lower;
        File.AppendAllText(Path.Combine(output, "plate-geometry.tsv"), label + "\tframe=" + Time.frameCount + "\tsize=" + size
            + "\tpos=" + lower + "\talpha=" + PlateAlpha(first)
            + "\tinUse=" + AccessTools.Field(typeof(IngredientContentsUIContainer), "m_iconsInUse").GetValue(ui) + "\n");
        Check(lower.x >= 0f && lower.y >= 0f && upper.x <= Screen.width && upper.y <= Screen.height
            && size.x > 1f && size.y > 1f, label + ": first icon has nonzero on-screen geometry after native layout");
        return size;
    }
}
