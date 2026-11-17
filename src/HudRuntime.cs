using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed class HudRuntime : IDisposable
    {
        private readonly HudLog _log;
        private readonly Harmony _recipeHarmony = new Harmony(PluginInfo.Guid + ".recipes");
        private readonly Harmony _contentsHarmony = new Harmony(PluginInfo.Guid + ".contents");
        internal RecipeAdapter Recipes;
        internal ContentsAdapter Contents;
        internal HudOptions Options;

        internal HudRuntime(HudOptions options, HudLog log) { Options = options; _log = log; }

        internal void Start()
        {
            try
            {
                Recipes = new RecipeAdapter(new RecipeAccess(), _log);
                Install(_recipeHarmony, new PatchSpec[] {
                    new PatchSpec(typeof(RecipeFlowGUI), "AddElement", "RecipeAdded", false, typeof(OrderDefinitionNode), typeof(float), typeof(VoidGeneric<RecipeFlowGUI.ElementToken>)),
                    new PatchSpec(typeof(RecipeFlowGUI), "LayoutWidgets", "RecipeLayout", false),
                    new PatchSpec(typeof(RecipeFlowGUI), "OnDestroy", "FlowDestroyed", true),
                    new PatchSpec(typeof(RecipeWidgetUIController), "OnRefreshSubObjectProperties", "RecipeRefreshed", false, typeof(GameObject)),
                    new PatchSpec(typeof(RecipeWidgetUIController), "OnDestroy", "RecipeDestroyed", true)
                });
            }
            catch (Exception exception) { FailRecipes("Recipe contract/install", exception); }
            try
            {
                Contents = new ContentsAdapter(new ContentsAccess(), _log);
                Install(_contentsHarmony, new PatchSpec[] {
                    new PatchSpec(typeof(ClientIngredientContentGUI), "OnOrderChanged", "ContentsChanged", false, typeof(AssembledDefinitionNode)),
                    new PatchSpec(typeof(ClientIngredientContentGUI), "OnDisable", "ContentsDisabled", false),
                    new PatchSpec(typeof(ClientIngredientContentGUI), "OnDestroy", "ContentsDestroyed", true),
                    new PatchSpec(typeof(IngredientContentsUIContainer), "RefreshIcons", "ContentsRefreshed", false, typeof(GameObject)),
                    new PatchSpec(typeof(UISubElementContainer), "RefreshSubElements", "BeforeContentsRefresh", true)
                });
            }
            catch (Exception exception) { FailContents("Contents contract/install", exception); }
            Discover();
            _log.Info(PluginInfo.Name + " " + PluginInfo.Version + "; recipes=" + (Recipes != null) + "; contents=" + (Contents != null) + ". All settings are native BepInEx ConfigEntry values.");
        }

        private void Install(Harmony harmony, PatchSpec[] specs)
        {
            // Resolve the complete dependent group before changing any target.
            MethodInfo[] originals = new MethodInfo[specs.Length];
            HarmonyMethod[] patches = new HarmonyMethod[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                originals[i] = Required.Method(specs[i].Owner, specs[i].Target, specs[i].Parameters);
                patches[i] = new HarmonyMethod(Required.Method(typeof(HudPatches), specs[i].Patch, new Type[] { specs[i].Owner }));
                patches[i].priority = Priority.Last;
            }
            for (int i = 0; i < specs.Length; i++)
            {
                harmony.Patch(originals[i], specs[i].Prefix ? patches[i] : null, specs[i].Prefix ? null : patches[i]);
                _log.Info("Patch " + originals[i].DeclaringType.FullName + "." + originals[i].Name + " " + (specs[i].Prefix ? "Prefix" : "Postfix") + " priority=Last");
            }
        }

        private void Discover()
        {
            if (Recipes != null)
                try
                {
                    RecipeFlowGUI[] flows = UnityEngine.Object.FindObjectsOfType<RecipeFlowGUI>();
                    for (int i = 0; i < flows.Length; i++) Recipes.Observe(flows[i], false, false);
                }
                catch (Exception exception) { FailRecipes("Recipe bounded discovery", exception); }
            if (Contents != null)
                try
                {
                    ClientIngredientContentGUI[] owners = UnityEngine.Object.FindObjectsOfType<ClientIngredientContentGUI>();
                    for (int i = 0; i < owners.Length; i++) Contents.Observe(owners[i], Options);
                }
                catch (Exception exception) { FailContents("Contents bounded discovery", exception); }
        }

        internal void SetOptions(HudOptions options)
        {
            Options = options;
            if (!options.Enabled) { RestoreRecipes(); RestoreContents(); }
            else if (Contents != null) Contents.MarkDirty();
        }

        internal void Tick()
        {
            if (Recipes != null)
                try { Recipes.Tick(Options); }
                catch (Exception exception) { FailRecipes("Recipe LateUpdate presentation", exception); }
            if (Contents != null)
                try { Contents.Tick(Options); }
                catch (Exception exception) { FailContents("Contents LateUpdate presentation", exception); }
        }

        internal void FailRecipes(string target, Exception exception)
        {
            _log.ErrorOnce(target, exception); RestoreRecipes(); Recipes = null;
            Unpatch(_recipeHarmony);
        }

        internal void FailContents(string target, Exception exception)
        {
            _log.ErrorOnce(target, exception); RestoreContents(); Contents = null;
            Unpatch(_contentsHarmony);
        }

        private void RestoreRecipes()
        {
            try { if (Recipes != null) Recipes.Restore(); }
            catch (Exception exception) { _log.ErrorOnce("Recipe restore", exception); }
        }

        private void RestoreContents()
        {
            try { if (Contents != null) Contents.Restore(); }
            catch (Exception exception) { _log.ErrorOnce("Contents restore", exception); }
        }

        public void Dispose()
        {
            RestoreRecipes(); RestoreContents();
            Unpatch(_recipeHarmony); Unpatch(_contentsHarmony);
            Recipes = null; Contents = null;
        }

        private void Unpatch(Harmony harmony)
        {
            try { harmony.UnpatchSelf(); }
            catch (Exception exception) { _log.ErrorOnce(harmony.Id + " unpatch", exception); }
        }

        private sealed class PatchSpec
        {
            internal readonly Type Owner;
            internal readonly string Target, Patch;
            internal readonly bool Prefix;
            internal readonly Type[] Parameters;
            internal PatchSpec(Type owner, string target, string patch, bool prefix, params Type[] parameters)
            { Owner = owner; Target = target; Patch = patch; Prefix = prefix; Parameters = parameters; }
        }
    }
}
