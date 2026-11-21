using System.Globalization;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed partial class HudEditor
    {
        internal bool CategoryAvailable(int kind)
        {
            if (kind < 0 || kind >= 10) return false;
            if (kind < 8) return Targets.Available((HudTargetKind)kind);
            HudRuntime runtime = Plugin.Runtime;
            return runtime != null && (kind == 8 ? runtime.Recipes != null && runtime.Recipes.HasInstance
                : runtime.Contents != null && runtime.Contents.HasInstance);
        }
        internal string CategoryName(int kind)
        { return Text(kind < 8 ? EditorText.Kinds[kind] : kind == 8 ? SettingsText.Recipes : SettingsText.Contents); }
        internal string PreviewStatus
        {
            get
            {
                if (_editingKind < 0) return Text(EditorText.SelectCategoryText);
                if (!CategoryAvailable(_editingKind)) return Text(EditorText.NoInstance);
                if (_editingKind == 9) return "";
                if (_editingKind >= 8) return Text(EditorText.LiveLayout);
                return Text(Targets.HasInstance((HudTargetKind)_editingKind) ? EditorText.CategoryActive : EditorText.NoInstance);
            }
        }
        internal int NumberCount { get { return _editingKind < 0 ? 0 : _editingKind < 8 ? 6 : _editingKind == 8 ? 5 : 8; } }
        internal string NumberLabel(int index)
        {
            if (_editingKind < 8)
            {
                if (index == 0) return "X (%)"; if (index == 1) return "Y (%)";
                return Text(index == 2 ? EditorText.Scale : index == 3 ? EditorText.Width : index == 4 ? EditorText.Height : EditorText.Opacity);
            }
            if (_editingKind == 8)
                return Text(index == 0 ? SettingsText.RecipeScale : index == 1 ? SettingsText.RecipesPerRow
                    : index == 2 ? SettingsText.RecipeMaxRows : index == 3 ? SettingsText.RecipeRowSpacing : SettingsText.RecipeOpacity);
            if (index == 0) return "X (%)"; if (index == 1) return "Y (%)";
            return Text(index == 2 ? EditorText.Scale : index == 3 ? EditorText.Width : index == 4 ? EditorText.Height
                : index == 5 ? SettingsText.ContentsPerRow : index == 6 ? EditorText.IngredientOpacity : EditorText.EmptySlotOpacity);
        }
        internal void NumberRange(int index, out float min, out float max, out bool integer)
        {
            integer = (_editingKind == 8 && (index == 1 || index == 2)) || (_editingKind == 9 && index == 5);
            if (_editingKind < 8 || _editingKind == 9)
            {
                min = index < 2 ? -100 : index == 2 ? .2f : index < 5 ? .25f : 0;
                max = index < 2 ? 100 : index == 2 ? 3 : index < 5 ? 4 : 100;
                if (_editingKind == 9)
                {
                    if (index == 2) { min = .5f; max = 2f; }
                    if (index == 5) max = 20;
                }
            }
            else { min = index == 0 ? .5f : 0; max = index == 0 ? 2 : index == 1 ? 20 : index == 2 ? HudSettings.MaxRecipeRows : 100; }
        }
        internal float GetNumber(int index)
        {
            if (_editingKind < 0) return 0;
            if (_editingKind < 8)
            {
                HudGeometry value = Targets.Values[_editingKind];
                return index == 0 ? value.X * 100 : index == 1 ? value.Y * 100 : index == 2 ? value.Scale
                    : index == 3 ? value.Width : index == 4 ? value.Height : value.Opacity * 100;
            }
            HudOptions o = DraftOptions;
            if (_editingKind == 8) return index == 0 ? o.RecipeScale : index == 1 ? o.RecipesPerRow : index == 2 ? o.RecipeMaxRows
                : index == 3 ? o.RecipeRowSpacing : o.RecipeOpacity * 100;
            return index == 0 ? o.ContentsX * 100 : index == 1 ? o.ContentsY * 100 : index == 2 ? o.ContentsScale
                : index == 3 ? o.ContentsWidth : index == 4 ? o.ContentsHeight : index == 5 ? o.ContentsPerRow
                : (index == 6 ? o.IngredientOpacity : o.EmptySlotOpacity) * 100;
        }
        internal void SetNumber(int index, float value)
        {
            if (_editingKind < 0 || index < 0 || index >= NumberCount) return;
            float min, max; bool integer; NumberRange(index, out min, out max, out integer);
            if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max) return;
            if (integer) value = Mathf.Round(value);
            if (_editingKind < 8)
            {
                HudGeometry g = Targets.Values[_editingKind];
                if (index == 0) g.X = value / 100; else if (index == 1) g.Y = value / 100;
                else if (index == 2) g.Scale = value; else if (index == 3) g.Width = value;
                else if (index == 4) g.Height = value; else g.Opacity = value / 100;
                g.Validate();
            }
            else
            {
                HudOptions o = DraftOptions;
                bool recipes = _editingKind == 8;
                DraftOptions = new HudOptions(o.Enabled,
                    recipes && index == 0 ? value : o.RecipeScale, recipes && index == 1 ? (int)value : o.RecipesPerRow,
                    recipes && index == 2 ? (int)value : o.RecipeMaxRows, recipes && index == 3 ? value : o.RecipeRowSpacing,
                    !recipes && index == 2 ? value : o.ContentsScale,
                    !recipes && index == 5 ? (int)value : o.ContentsPerRow, recipes && index == 4 ? value / 100 : o.RecipeOpacity,
                    !recipes && index == 7 ? value / 100 : o.EmptySlotOpacity,
                    !recipes && index == 6 ? value / 100 : o.IngredientOpacity,
                    !recipes && index == 0 ? value / 100 : o.ContentsX, !recipes && index == 1 ? value / 100 : o.ContentsY,
                    !recipes && index == 3 ? value : o.ContentsWidth, !recipes && index == 4 ? value : o.ContentsHeight);
                if (Plugin.Runtime != null) Plugin.Runtime.SetOptions(DraftOptions);
            }
        }
        internal void ResetCategory()
        {
            if (_editingKind < 0) return;
            if (_editingKind < 8) Targets.Values[_editingKind] = new HudGeometry();
            else if (_editingKind == 8) { SetNumber(0, 1); SetNumber(1, 0); SetNumber(2, 0); SetNumber(3, 8); SetNumber(4, 100); }
            else { SetNumber(0, 0); SetNumber(1, 0); SetNumber(2, 1); SetNumber(3, 1); SetNumber(4, 1); SetNumber(5, 0); SetNumber(6, 100); SetNumber(7, 100); }
            ClearText();
        }
        private string NumberKey(int index) { return _editingKind.ToString(CultureInfo.InvariantCulture) + ":" + index; }
        internal void Invalid(int index, bool value)
        { if (value) _invalid.Add(NumberKey(index)); else _invalid.Remove(NumberKey(index)); if (_invalid.Count == 0) _error = false; }
        internal bool IsInvalid(int index) { return _invalid.Contains(NumberKey(index)); }
        private HudGeometry ReadGeometry(int category)
        {
            if (category < 8) return Targets.Values[category].Copy();
            HudOptions o = DraftOptions;
            return new HudGeometry { X = o.ContentsX, Y = o.ContentsY, Scale = o.ContentsScale,
                Width = o.ContentsWidth, Height = o.ContentsHeight };
        }
        private void WriteGeometry(int category, HudGeometry value)
        {
            if (category < 8) { Targets.Values[category] = value; return; }
            HudOptions o = DraftOptions;
            DraftOptions = new HudOptions(o.Enabled, o.RecipeScale, o.RecipesPerRow, o.RecipeMaxRows, o.RecipeRowSpacing,
                value.Scale, o.ContentsPerRow, o.RecipeOpacity, o.EmptySlotOpacity, o.IngredientOpacity,
                value.X, value.Y, value.Width, value.Height);
            if (Plugin.Runtime != null) Plugin.Runtime.SetOptions(DraftOptions);
        }
    }
}
