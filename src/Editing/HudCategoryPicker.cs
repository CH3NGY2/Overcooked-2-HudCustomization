using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    // A compact selector using the same native buttons as the editor. Its popup keeps
    // unavailable categories visible but disabled, including those without an instance.
    internal sealed class HudCategoryPicker
    {
        private readonly HudEditor _editor;
        private readonly Button _toggle;
        private readonly RectTransform _popup;
        private readonly Button[] _items = new Button[10];
        private readonly Text[] _labels = new Text[10];
        private readonly GameObject[] _markers = new GameObject[10];
        private readonly Text _caption, _arrow;
        internal bool IsOpen { get { return _popup.gameObject.activeSelf; } }

        internal HudCategoryPicker(HudEditor editor, HudNativeAssets assets, RectTransform frame)
        {
            _editor = editor;
            _toggle = HudNativeUi.Button(frame, "CategoryPicker", "", assets, 28, 78, 414, 38,
                delegate { _editor.Ui(delegate { SetOpen(!IsOpen); }); });
            _caption = _toggle.GetComponentInChildren<Text>();
            HudNativeUi.Box(_caption.rectTransform, 12, 2, 357, 34); _caption.alignment = TextAnchor.MiddleLeft;
            RectTransform arrow = HudNativeUi.Rect(_toggle.transform, "Arrow", 377, 2, 28, 34);
            _arrow = HudNativeUi.Text(arrow, "v", assets.BodyFont, 20, Color.white); _arrow.alignment = TextAnchor.MiddleCenter;
            _popup = HudNativeUi.Rect(frame, "CategoryPickerPopup", 0, 0, 470, 700);
            RectTransform outside = HudNativeUi.Rect(_popup, "CategoryPickerDismiss", -10000, -10000, 20000, 20000);
            Image blocker = HudNativeUi.Image(outside, null, Color.clear, true);
            Button dismiss = outside.gameObject.AddComponent<Button>(); dismiss.targetGraphic = blocker;
            dismiss.transition = Selectable.Transition.None;
            Navigation navigation = dismiss.navigation; navigation.mode = Navigation.Mode.None; dismiss.navigation = navigation;
            dismiss.onClick.AddListener(delegate { SetOpen(false); });
            RectTransform list = HudNativeUi.Rect(_popup, "CategoryList", 28, 120, 414, 372);
            HudNativeUi.Image(list, null, new Color(.79f, .85f, .89f, 1f), true);
            for (int i = 0; i < _items.Length; i++)
            {
                int kind = i;
                _items[i] = HudNativeUi.Button(list, "Category_" + i, "", assets, 6, 6 + i * 36, 402, 34,
                    delegate { _editor.Ui(delegate {
                        if (!_editor.CategoryAvailable(kind)) return;
                        SetOpen(false); _editor.ToggleCategory(kind);
                    }); });
                _labels[i] = _items[i].GetComponentInChildren<Text>();
                _labels[i].alignment = TextAnchor.MiddleLeft;
                _markers[i] = HudNativeUi.Rect(_items[i].transform, "Selected", 0, 0, 4, 34).gameObject;
                HudNativeUi.Image((RectTransform)_markers[i].transform, null, HudNativeUi.Gold, false);
            }
            SetOpen(false);
        }
        private void SetOpen(bool open)
        {
            _popup.gameObject.SetActive(open); _arrow.text = open ? "^" : "v";
            if (open) { Refresh(); _popup.SetAsLastSibling(); }
        }
        internal void Close() { SetOpen(false); }
        internal void Refresh()
        {
            int selected = _editor.EditingKind;
            _caption.text = selected < 0 ? _editor.Text(EditorText.SelectCategoryText) : _editor.CategoryName(selected);
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i].interactable = _editor.CategoryAvailable(i);
                _markers[i].SetActive(selected == i);
                _labels[i].text = _editor.CategoryName(i);
            }
        }
    }
}
