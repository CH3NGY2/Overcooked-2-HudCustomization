using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal static class HudNativeUi
    {
        internal static readonly Color Ink = new Color(.13f, .24f, .30f, 1f);
        internal static readonly Color Blue = new Color(.18f, .43f, .56f, 1f);
        internal static readonly Color Gold = new Color(1f, .75f, .15f, 1f);
        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Box(rect, x, y, width, height); return rect;
        }
        internal static void Box(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        internal static Image Image(RectTransform rect, Sprite sprite, Color color, bool hit)
        {
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color;
            image.type = sprite && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = hit; return image;
        }
        internal static Text Text(RectTransform rect, string value, Font font, int size, Color color)
        {
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.text = value;
            text.color = color; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        internal static Button Button(Transform parent, string name, string label, HudNativeAssets assets,
            float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image backer = Image(rect, assets.Button, assets.Button ? Color.white : Blue, true);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = backer;
            ColorBlock colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f); colors.pressedColor = new Color(.8f, .8f, .8f, 1f);
            colors.disabledColor = new Color(.48f, .52f, .55f, .55f);
            colors.fadeDuration = .08f; button.colors = colors;
            Text text = Text(Rect(rect, "Label", 9, 2, width - 18, height - 4), label, assets.BodyFont, 18, Color.white);
            text.alignment = TextAnchor.MiddleCenter; text.resizeTextForBestFit = true; text.resizeTextMinSize = 13; text.resizeTextMaxSize = 18;
            button.onClick.AddListener(action); Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            return button;
        }
    }

    // A private mouse/text domain, never registered in the game's recyclable T17 pool.
    // Native T17 domains remain suppressed by the edit session's existing lease.
    internal sealed class HudEditorEventSystem : EventSystem
    {
        protected override void Update()
        {
            EventSystem previous = current;
            try { current = this; base.Update(); }
            finally { if (previous && previous != this) current = previous; }
        }
    }
    internal sealed class HudEditorInputModule : StandaloneInputModule
    {
        public override bool ShouldActivateModule() { return isActiveAndEnabled; }
    }
    internal sealed class HudPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal HudEditorPanel Panel;
        private Vector2 _start, _pointer;
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            _start = Panel.Position; _pointer = data.position; Panel.Dragging = true; data.Use();
        }
        public void OnDrag(PointerEventData data)
        {
            if (!Panel.Dragging || data.button != PointerEventData.InputButton.Left) return;
            Vector2 delta = (data.position - _pointer) / Panel.Scale;
            Panel.Position = _start + new Vector2(delta.x, -delta.y); data.Use();
        }
        public void OnEndDrag(PointerEventData data) { Panel.Dragging = false; data.Use(); }
    }
}
