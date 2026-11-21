using BepInEx.Configuration;
using OC2ModSettings.SDK;

namespace OC2HudCustomization
{
    internal sealed partial class HudSettings
    {
        internal const int MaxRecipeRows = 20;
        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<float> RecipeScale;
        internal readonly ConfigEntry<int> RecipesPerRow, RecipeMaxRows;
        internal readonly ConfigEntry<float> RecipeRowSpacing;
        internal readonly ConfigEntry<float> ContentsScale;
        internal readonly ConfigEntry<int> ContentsPerRow;
        internal readonly ConfigEntry<float> RecipeOpacity;
        internal readonly ConfigEntry<float> EmptySlotOpacity;
        internal readonly ConfigEntry<float> IngredientOpacity;
        internal readonly ConfigEntry<float> ContentsX, ContentsY, ContentsWidth, ContentsHeight;

        internal HudSettings(ConfigFile config)
        {
            Enabled = Bind(config, "General", "Enabled", true, SettingsText.Enabled, SettingsText.EnabledDescription,
                SettingsText.General, 400, 100, null);
            RecipeScale = Bind(config, "Recipes", "RecipeScale", 1f, SettingsText.RecipeScale, SettingsText.RecipeScaleDescription,
                SettingsText.Recipes, 300, 100, new AcceptableValueRange<float>(0.5f, 2f));
            RecipesPerRow = Bind(config, "Recipes", "RecipesPerRow", 0, SettingsText.RecipesPerRow, SettingsText.RecipesPerRowDescription,
                SettingsText.Recipes, 300, 90, new AcceptableValueRange<int>(0, 20));
            RecipeMaxRows = Bind(config, "Recipes", "RecipeMaxRows", 0, SettingsText.RecipeMaxRows, SettingsText.RecipeMaxRowsDescription,
                SettingsText.Recipes, 300, 85, new AcceptableValueRange<int>(0, MaxRecipeRows));
            RecipeRowSpacing = Bind(config, "Recipes", "RecipeRowSpacing", 8f, SettingsText.RecipeRowSpacing, SettingsText.RecipeRowSpacingDescription,
                SettingsText.Recipes, 300, 80, new AcceptableValueRange<float>(0f, 100f));
            ContentsScale = Bind(config, "Contents", "ContentsScale", 1f, SettingsText.ContentsScale, SettingsText.ContentsScaleDescription,
                SettingsText.Contents, 200, 100, new AcceptableValueRange<float>(0.5f, 2f));
            ContentsPerRow = Bind(config, "Contents", "ContentsPerRow", 0, SettingsText.ContentsPerRow, SettingsText.ContentsPerRowDescription,
                SettingsText.Contents, 200, 90, new AcceptableValueRange<int>(0, 20));
            RecipeOpacity = Bind(config, "Opacity", "RecipeOpacity", 100f, SettingsText.RecipeOpacity, SettingsText.RecipeOpacityDescription,
                SettingsText.Opacity, 100, 100, new AcceptableValueRange<float>(0f, 100f));
            EmptySlotOpacity = Bind(config, "Opacity", "EmptySlotOpacity", 100f, SettingsText.EmptySlotOpacity, SettingsText.EmptySlotOpacityDescription,
                SettingsText.Opacity, 100, 90, new AcceptableValueRange<float>(0f, 100f));
            IngredientOpacity = Bind(config, "Opacity", "IngredientOpacity", 100f, SettingsText.IngredientOpacity, SettingsText.IngredientOpacityDescription,
                SettingsText.Opacity, 100, 80, new AcceptableValueRange<float>(0f, 100f));
            ContentsX = BindHidden(config, "X", 0f, -1f, 1f);
            ContentsY = BindHidden(config, "Y", 0f, -1f, 1f);
            ContentsWidth = BindHidden(config, "Width", 1f, .25f, 4f);
            ContentsHeight = BindHidden(config, "Height", 1f, .25f, 4f);
        }

        private static ConfigEntry<float> BindHidden(ConfigFile config, string key, float value, float min, float max)
        { return config.Bind("HudEditor.Contents", key, value, new ConfigDescription("Shared contents geometry; X/Y are screen fractions (Y up).",
            new AcceptableValueRange<float>(min, max), new ModSettingsAttributes { Browsable = false })); }

