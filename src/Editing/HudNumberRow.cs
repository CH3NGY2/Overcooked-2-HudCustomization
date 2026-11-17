using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    // Native uGUI input/slider; invalid intermediate text stays visible and blocks saving.
    internal sealed class HudNumberRow
    {
        private readonly HudEditor _editor;
        private readonly RectTransform _root;
        private readonly Text _label;
        private readonly Slider _slider;
        private readonly InputField _field;
        private int _index, _kind;
        private bool _sync;
        private float _min, _max;
        internal HudNumberRow(HudEditorPanel panel, HudEditor editor, HudNativeAssets assets, Transform parent, int index)
        {
            _editor = editor;
            _root = HudNativeUi.Rect(parent, "Parameter_" + index, 0, index * 47, 412, 44);
            _label = HudNativeUi.Text(HudNativeUi.Rect(_root, "Label", 0, 0, 186, 42), "", assets.BodyFont, 16, HudNativeUi.Ink);
            _label.resizeTextForBestFit = true; _label.resizeTextMinSize = 13; _label.resizeTextMaxSize = 16;
            RectTransform track = HudNativeUi.Rect(_root, "Slider", 192, 6, 128, 32);
            HudNativeUi.Image(track, null, new Color(0f, 0f, 0f, 0f), true);
            HudNativeUi.Image(HudNativeUi.Rect(track, "Track", 0, 14, 128, 4), null, new Color(.35f, .45f, .52f, .5f), false);
            RectTransform area = HudNativeUi.Rect(track, "HandleArea", 7, 6, 114, 20);
            RectTransform handle = HudNativeUi.Rect(area, "Handle", 0, 0, 12, 0);
            Image handleImage = HudNativeUi.Image(handle, null, HudNativeUi.Blue, true);
            _slider = track.gameObject.AddComponent<Slider>(); _slider.handleRect = handle; _slider.targetGraphic = handleImage;
            _slider.direction = Slider.Direction.LeftToRight;
            RectTransform field = HudNativeUi.Rect(_root, "Value", 330, 6, 80, 32);
            Image fieldImage = HudNativeUi.Image(field, null, new Color(.97f, .98f, 1f, 1f), true);
            Text text = HudNativeUi.Text(HudNativeUi.Rect(field, "Text", 6, 2, 68, 28), "", assets.BodyFont, 18, HudNativeUi.Ink);
            text.alignment = TextAnchor.MiddleRight;
            _field = field.gameObject.AddComponent<InputField>(); _field.textComponent = text; _field.targetGraphic = fieldImage;
            _field.lineType = InputField.LineType.SingleLine; _field.characterLimit = 18;
            _field.onValidateInput = delegate(string current, int position, char added)
                { return added == '`' || added == '~' ? '\0' : added; };
            _slider.onValueChanged.AddListener(delegate(float value)
            {
                if (_sync) return;
                editor.Ui(delegate { editor.SetNumber(_index, value); editor.Invalid(_index, false); RefreshValue(true); });
            });
            _field.onValueChanged.AddListener(delegate(string value)
            {
                if (_sync) return;
                editor.Ui(delegate
                {
                    float parsed; bool valid = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                        && !float.IsNaN(parsed) && !float.IsInfinity(parsed) && parsed >= _min && parsed <= _max
                        && (!_slider.wholeNumbers || parsed == Mathf.Round(parsed));
                    editor.Invalid(_index, !valid);
                    text.color = valid ? HudNativeUi.Ink : Color.red;
                    if (valid) { editor.SetNumber(_index, parsed); RefreshValue(false); }
                });
            });
            Navigation nav = _slider.navigation; nav.mode = Navigation.Mode.None; _slider.navigation = nav; _field.navigation = nav;
        }
        internal void Bind(int kind, int index)
        {
            _kind = kind; _index = index;
            bool visible = index < _editor.NumberCount; _root.gameObject.SetActive(visible);
            if (!visible) return;
            HudNativeUi.Box(_root, 0, index * 47, 412, 44);
            bool integer; _label.text = _editor.NumberLabel(index);
            _editor.NumberRange(index, out _min, out _max, out integer);
            _sync = true;
            try { _slider.minValue = _min; _slider.maxValue = _max; _slider.wholeNumbers = integer; }
            finally { _sync = false; }
            RefreshValue(true);
        }
        internal void Refresh()
        { if (_root.gameObject.activeSelf && _kind == _editor.EditingKind && !_field.isFocused) RefreshValue(!_editor.IsInvalid(_index)); }
        private void RefreshValue(bool text)
        {
            _sync = true;
            try
            {
                float value = _editor.GetNumber(_index); _slider.value = value;
                if (text) { _field.text = value.ToString("0.###", CultureInfo.InvariantCulture); _field.textComponent.color = HudNativeUi.Ink; }
            }
            finally { _sync = false; }
        }
    }
}
