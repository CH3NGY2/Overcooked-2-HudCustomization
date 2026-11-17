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
    private IEnumerator RecipeEditorScenario(object editor, BaseUnityPlugin hud)
    {
        Phase("recipe editor display clock independent from frozen gameplay timers");
        Call(editor, "End", false);
        RecipeFlowGUI source = UnityEngine.Object.FindObjectOfType<RecipeFlowGUI>();
        RectTransform native = (RectTransform)source.transform;
        GameObject fixture = new GameObject("HudRecipeEditorRegression", typeof(RectTransform)); fixture.SetActive(false);
        RectTransform area = (RectTransform)fixture.transform; area.SetParent(native.parent, false);
        area.anchorMin = native.anchorMin; area.anchorMax = native.anchorMax; area.pivot = native.pivot;
        area.sizeDelta = native.sizeDelta; area.anchoredPosition = native.anchoredPosition; area.localScale = native.localScale;
        RecipeFlowGUI flow = fixture.AddComponent<RecipeFlowGUI>();
        AccessTools.Field(typeof(RecipeFlowGUI), "m_recipeWidgetPrefab").SetValue(flow,
            AccessTools.Field(typeof(RecipeFlowGUI), "m_recipeWidgetPrefab").GetValue(source));
        OrderDefinitionNode recipe = null;
        foreach (OrderDefinitionNode candidate in Resources.FindObjectsOfTypeAll<OrderDefinitionNode>())
            if (HasCookware(candidate) && (recipe == null || candidate.name.Contains("Burger"))) recipe = candidate;
        Check(recipe != null, "editor uses real native recipe with cookware");
        fixture.SetActive(true);
        List<RecipeWidgetUIController> cards = new List<RecipeWidgetUIController>();
        for (int i = 0; i < 4; i++) cards.Add(flow.GetData(flow.AddElement(recipe, 180f, delegate(RecipeFlowGUI.ElementToken token) { })).m_widget);
        // Generate the native Animator before acquiring the editor's pause, exactly
        // like a card already on the player's HUD when they open the editor.
        yield return new WaitForEndOfFrame(); yield return null;
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_8");
        Animator animator = cards[3].GetComponentInChildren<Animator>();
        Check(animator != null, "native recipe animator exists before pause regression");
        int round = UnityEngine.Object.FindObjectOfType<LevelTimerUIController>().Value;
        RecipeWidgetUIController[] realOrders = source.GetComponentsInChildren<RecipeWidgetUIController>();
        float[] remaining = new float[realOrders.Length];
        for (int i = 0; i < realOrders.Length; i++) remaining[i] = realOrders[i].GetTimePropRemaining();
        yield return new WaitForSecondsRealtime(2.5f);
        AnimatorStateInfo animation = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Override"));
        float progress = animation.normalizedTime;
        File.AppendAllText(Path.Combine(output, "editor-recipe-geometry.tsv"), "native-animation\tprogress=" + progress + "\tspeed=" + animator.speed + "\tmode=" + animator.updateMode + "\n");
        // Keep the timer assertion ahead of geometry: moving the whole game is not a fix.
        Check(UnityEngine.Object.FindObjectOfType<LevelTimerUIController>().Value == round
            && Mathf.Approximately(cards[0].GetTimePropRemaining(), 1f), "round and order timers remain frozen during recipe preview");
        Check((progress >= 1f || !animation.IsName("Override.RecipeTransitionIn")) && BranchOpacity(cards[3]) >= .99f,
            "native recipe unfolds while editor freezes gameplay timers");
        GameObject root = NativeRoot(editor);
        Slider scale = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_0/Slider").GetComponent<Slider>();
        InputField columns = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_1/Value").GetComponent<InputField>();
        Slider spacing = root.transform.Find("HudEditorPanel/ParameterViewport/Parameters/Parameter_2/Slider").GetComponent<Slider>();
        scale.value = .5f; columns.text = "2"; spacing.value = 22;
        yield return new WaitForSecondsRealtime(1f);
        yield return new WaitForEndOfFrame();
        Rect first = HeadBounds(cards[0], area), third = HeadBounds(cards[2], area);
        Rect upper = Bounds(cards[0], area), lower = Bounds(cards[2], area);
        File.AppendAllText(Path.Combine(output, "editor-recipe-geometry.tsv"), "two-columns\tfirst=" + first + "\tthird=" + third + "\tupper=" + upper + "\tlower=" + lower + "\n");
        Check(first.yMax - third.yMax > first.height && Mathf.Abs(upper.yMin - lower.yMax - 22f) < 2f,
            "changing columns while paused recalculates row height including cookware");
        columns.text = "1"; scale.value = .65f; spacing.value = 35;
        yield return new WaitForSecondsRealtime(1f);
        yield return new WaitForEndOfFrame();
        first = HeadBounds(cards[0], area); Rect second = HeadBounds(cards[1], area); upper = Bounds(cards[0], area); lower = Bounds(cards[1], area);
        File.AppendAllText(Path.Combine(output, "editor-recipe-geometry.tsv"), "one-column\tfirst=" + first + "\tsecond=" + second + "\tupper=" + upper + "\tlower=" + lower + "\n");
        Check(Mathf.Abs(upper.yMin - lower.yMax - 35f) < 2f && Mathf.Abs(first.xMin - second.xMin) < 2f,
            "repeated scale and spacing edits converge to new rows without unpausing");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "editor-recipe-live-layout.png"));
        yield return new WaitForEndOfFrame(); yield return null;
        Check(UnityEngine.Object.FindObjectOfType<LevelTimerUIController>().Value == round
            && Mathf.Approximately(cards[3].GetTimePropRemaining(), 1f), "timers stay frozen after repeated live layout edits");
        for (int i = 0; i < realOrders.Length; i++) Check(realOrders[i]
            && Mathf.Approximately(realOrders[i].GetTimePropRemaining(), remaining[i]), "real gameplay order lifetime unchanged during live editor layout " + i);
        TimeManager time = GameUtils.RequestManager<TimeManager>(); object other = new object();
        time.SetPaused(TimeManager.PauseLayer.Main, true, other); yield return null; yield return null;
        Check(!(bool)AccessTools.Property(editor.GetType(), "AllowsRecipePreview").GetValue(editor, null), "another pause owner suspends the recipe display clock");
        Call(editor, "End", false);
        File.AppendAllText(Path.Combine(output, "editor-recipe-geometry.tsv"), "foreign-pause-exit\tlayer=" + animator.gameObject.layer
            + "\tdelta=" + TimeManager.GetDeltaTime(animator.gameObject) + "\tspeed=" + animator.speed + "\tmode=" + animator.updateMode + "\n");
        Check(TimeManager.IsPaused(TimeManager.PauseLayer.Main) && animator.speed == 0f,
            "leaving editor preserves another menu's pause and keeps native recipe animator frozen");
        time.SetPaused(TimeManager.PauseLayer.Main, false, other);
        UnityEngine.Object.Destroy(fixture); yield return null;
        Call(editor, "Begin"); yield return NativeClick(editor, "Category_8");
    }
}
