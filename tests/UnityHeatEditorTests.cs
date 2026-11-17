using System;
using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private IEnumerator HeatEditorScenario(object editor, BaseUnityPlugin hud, object registry)
    {
        Phase("barbeque native heat gauge editor and cold preview");
        ClientHeatedStationGUI selectedOwner = null;
        int stations = 0;
        foreach (ClientHeatedStationGUI owner in UnityEngine.Object.FindObjectsOfType<ClientHeatedStationGUI>())
            if (owner.isActiveAndEnabled)
            {
                stations++;
                if (!selectedOwner || !(HeatValueUIController)AccessTools.Field(typeof(ClientHeatedStationGUI), "m_uiInstance").GetValue(owner)) selectedOwner = owner;
            }
        HeatedStationGUI definition = selectedOwner ? selectedOwner.GetSynchronisedComponent() as HeatedStationGUI : null;
        File.AppendAllText(Path.Combine(output, "editor-heat-geometry.tsv"), "native-stations=" + stations
            + "\tconfig=" + GameUtils.GetLevelConfig().name + "\n");
        Check(Array.IndexOf(Environment.GetCommandLineArgs(), "-hudHeatTest") < 0 || stations > 0,
            "dedicated heat scenario has a real barbeque station");
        if (!definition)
            foreach (HeatedStationGUI candidate in Resources.FindObjectsOfTypeAll<HeatedStationGUI>())
                if (candidate.m_heatUIPrefab) { definition = candidate; break; }
        if (!definition)
        {
            Check(Array.IndexOf(Environment.GetCommandLineArgs(), "-hudHeatTest") < 0, "barbeque verification requires a native heat gauge prefab");
            File.AppendAllText(Path.Combine(output, "editor-heat-geometry.tsv"), "NOT COVERED: this kitchen has no loaded native barbeque heat prefab\n");
            yield break;
        }
        Type kindType = hud.GetType().Assembly.GetType("OC2HudCustomization.HudTargetKind");
        object heat = Enum.Parse(kindType, "Heat");
        GameObject fixture = null;
        HeatValueUIController display;
        HeatValueUIController original = selectedOwner
            ? (HeatValueUIController)AccessTools.Field(typeof(ClientHeatedStationGUI), "m_uiInstance").GetValue(selectedOwner) : null;
        bool originalActive = original && original.gameObject.activeSelf;
        float originalHeat = selectedOwner ? selectedOwner.GetComponent<ClientHeatedStation>().HeatValue : 0f;
        if (selectedOwner)
        {
            Call(registry, "ObserveOwner", selectedOwner);
            yield return NativeClick(editor, "Category_5");
            display = original;
            if (!display)
                foreach (HeatValueUIController item in UnityEngine.Object.FindObjectsOfType<HeatValueUIController>())
                    if (item.name == "HudEditorHeatPreview" && item.GetFollowTransform() == NetworkUtils.FindVisualRoot(selectedOwner.gameObject)) { display = item; break; }
            Check(display != null && (HeatValueUIController)AccessTools.Field(typeof(ClientHeatedStationGUI), "m_uiInstance").GetValue(selectedOwner) == original,
                "real barbeque preview leaves native business UI reference untouched");
            File.AppendAllText(Path.Combine(output, "editor-heat-geometry.tsv"), original
                ? "NATIVE STATION: existing native heat display\n" : "COLD STATION: owned display-only preview\n");
        }
        else
        {
            fixture = GameUtils.InstantiateUIController(definition.m_heatUIPrefab.gameObject, "HoverIconCanvas");
            display = fixture.GetComponent<HeatValueUIController>(); display.SetFollowTransform(null, new Vector3(.4f, .55f));
            fixture.SetActive(false);
            Call(registry, "Observe", display, display, heat, true);
            yield return NativeClick(editor, "Category_5");
            File.AppendAllText(Path.Combine(output, "editor-heat-geometry.tsv"), "DISPLAY FIXTURE: native prefab; cold station ownership not covered in this kitchen\n");
        }
        yield return new WaitForEndOfFrame(); yield return null;
        object target = null;
        foreach (object candidate in (IEnumerable)ReadField(registry, "_targets"))
            if (display && (RectTransform)ReadField(candidate, "Rect") == display.transform) { target = candidate; break; }
        Check(display && display.gameObject.activeInHierarchy && target != null, "heat category reveals a pickable native gauge");
        ProgressGaugeUI gauge = (ProgressGaugeUI)AccessTools.Field(typeof(HeatValueUIController), "m_progressBar").GetValue(display);
        Check(Mathf.Abs(gauge.Value - .5f) < .001f, "native heat gauge previews at half without heating the stove");
        Rect before = TargetBounds(target);
        Call(editor, "SetNumber", 2, 1.5f); Call(editor, "SetNumber", 5, 40f);
        yield return new WaitForEndOfFrame(); yield return null;
        Rect after = TargetBounds(target);
        File.AppendAllText(Path.Combine(output, "editor-heat-geometry.tsv"), "native-gauge\tbefore=" + before + "\tafter=" + after + "\n");
        Check(Mathf.Abs(after.width / before.width - 1.5f) < .04f
            && Mathf.Abs(display.GetComponent<CanvasGroup>().alpha - .4f) < .001f, "heat editor applies scale and opacity through common geometry");
        Check(!selectedOwner || Mathf.Approximately(selectedOwner.GetComponent<ClientHeatedStation>().HeatValue, originalHeat),
            "heat preview and geometry editing do not heat the real station");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-barbecue-heat.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        Call(editor, "ResetCategory");
        yield return NativeClick(editor, "Category_5"); yield return null;
        Check(selectedOwner ? original ? original.gameObject.activeSelf == originalActive : !display : !fixture.activeSelf,
            "leaving heat category destroys owned cold preview or restores native visibility");
        if (fixture) UnityEngine.Object.Destroy(fixture);
    }
}
