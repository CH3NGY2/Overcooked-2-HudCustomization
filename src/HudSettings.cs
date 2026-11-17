using BepInEx.Configuration;

namespace OC2HudCustomization
{
    internal sealed partial class HudSettings
    {
        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<float> RecipeScale;
        internal readonly ConfigEntry<int> RecipesPerRow;
        internal readonly ConfigEntry<float> RecipeRowSpacing;
        internal readonly ConfigEntry<float> ContentsScale;
        internal readonly ConfigEntry<int> ContentsPerRow;
        internal readonly ConfigEntry<float> RecipeOpacity;
        internal readonly ConfigEntry<float> EmptySlotOpacity;
        internal readonly ConfigEntry<float> IngredientOpacity;

        internal HudSettings(ConfigFile config)
        {
            Enabled = Bind(config, "General", "Enabled", true, SettingsText.Enabled, SettingsText.EnabledDescription,
                SettingsText.General, 400, 100, null);
            RecipeScale = Bind(config, "Recipes", "RecipeScale", 1f, SettingsText.RecipeScale, SettingsText.RecipeScaleDescription,
                SettingsText.Recipes, 300, 100, new AcceptableValueRange<float>(0.5f, 2f));
            RecipesPerRow = Bind(config, "Recipes", "RecipesPerRow", 0, SettingsText.RecipesPerRow, SettingsText.RecipesPerRowDescription,
                SettingsText.Recipes, 300, 90, new AcceptableValueRange<int>(0, 20));
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
        }

        internal HudOptions Snapshot()
        {
            return new HudOptions(Enabled.Value, Finite(RecipeScale.Value, 1f), RecipesPerRow.Value, Finite(RecipeRowSpacing.Value, 8f),
                Finite(ContentsScale.Value, 1f), ContentsPerRow.Value, Finite(RecipeOpacity.Value, 100f) / 100f,
                Finite(EmptySlotOpacity.Value, 100f) / 100f, Finite(IngredientOpacity.Value, 100f) / 100f);
        }

        internal void Apply(HudOptions value)
        {
            RecipeScale.Value = value.RecipeScale; RecipesPerRow.Value = value.RecipesPerRow;
            RecipeRowSpacing.Value = value.RecipeRowSpacing; ContentsScale.Value = value.ContentsScale;
            ContentsPerRow.Value = value.ContentsPerRow; RecipeOpacity.Value = value.RecipeOpacity * 100f;
            EmptySlotOpacity.Value = value.EmptySlotOpacity * 100f; IngredientOpacity.Value = value.IngredientOpacity * 100f;
        }

        // Defense after BepInEx range validation; never send non-finite values into Unity.
        private static float Finite(float value, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value; }
    }

    internal sealed class HudOptions
    {
        internal readonly bool Enabled;
        internal readonly float RecipeScale, RecipeRowSpacing, ContentsScale;
        internal readonly int RecipesPerRow, ContentsPerRow;
        internal readonly float RecipeOpacity, EmptySlotOpacity, IngredientOpacity;

        internal bool Same(HudOptions other)
        {
            return other != null && Enabled == other.Enabled && RecipeScale == other.RecipeScale
                && RecipesPerRow == other.RecipesPerRow && RecipeRowSpacing == other.RecipeRowSpacing
                && ContentsScale == other.ContentsScale && ContentsPerRow == other.ContentsPerRow
                && RecipeOpacity == other.RecipeOpacity && EmptySlotOpacity == other.EmptySlotOpacity
                && IngredientOpacity == other.IngredientOpacity;
        }

        internal HudOptions(bool enabled, float recipeScale, int recipesPerRow, float recipeRowSpacing,
            float contentsScale, int contentsPerRow, float recipeOpacity, float emptySlotOpacity, float ingredientOpacity)
        {
            Enabled = enabled;
            RecipeScale = recipeScale;
            RecipesPerRow = recipesPerRow;
            RecipeRowSpacing = recipeRowSpacing;
            ContentsScale = contentsScale;
            ContentsPerRow = contentsPerRow;
            RecipeOpacity = recipeOpacity;
            EmptySlotOpacity = emptySlotOpacity;
            IngredientOpacity = ingredientOpacity;
        }
    }
}
