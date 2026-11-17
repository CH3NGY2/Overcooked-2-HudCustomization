using System;

namespace OC2HudCustomization
{
    internal static class HudPatches
    {
        private static void RecipeAdded(RecipeFlowGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Recipes == null) return;
            try { r.Recipes.Observe(__instance, false, r.Options.Enabled && (r.Options.RecipesPerRow > 0 || !UnityEngine.Mathf.Approximately(r.Options.RecipeScale, 1f))); }
            catch (Exception e) { r.FailRecipes("RecipeFlowGUI.AddElement Postfix", e); }
        }
        private static void RecipeLayout(RecipeFlowGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Recipes == null) return;
            try { r.Recipes.Observe(__instance, true, false); }
            catch (Exception e) { r.FailRecipes("RecipeFlowGUI.LayoutWidgets Postfix", e); }
        }
        private static void FlowDestroyed(RecipeFlowGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Recipes == null) return;
            try { r.Recipes.Forget(__instance); }
            catch (Exception e) { r.FailRecipes("RecipeFlowGUI.OnDestroy Prefix", e); }
        }
        private static void RecipeRefreshed(RecipeWidgetUIController __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Recipes == null) return;
            try { r.Recipes.Refresh(__instance, r.Options); }
            catch (Exception e) { r.FailRecipes("RecipeWidgetUIController.OnRefreshSubObjectProperties Postfix", e); }
        }
        private static void RecipeDestroyed(RecipeWidgetUIController __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Recipes == null) return;
            try { r.Recipes.Forget(__instance); }
            catch (Exception e) { r.FailRecipes("RecipeWidgetUIController.OnDestroy Prefix", e); }
        }
        private static void ContentsChanged(ClientIngredientContentGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Contents == null) return;
            try { r.Contents.Observe(__instance, r.Options); }
            catch (Exception e) { r.FailContents("ClientIngredientContentGUI.OnOrderChanged Postfix", e); }
        }
        private static void ContentsDisabled(ClientIngredientContentGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Contents == null) return;
            try { r.Contents.Suspend(__instance, false); }
            catch (Exception e) { r.FailContents("ClientIngredientContentGUI.OnDisable Postfix", e); }
        }
        private static void ContentsDestroyed(ClientIngredientContentGUI __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Contents == null) return;
            try { r.Contents.Suspend(__instance, true); }
            catch (Exception e) { r.FailContents("ClientIngredientContentGUI.OnDestroy Prefix", e); }
        }
        private static void ContentsRefreshed(IngredientContentsUIContainer __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Contents == null) return;
            try { r.Contents.Refresh(__instance, r.Options); }
            catch (Exception e) { r.FailContents("IngredientContentsUIContainer.RefreshIcons Postfix", e); }
        }
        private static void BeforeContentsRefresh(UISubElementContainer __instance)
        {
            HudRuntime r = Plugin.Runtime;
            if (r == null || r.Contents == null) return;
            try { r.Contents.BeforeRefresh(__instance); }
            catch (Exception e) { r.FailContents("UISubElementContainer.RefreshSubElements Prefix", e); }
        }
    }
}
