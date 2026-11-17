using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private const BindingFlags EditorMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static object ReadField(object target, string name) { return AccessTools.Field(target.GetType(), name).GetValue(target); }
    private static object Call(object target, string name, params object[] values) { return AccessTools.Method(target.GetType(), name).Invoke(target, values); }
    private static bool IsEditing(object editor) { return (bool)AccessTools.Property(editor.GetType(), "Active").GetValue(editor, null); }
    private static Event _pendingEditorEvent, _injectedEditorEvent;
    private static void InjectEditorEvent(out Event __state)
    {
        __state = null;
        if (_pendingEditorEvent == null || Event.current == null || Event.current.type != EventType.Repaint) return;
        __state = new Event(Event.current);
        _injectedEditorEvent = _pendingEditorEvent;
        Event.current.type = _pendingEditorEvent.type; Event.current.button = _pendingEditorEvent.button;
        Event.current.mousePosition = _pendingEditorEvent.mousePosition;
        _pendingEditorEvent = null;
    }
    private static void EditorMatrixChanged()
    {
        // GUIClip recomputes mousePosition from the physical cursor whenever its matrix
        // changes. Keep scripted input in the same local coordinates as a native event.
        if (_injectedEditorEvent != null && Event.current != null)
            Event.current.mousePosition = GUI.matrix.inverse.MultiplyPoint3x4(_injectedEditorEvent.mousePosition);
    }
    private static void RestoreEditorEvent(Event __state)
    {
        if (__state == null) return;
        File.AppendAllText(Path.Combine(output, "editor-input.tsv"), Event.current.type + "\t" + Event.current.mousePosition
            + "\thot=" + GUIUtility.hotControl + "\tenabled=" + GUI.enabled + "\n");
        _injectedEditorEvent = null; Event.current = __state;
    }
    private IEnumerator GuiInput(EventType type, Vector2 screenPoint)
    {
        _pendingEditorEvent = new Event { type = type, button = 0, mousePosition = screenPoint };
        float deadline = Time.realtimeSinceStartup + 3f;
        while (_pendingEditorEvent != null && Time.realtimeSinceStartup < deadline) yield return null;
        Check(_pendingEditorEvent == null, "scripted pointer delivered through production OnGUI: " + type);
        yield return null;
    }
    private IEnumerator GuiCategory(object editor, int kind)
    {
        if ((int)ReadField(editor, "_editingKind") != kind) yield return NativeClick(editor, "Category_" + kind);
    }
    private IEnumerator GuiPreview(object editor, int kind)
    {
        yield return NativeClick(editor, "Category_" + kind);
    }
    private static Rect TargetBounds(object target)
    {
        object[] args = new object[] { new Rect() };
        if (!(bool)Call(target, "TryBounds", args)) throw new InvalidOperationException("Target lacks visible bounds " + ReadField(target, "Kind"));
        return (Rect)args[0];
    }
    private static object Target(object registry, string kind)
    {
        foreach (object target in (IEnumerable)ReadField(registry, "_targets"))
            if (ReadField(target, "Kind").ToString() == kind && (bool)ReadField(target, "Pickable"))
            { object[] args = new object[] { new Rect() }; if ((bool)Call(target, "TryBounds", args)) return target; }
        return null;
    }
    private IEnumerator EditorScenario()
    {
        Phase("HUD editor native action, input, freeze and geometry");
        BaseUnityPlugin hud = Chainloader.PluginInfos["com.ch3ngyz.plugin.overcooked2.hudcustomization"].Instance;
        object editor = AccessTools.Field(hud.GetType(), "Editor").GetValue(null);
        Check(editor != null, "production editor initialized with all native patch contracts");
        object registry = ReadField(editor, "Targets");
        hud.Config.SaveOnConfigSet = false; hud.Config["General", "Enabled"].BoxedValue = true;
        foreach (KeyValuePair<BepInEx.Configuration.ConfigDefinition, BepInEx.Configuration.ConfigEntryBase> pair in hud.Config)
            if (pair.Key.Section.StartsWith("HudEditor.", StringComparison.Ordinal)) pair.Value.BoxedValue = pair.Value.DefaultValue;
        yield return new WaitForSecondsRealtime(1.2f);
        object assets = ReadField(editor, "_assets");
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "native-assets\tready="
            + AccessTools.Property(assets.GetType(), "Ready").GetValue(assets, null) + "\theader=" + ReadField(assets, "Header")
            + "\tbodyFont=" + ReadField(assets, "BodyFont") + "\theaderFont=" + ReadField(assets, "HeaderFont") + "\n");
        Check((bool)Call(editor, "CanEnter"), "local kitchen editor action enabled");
        object service = Chainloader.PluginInfos["com.ch3ngyz.plugin.overcooked2.modsettings"].Instance;
        Call(service, "OpenSettings"); yield return new WaitForSecondsRealtime(.8f);
        object shortcut = ReadField(service, "shortcut"), page = ReadField(shortcut, "page");
        Check(page != null && ((Component)page).gameObject.activeInHierarchy, "actual ModSettings page open");
        Transform tab = ((Component)page).transform.Find("SettingsBody/ModSettingsContent/Mods/Viewport/Content/Mod_com.ch3ngyz.plugin.overcooked2.hudcustomization");
        if (!tab)
            foreach (T17Button button in ((Component)page).GetComponentsInChildren<T17Button>(true))
                if (button.name == "Mod_com.ch3ngyz.plugin.overcooked2.hudcustomization") { tab = button.transform; break; }
        Check(tab != null, "actual HUD catalog tab available");
        T17EventSystem domain = (T17EventSystem)AccessTools.Property(page.GetType(), "Domain").GetValue(page, null);
        ExecuteEvents.Execute(tab.gameObject, new BaseEventData(domain), ExecuteEvents.submitHandler);
        yield return new WaitForSecondsRealtime(.7f);
        T17Button action = null;
        foreach (T17Button button in ((Component)page).GetComponentsInChildren<T17Button>(true)) if (button.name == "Action_edit-hud") { action = button; break; }
        Check(action != null && action.interactable, "real enabled action row directly after enable setting");
        RectTransform ar = action.transform as RectTransform;
        GraphicRaycaster caster = action.GetComponentInParent<GraphicRaycaster>();
        Canvas canvas = action.GetComponentInParent<Canvas>(); Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : caster.eventCamera;
        PointerEventData pointer = new PointerEventData(domain) { position = RectTransformUtility.WorldToScreenPoint(cam, ar.TransformPoint(ar.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); domain.RaycastAll(pointer, hits);
        GameObject first = null;
        foreach (RaycastResult hit in hits) { first = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (first != null) break; }
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "action\t" + pointer.position + "\tfirst=" + (first ? first.name : "none") + Environment.NewLine);
        Check(first == action.gameObject, "actual event-system raycast reaches HUD editor action");
        ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        yield return null;
        Check(IsEditing(editor) && !((Component)page).gameObject.activeSelf, "ClosePage hides ModSettings before acquiring native editor freeze");
        Check(TimeManager.IsPaused(TimeManager.PauseLayer.Main) && TimeManager.IsPaused(TimeManager.PauseLayer.UI)
            && TimeManager.IsPaused(TimeManager.PauseLayer.Camera), "native main/UI/camera layers frozen");
        foreach (PlayerControls player in UnityEngine.Object.FindObjectsOfType<PlayerControls>()) Check(player.IsSuppressed(), "native player input suppressed while editing");
        foreach (T17EventSystem system in UnityEngine.Object.FindObjectsOfType<T17EventSystem>()) Check(system.IsDisabled(), "native event-system input suppressed while editor owns native Canvas");
        yield return NativePanelScenario(editor, hud);
        yield return HeatEditorScenario(editor, hud, registry);
        int timerValue = UnityEngine.Object.FindObjectOfType<LevelTimerUIController>().Value;
        yield return new WaitForSecondsRealtime(1.2f);
        Check(UnityEngine.Object.FindObjectOfType<LevelTimerUIController>().Value == timerValue, "round timer frozen across real elapsed time");
        Call(shortcut, "Tick", true); Check(!((Component)page).gameObject.activeSelf, "ModSettings backquote ignored during HUD editing");
        object score = Target(registry, "Score"), timer = Target(registry, "Timer");
        foreach (object candidate in (IEnumerable)ReadField(registry, "_targets"))
        {
            Component component = (Component)ReadField(candidate, "Rect"); string path = component.name;
            for (Transform ancestor = component.transform.parent; ancestor; ancestor = ancestor.parent) path = ancestor.name + "/" + path;
            File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), ReadField(candidate, "Kind") + "-path\t" + path + "\n");
        }
        Check(score != null && timer != null, "native fixed score and timer registered with visible geometry");
        Rect before = TargetBounds(score), timerBefore = TargetBounds(timer);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-before.png")); yield return new WaitForEndOfFrame(); yield return null;
        harmony.Patch(AccessTools.Method(hud.GetType(), "OnGUI"), new HarmonyMethod(typeof(UnityRecipeTests), "InjectEditorEvent"), new HarmonyMethod(typeof(UnityRecipeTests), "RestoreEditorEvent"));
        harmony.Patch(AccessTools.PropertySetter(typeof(GUI), "matrix"), null, new HarmonyMethod(typeof(UnityRecipeTests), "EditorMatrixChanged"));
        Vector2 delta = new Vector2(Screen.width * .08f, -Screen.height * .12f);
        yield return GuiCategory(editor, 0);
        yield return GuiInput(EventType.MouseDown, before.center);
        Check(ReferenceEquals(ReadField(editor, "_selected"), score), "production overlay selects real scoreboard by displayed bounds");
        yield return GuiInput(EventType.MouseDrag, before.center + delta);
        yield return GuiInput(EventType.MouseUp, before.center + delta);
        yield return new WaitForEndOfFrame(); yield return null;
        Rect after = TargetBounds(score);
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "score-before\t" + before + "\nscore-after-drag\t" + after + "\n");
        Check((after.center - before.center - delta).magnitude < 3f, "drag produces exact measured screen XY shift without layout drift");
        Vector2 edge = new Vector2(after.xMax, after.center.y);
        yield return GuiInput(EventType.MouseDown, edge);
        yield return GuiInput(EventType.MouseDrag, edge + new Vector2(after.width * .2f, 0f));
        yield return GuiInput(EventType.MouseUp, edge + new Vector2(after.width * .2f, 0f));
        yield return new WaitForEndOfFrame(); yield return null;
        Rect stretched = TargetBounds(score);
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "score-after-edge\t" + stretched + "\n");
        Check(Mathf.Abs(stretched.width / after.width - 1.2f) < .03f && Mathf.Abs(stretched.xMin - after.xMin) < 3f, "real edge resize changes width with opposite edge fixed");
        Vector2 corner = new Vector2(stretched.xMax, stretched.yMin);
        yield return GuiInput(EventType.MouseDown, corner);
        yield return GuiInput(EventType.MouseDrag, corner + new Vector2(stretched.width * .1f, -stretched.height * .1f));
        yield return GuiInput(EventType.MouseUp, corner + new Vector2(stretched.width * .1f, -stretched.height * .1f));
        yield return new WaitForEndOfFrame(); yield return null;
        Rect scaled = TargetBounds(score);
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "score-after-corner\t" + scaled + "\n");
        Check(Mathf.Abs(scaled.width / stretched.width - scaled.height / stretched.height) < .03f, "real corner resize preserves displayed aspect ratio");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-selected.png")); yield return new WaitForEndOfFrame(); yield return null;
        Vector2 timerDelta = new Vector2(-Screen.width * .07f, -Screen.height * .15f);
        yield return GuiCategory(editor, 1);
        yield return GuiInput(EventType.MouseDown, timerBefore.center);
        Check(ReferenceEquals(ReadField(editor, "_selected"), timer), "production overlay selects real timer");
        yield return GuiInput(EventType.MouseDrag, timerBefore.center + timerDelta);
        yield return GuiInput(EventType.MouseUp, timerBefore.center + timerDelta);
        yield return null;
        Rect timerAfter = TargetBounds(timer);
        Check((timerAfter.center - timerBefore.center - timerDelta).magnitude < 3f, "timer moves by measured normalized screen delta");
        Vector2 timerTop = new Vector2(timerAfter.center.x, timerAfter.yMin);
        yield return GuiInput(EventType.MouseDown, timerTop);
        yield return GuiInput(EventType.MouseDrag, timerTop - new Vector2(0f, timerAfter.height * .2f));
        yield return GuiInput(EventType.MouseUp, timerTop - new Vector2(0f, timerAfter.height * .2f));
        yield return null;
        Rect timerSized = TargetBounds(timer);
        Check(Mathf.Abs(timerSized.height / timerAfter.height - 1.2f) < .03f && Mathf.Abs(timerSized.yMax - timerAfter.yMax) < 3f,
            "real timer height resize pins opposite edge");
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "timer-before\t" + timerBefore + "\ntimer-after-drag\t" + timerAfter + "\ntimer-after-edge\t" + timerSized + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-timer.png")); yield return new WaitForEndOfFrame(); yield return null;
        // Existing loaded stock prefabs: display fixture only, never an invented progress implementation.
        WorkableItem workable = null;
        foreach (WorkableItem item in Resources.FindObjectsOfTypeAll<WorkableItem>()) if (item.m_progressUIPrefab != null) { workable = item; break; }
        Check(workable != null, "loaded native chopping progress prefab available");
        GameObject a = GameUtils.InstantiateUIController(workable.m_progressUIPrefab.gameObject, "HoverIconCanvas");
        GameObject b = GameUtils.InstantiateUIController(workable.m_progressUIPrefab.gameObject, "HoverIconCanvas");
        ProgressUIController pa = a.GetComponent<ProgressUIController>(), pb = b.GetComponent<ProgressUIController>();
        pa.SetFollowTransform(null, new Vector3(.35f, .5f)); pb.SetFollowTransform(null, new Vector3(.55f, .5f));
        pa.AutoHide = pb.AutoHide = false; pa.SetProgress(.4f); pb.SetProgress(.7f);
        Vector2 paNative = pa.GetOffset(), pbNative = pb.GetOffset();
        Type kindType = hud.GetType().Assembly.GetType("OC2HudCustomization.HudTargetKind"); object chopping = Enum.Parse(kindType, "Chopping");
        Call(registry, "Observe", pa, pa, chopping, true); Call(registry, "Observe", pb, pb, chopping, true);
        yield return null; yield return new WaitForEndOfFrame(); yield return null;
        object chop = Target(registry, "Chopping"); Check(chop != null, "actual native progress displays registered for category test");
        a.SetActive(false); b.SetActive(false);
        yield return GuiPreview(editor, (int)chopping);
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "preview-state\tchecked=" + ((bool[])ReadField(registry, "Previews"))[(int)chopping]
            + "\thasInstance=" + Call(registry, "HasInstance", chopping) + "\tactive=" + a.activeSelf + "/" + b.activeSelf
            + "\tlease=" + (ReadField(chop, "_preview") != null) + "\tenabled=" + ReadField(registry, "Enabled") + "\n");
        Check(a.activeSelf && b.activeSelf, "floating panel preview reveals originally hidden native progress displays");
        ProgressBarUI previewBar = (ProgressBarUI)AccessTools.Field(typeof(ProgressUIController), "m_progressBar").GetValue(pa);
        Check(Mathf.Abs(previewBar.Value - .5f) < .001f, "preview uses half-full native display without changing gameplay progress");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-preview.png")); yield return new WaitForEndOfFrame(); yield return null;
        yield return GuiPreview(editor, (int)chopping);
        Check(!a.activeSelf && !b.activeSelf && Mathf.Abs(previewBar.Value - .4f) < .001f,
            "unchecking preview restores hidden root and original displayed progress");
        a.SetActive(true); b.SetActive(true); yield return null; yield return null;
        yield return GuiCategory(editor, (int)chopping);
        Rect chopBefore = TargetBounds(chop);
        yield return GuiInput(EventType.MouseDown, chopBefore.center);
        Check(ReferenceEquals(ReadField(editor, "_selected"), chop), "shader-offset-aware hit test selects chopping bar at actual displayed location");
        yield return GuiInput(EventType.MouseDrag, chopBefore.center + new Vector2(0f, -Screen.height * .1f));
        yield return GuiInput(EventType.MouseUp, chopBefore.center + new Vector2(0f, -Screen.height * .1f));
        yield return null;
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "chopping-offset-a\t" + paNative + "\t" + pa.GetOffset()
            + "\nchopping-offset-b\t" + pbNative + "\t" + pb.GetOffset() + "\n");
        Check(Mathf.Abs(pa.GetOffset().y - paNative.y - .1f) < .001f && Mathf.Abs(pb.GetOffset().y - pbNative.y - .1f) < .001f,
            "dragging one chopping bar applies same relative offset to both native instances");
        Check(pa.GetFollowTransform() == null && pb.GetFollowTransform() == null, "presentation changes preserve native follow target contract");
        Array values = (Array)ReadField(registry, "Values"); object choppingValue = values.GetValue((int)chopping);
        AccessTools.Field(choppingValue.GetType(), "Opacity").SetValue(choppingValue, .45f);
        yield return null;
        Check(Mathf.Abs(a.GetComponent<CanvasGroup>().alpha - .45f) < .001f && Mathf.Abs(b.GetComponent<CanvasGroup>().alpha - .45f) < .001f, "category opacity multiplies both real progress displays");
        File.AppendAllText(Path.Combine(output, "editor-geometry.tsv"), "chopping-before\t" + chopBefore + "\nchopping-after\t" + TargetBounds(chop) + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-progress.png")); yield return new WaitForEndOfFrame(); yield return null;
        GameObject c = GameUtils.InstantiateUIController(workable.m_progressUIPrefab.gameObject, "HoverIconCanvas");
        ProgressUIController pc = c.GetComponent<ProgressUIController>(); Vector2 pcNative = pc.GetOffset();
        pc.SetFollowTransform(null, new Vector3(.45f, .4f)); pc.AutoHide = false; pc.SetProgress(.6f);
        Call(registry, "Observe", pc, pc, chopping, true); yield return null; yield return null;
        Check(Mathf.Abs(pc.GetOffset().y - pcNative.y - .1f) < .001f && Mathf.Abs(c.GetComponent<CanvasGroup>().alpha - .45f) < .001f,
            "later native progress instance inherits global draft offset and opacity");
        yield return GuiCategory(editor, (int)chopping);
        AccessTools.Field(choppingValue.GetType(), "Opacity").SetValue(choppingValue, 0f);
        yield return null;
        Check(a.GetComponent<CanvasGroup>().alpha == 0f && (int)ReadField(editor, "_editingKind") == (int)chopping,
            "fully transparent category remains editable from the category selector");
        AccessTools.Field(choppingValue.GetType(), "Opacity").SetValue(choppingValue, .45f);
        yield return GuiPreview(editor, (int)chopping);
        a.SetActive(false); yield return GuiPreview(editor, (int)chopping);
        Check(a.activeSelf, "preview active before editor save");
        Call(editor, "End", true);
        Check(!a.activeSelf && !((bool[])ReadField(registry, "Previews"))[(int)chopping], "save exit restores hidden UI and clears session-only preview");
        a.SetActive(true);
        Check(!IsEditing(editor) && !TimeManager.IsPaused(TimeManager.PauseLayer.Main), "save exits editor and releases owned native pause");
        Call(shortcut, "Tick", true); Check(!((Component)page).gameObject.activeSelf, "exit-frame backquote cannot reopen ModSettings");
        Check(Mathf.Abs((float)hud.Config["HudEditor.Score", "X"].BoxedValue) > .01f
            && Mathf.Abs((float)hud.Config["HudEditor.Chopping", "Y"].BoxedValue - .1f) < .001f, "actual hidden ConfigEntry values saved from draft");
        string savedConfig = File.ReadAllText(hud.Config.ConfigFilePath);
        yield return new WaitForSecondsRealtime(.3f);
        Call(editor, "Begin"); Check(IsEditing(editor), "second editor session can reacquire resources");
        values = (Array)ReadField(registry, "Values"); object draft = values.GetValue(0);
        AccessTools.Field(draft.GetType(), "X").SetValue(draft, -.5f);
        yield return GuiCategory(editor, 0);
        Check((int)ReadField(editor, "_editingKind") == 0, "category selector can recover offscreen geometry without a target click");
        Call(editor, "End", false);
        Check(savedConfig == File.ReadAllText(hud.Config.ConfigFilePath), "cancelled session preserves saved configuration bytes");
        TimeManager time = GameUtils.RequestManager<TimeManager>(); object other = new object(); time.SetPaused(TimeManager.PauseLayer.Main, true, other);
        Call(editor, "Begin"); Call(editor, "End", false);
        Check(TimeManager.IsPaused(TimeManager.PauseLayer.Main), "preexisting unrelated pause survives editor exit");
        time.SetPaused(TimeManager.PauseLayer.Main, false, other);
        Call(editor, "Begin");
        bool allowPause = (bool)AccessTools.Method(editor.GetType(), "FilterPause").Invoke(null, null);
        Check(!allowPause && IsEditing(editor), "native pause shortcut suppressed during editor input");
        time.SetPaused(TimeManager.PauseLayer.System, true, other);
        allowPause = (bool)AccessTools.Method(editor.GetType(), "FilterPause").Invoke(null, null);
        Check(allowPause && !IsEditing(editor) && TimeManager.IsPaused(TimeManager.PauseLayer.System), "system pause cancels draft and preserves original native pause path");
        time.SetPaused(TimeManager.PauseLayer.System, false, other);
        yield return null;
        Call(editor, "Begin"); Check(IsEditing(editor), "editor resumes after system pause ownership released");
        hud.Config["General", "Enabled"].BoxedValue = false; yield return null; yield return null;
        Check(!IsEditing(editor), "disabling master during editing cancels draft and releases freeze");
        Check((TargetBounds(score).center - before.center).magnitude < 3f, "disabling master restores scoreboard geometry");
        Check((TargetBounds(timer).center - timerBefore.center).magnitude < 3f, "disabling master restores timer geometry");
        Check(pa.GetOffset() == paNative && pb.GetOffset() == pbNative, "disabling master restores both native progress offsets");
        Check(a.GetComponent<CanvasGroup>() == null && b.GetComponent<CanvasGroup>() == null, "disabling destroys only owned alpha components");
        UnityEngine.Object.Destroy(a); UnityEngine.Object.Destroy(b); UnityEngine.Object.Destroy(c);
        hud.Config["General", "Enabled"].BoxedValue = true;
        File.WriteAllText(Path.Combine(output, "loaded.txt"), hud.Info.Metadata.GUID + " " + hud.Info.Metadata.Version + " " + Hash(hud.GetType().Assembly.Location) + "\nScreen=" + Screen.width + "x" + Screen.height);
    }
}