        internal HudOptions Snapshot()
        {
            return new HudOptions(Enabled.Value, Finite(RecipeScale.Value, 1f), RecipesPerRow.Value, RecipeMaxRows.Value, Finite(RecipeRowSpacing.Value, 8f),
                Finite(ContentsScale.Value, 1f), ContentsPerRow.Value, Finite(RecipeOpacity.Value, 100f) / 100f,
                Finite(EmptySlotOpacity.Value, 100f) / 100f, Finite(IngredientOpacity.Value, 100f) / 100f,
                Finite(ContentsX.Value, 0f), Finite(ContentsY.Value, 0f), Finite(ContentsWidth.Value, 1f), Finite(ContentsHeight.Value, 1f));
        }

        internal void Apply(HudOptions value)
        {
            RecipeScale.Value = value.RecipeScale; RecipesPerRow.Value = value.RecipesPerRow; RecipeMaxRows.Value = value.RecipeMaxRows;
            RecipeRowSpacing.Value = value.RecipeRowSpacing; ContentsScale.Value = value.ContentsScale;
            ContentsPerRow.Value = value.ContentsPerRow; RecipeOpacity.Value = value.RecipeOpacity * 100f;
            EmptySlotOpacity.Value = value.EmptySlotOpacity * 100f; IngredientOpacity.Value = value.IngredientOpacity * 100f;
            ContentsX.Value = value.ContentsX; ContentsY.Value = value.ContentsY;
            ContentsWidth.Value = value.ContentsWidth; ContentsHeight.Value = value.ContentsHeight;
        }

        // Defense after BepInEx range validation; never send non-finite values into Unity.
        private static float Finite(float value, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value; }
    }

    internal sealed class HudOptions
    {
        internal readonly bool Enabled;
        internal readonly float RecipeScale, RecipeRowSpacing, ContentsScale;
        internal readonly int RecipesPerRow, RecipeMaxRows, ContentsPerRow;
        internal readonly float RecipeOpacity, EmptySlotOpacity, IngredientOpacity;
        internal readonly float ContentsX, ContentsY, ContentsWidth, ContentsHeight;

        internal bool UsesCustomRecipeLayout { get { return RecipesPerRow > 0 || !UnityEngine.Mathf.Approximately(RecipeScale, 1f); } }

        internal bool Same(HudOptions other)
        {
            return other != null && Enabled == other.Enabled && RecipeScale == other.RecipeScale
                && RecipesPerRow == other.RecipesPerRow && RecipeMaxRows == other.RecipeMaxRows && RecipeRowSpacing == other.RecipeRowSpacing
                && ContentsScale == other.ContentsScale && ContentsPerRow == other.ContentsPerRow
                && RecipeOpacity == other.RecipeOpacity && EmptySlotOpacity == other.EmptySlotOpacity
                && IngredientOpacity == other.IngredientOpacity && ContentsX == other.ContentsX && ContentsY == other.ContentsY
                && ContentsWidth == other.ContentsWidth && ContentsHeight == other.ContentsHeight;
        }

        internal HudOptions(bool enabled, float recipeScale, int recipesPerRow, int recipeMaxRows, float recipeRowSpacing,
            float contentsScale, int contentsPerRow, float recipeOpacity, float emptySlotOpacity, float ingredientOpacity,
            float contentsX, float contentsY, float contentsWidth, float contentsHeight)
        {
            Enabled = enabled;
            RecipeScale = recipeScale;
            RecipesPerRow = recipesPerRow;
            RecipeMaxRows = recipeMaxRows;
            RecipeRowSpacing = recipeRowSpacing;
            ContentsScale = contentsScale;
            ContentsPerRow = contentsPerRow;
            RecipeOpacity = recipeOpacity;
            EmptySlotOpacity = emptySlotOpacity;
            IngredientOpacity = ingredientOpacity;
            ContentsX = contentsX; ContentsY = contentsY; ContentsWidth = contentsWidth; ContentsHeight = contentsHeight;
        }
    }
}
