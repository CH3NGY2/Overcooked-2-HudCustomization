using System.Globalization;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed partial class HudEditor
    {
        internal string CategoryName(int kind)
        { return Text(kind < 8 ? EditorText.Kinds[kind] : kind == 8 ? SettingsText.Recipes : SettingsText.Contents); }
        internal string PreviewStatus
        {
            get
            {
                if (_editingKind < 0) return Text(EditorText.SelectCategoryText);
                if (_editingKind >= 8) return Text(EditorText.LiveLayout);
                return Text(Targets.HasInstance((HudTargetKind)_editingKind) ? EditorText.CategoryActive : EditorText.NoInstance);
            }
        }
        internal int NumberCount { get { return _editingKind < 0 ? 0 : _editingKind < 8 ? 6 : _editingKind == 8 ? 4 : 4; } }
        internal string NumberLabel(int index)
        {
            if (_editingKind < 8)
            {
                if (index == 0) return "X (%)"; if (index == 1) return "Y (%)";
                return Text(index == 2 ? EditorText.Scale : index == 3 ? EditorText.Width : index == 4 ? EditorText.Height : EditorText.Opacity);
            }
            if (_editingKind == 8)
                return Text(index == 0 ? SettingsText.RecipeScale : index == 1 ? SettingsText.RecipesPerRow
                    : index == 2 ? SettingsText.RecipeRowSpacing : SettingsText.RecipeOpacity);
            return Text(index == 0 ? SettingsText.ContentsScale : index == 1 ? SettingsText.ContentsPerRow
                : index == 2 ? SettingsText.EmptySlotOpacity : SettingsText.IngredientOpacity);
        }
        internal void NumberRange(int index, out float min, out float max, out bool integer)
        {
            integer = _editingKind >= 8 && index == 1;
            if (_editingKind < 8)
            {
                min = index < 2 ? -100 : index == 2 ? .2f : index < 5 ? .25f : 0;
                max = index < 2 ? 100 : index == 2 ? 3 : index < 5 ? 4 : 100;
            }
            else { min = index == 0 ? .5f : 0; max = index == 0 ? 2 : index == 1 ? 20 : 100; }
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
            if (_editingKind == 8) return index == 0 ? o.RecipeScale : index == 1 ? o.RecipesPerRow : index == 2 ? o.RecipeRowSpacing : o.RecipeOpacity * 100;
            return index == 0 ? o.ContentsScale : index == 1 ? o.ContentsPerRow : index == 2 ? o.EmptySlotOpacity * 100 : o.IngredientOpacity * 100;
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
                    recipes && index == 2 ? value : o.RecipeRowSpacing, !recipes && index == 0 ? value : o.ContentsScale,
                    !recipes && index == 1 ? (int)value : o.ContentsPerRow, recipes && index == 3 ? value / 100 : o.RecipeOpacity,
                    !recipes && index == 2 ? value / 100 : o.EmptySlotOpacity, !recipes && index == 3 ? value / 100 : o.IngredientOpacity);
                if (Plugin.Runtime != null) Plugin.Runtime.SetOptions(DraftOptions);
            }
        }
        internal void ResetCategory()
        {
            if (_editingKind < 0) return;
            if (_editingKind < 8) Targets.Values[_editingKind] = new HudGeometry();
            else if (_editingKind == 8) { SetNumber(0, 1); SetNumber(1, 0); SetNumber(2, 8); SetNumber(3, 100); }
            else { SetNumber(0, 1); SetNumber(1, 0); SetNumber(2, 100); SetNumber(3, 100); }
            ClearText();
        }
        private string NumberKey(int index) { return _editingKind.ToString(CultureInfo.InvariantCulture) + ":" + index; }
        internal void Invalid(int index, bool value)
        { if (value) _invalid.Add(NumberKey(index)); else _invalid.Remove(NumberKey(index)); if (_invalid.Count == 0) _error = false; }
        internal bool IsInvalid(int index) { return _invalid.Contains(NumberKey(index)); }
    }
}
