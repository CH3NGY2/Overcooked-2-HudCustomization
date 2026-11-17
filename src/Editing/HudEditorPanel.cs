using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OC2HudCustomization
{
    // The editor owns this Canvas and every control. Only artwork and fonts come from
    // GameOptions; native menus and their input/callback lifecycle are left to the game.
    internal sealed class HudEditorPanel : IDisposable
    {
        private readonly HudEditor _editor;
        private readonly HudNativeAssets _assets;
        internal GameObject Root { get; private set; }
        internal HudEditorEventSystem Domain { get; private set; }
        internal RectTransform Frame, Header;
        internal bool Dragging;
        private CanvasScaler _scaler;
        private EventSystem _previousDomain;
        private HudCategoryPicker _picker;
        private readonly HudNumberRow[] _numbers = new HudNumberRow[8];
        private Text _title, _status, _error;
        private Button _reset, _save, _cancel;
        private ScrollRect _scroll;
        private RectTransform _viewport, _content;
        private Vector2 _position;
        private bool _positioned;
        private HudLanguage _language = (HudLanguage)(-1);
        private int _kind = -2;
        internal float Scale { get { return Mathf.Clamp(Screen.height / 1080f, .65f, 2.5f); } }
        internal Rect Bounds { get { return new Rect(_position.x, _position.y, 470f, Height); } }
        private float Height { get { return Mathf.Min(700f, Screen.height / Scale - 24f); } }
        internal bool BlocksHudInput { get { return _picker != null && _picker.IsOpen; } }
        internal Vector2 Position
        {
            get { return _position; }
            set
            {
                _position = new Vector2(Mathf.Clamp(value.x, 0f, Mathf.Max(0f, Screen.width / Scale - 470f)),
                    Mathf.Clamp(value.y, 0f, Mathf.Max(0f, Screen.height / Scale - Height)));
                HudNativeUi.Box(Frame, _position.x, _position.y, 470f, Height);
            }
        }
        internal HudEditorPanel(HudEditor editor, HudNativeAssets assets)
        {
            _editor = editor; _assets = assets;
            Root = new GameObject("HudEditorNativeCanvas", typeof(RectTransform)); Root.SetActive(false);
            Object.DontDestroyOnLoad(Root);
            try
            {
                Canvas canvas = Root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                int top = int.MinValue;
                foreach (SortingLayer layer in SortingLayer.layers)
                    if (layer.value >= top) { top = layer.value; canvas.sortingLayerID = layer.id; }
                canvas.sortingOrder = short.MaxValue;
                _scaler = Root.AddComponent<CanvasScaler>(); _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                Root.AddComponent<GraphicRaycaster>(); Domain = Root.AddComponent<HudEditorEventSystem>();
                Domain.sendNavigationEvents = false;
                Root.AddComponent<HudEditorInputModule>();
                Build();
            }
            catch { Dispose(); throw; }
        }
        private Text Label(string name, string value, float x, float y, float width, float height, int size)
        { return HudNativeUi.Text(HudNativeUi.Rect(Frame, name, x, y, width, height), value, _assets.BodyFont, size, HudNativeUi.Ink); }
        private void Build()
        {
            Frame = HudNativeUi.Rect(Root.transform, "HudEditorPanel", 0, 0, 470, Height);
            HudNativeUi.Image(Frame, null, new Color(.86f, .90f, .93f, .98f), true);
            Header = HudNativeUi.Rect(Frame, "DragHeader", 10, 10, 450, 55);
            HudNativeUi.Image(Header, _assets.Header, HudNativeUi.Blue, true);
            Header.gameObject.AddComponent<HudPanelDrag>().Panel = this;
            _title = HudNativeUi.Text(HudNativeUi.Rect(Header, "Title", 18, 3, 414, 48), "", _assets.HeaderFont, 25, Color.white);
            _title.resizeTextForBestFit = true; _title.resizeTextMinSize = 17; _title.resizeTextMaxSize = 25;
            _status = Label("PreviewStatus", "", 28, 124, 414, 44, 16);
            _viewport = HudNativeUi.Rect(Frame, "ParameterViewport", 28, 174, 414, Height - 322);
            HudNativeUi.Image(_viewport, null, new Color(1f, 1f, 1f, .17f), true);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _content = HudNativeUi.Rect(_viewport, "Parameters", 0, 0, 414, 0);
            _scroll = _viewport.gameObject.AddComponent<ScrollRect>(); _scroll.viewport = _viewport; _scroll.content = _content;
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 25;
            for (int i = 0; i < _numbers.Length; i++) _numbers[i] = new HudNumberRow(this, _editor, _assets, _content, i);
            _reset = HudNativeUi.Button(Frame, "ResetCategory", "", _assets, 28, Height - 100, 414, 31,
                delegate { _editor.Ui(_editor.ResetCategory); });
            _save = HudNativeUi.Button(Frame, "Save", "", _assets, 28, Height - 59, 254, 34,
                delegate { _editor.Ui(delegate { _editor.End(true); }); });
            _cancel = HudNativeUi.Button(Frame, "Cancel", "", _assets, 290, Height - 59, 152, 34,
                delegate { _editor.Ui(delegate { _editor.End(false); }); });
            _error = Label("SaveError", "", 28, Height - 138, 414, 32, 15); _error.color = new Color(.7f, .06f, .04f, 1f);
            _picker = new HudCategoryPicker(_editor, _assets, Frame);
        }
        internal void Show()
        {
            _previousDomain = EventSystem.current; _kind = -2; _picker.Close();
            Root.SetActive(true);
            if (_previousDomain && _previousDomain != Domain) EventSystem.current = _previousDomain;
            Tick(); Refresh(true);
        }
        internal void Hide()
        {
            Dragging = false;
            if (_picker != null) _picker.Close();
            if (Domain) Domain.SetSelectedGameObject(null);
            if (Root) Root.SetActive(false);
            if (_previousDomain) EventSystem.current = _previousDomain;
            _previousDomain = null;
        }
        internal void Tick()
        {
            _scaler.scaleFactor = Scale;
            if (!_positioned) { _position = new Vector2(Screen.width / Scale - 490f, 18f); _positioned = true; }
            Position = _position;
            LayoutParameters();
            HudNativeUi.Box(_reset.transform as RectTransform, 28, Height - 100, 414, 31);
            HudNativeUi.Box(_save.transform as RectTransform, 28, Height - 59, 254, 34);
            HudNativeUi.Box(_cancel.transform as RectTransform, 290, Height - 59, 152, 34);
            HudNativeUi.Box(_error.rectTransform, 28, Height - 137, 414, 32);
            Refresh(false);
        }
        internal void Refresh(bool force)
        {
            if (!Root || !Root.activeSelf) return;
            _picker.Refresh();
            bool changed = force || _kind != _editor.EditingKind || _language != _editor.DisplayLanguage;
            if (changed)
            {
                _kind = _editor.EditingKind; _language = _editor.DisplayLanguage;
                _title.text = _editor.Text(EditorText.Title);
                _save.GetComponentInChildren<Text>().text = _editor.Text(EditorText.Save);
                _cancel.GetComponentInChildren<Text>().text = _editor.Text(EditorText.Cancel);
                _reset.GetComponentInChildren<Text>().text = _editor.Text(EditorText.Reset);
                _reset.interactable = _kind >= 0;
                for (int i = 0; i < _numbers.Length; i++) _numbers[i].Bind(_kind, i);
                LayoutParameters();
                _content.sizeDelta = new Vector2(414, _editor.NumberCount * 47f);
                _scroll.verticalNormalizedPosition = 1f;
            }
            _status.text = _editor.PreviewStatus;
            _error.text = _editor.HasError ? _editor.Text(EditorText.SaveError) : "";
            for (int i = 0; i < _numbers.Length; i++) _numbers[i].Refresh();
        }
        internal void FinishText()
        { if (Domain) Domain.SetSelectedGameObject(null); }
        private void LayoutParameters()
        {
            bool contents = _editor.EditingKind == 9;
            _status.gameObject.SetActive(!contents);
            float top = contents ? 128f : 174f;
            HudNativeUi.Box(_viewport, 28, top, 414, Mathf.Max(64f, Height - top - 148f));
        }
        public void Dispose()
        { Hide(); if (Root) Object.Destroy(Root); Root = null; Domain = null; }
    }
}
