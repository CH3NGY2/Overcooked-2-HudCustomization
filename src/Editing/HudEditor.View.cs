using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace OC2HudCustomization
{
    // The overlay owns only its own IMGUI input. Native UI domains remain suppressed while
    // frozen; the original HUD objects stay alive and supply both geometry and presentation.
    internal sealed partial class HudEditor
    {
        private HudTarget _selected;
        private int _editingKind = -1;
        private bool _dragging, _moved, _error;
        private HudHandle _handle;
        private Vector2 _down;
        private Rect _downBounds;
        private HudGeometry _downValue;
        private Font _font;
        private GUISkin _skin;
        private GUIStyle _label, _heading, _toggle, _button, _field;
        private readonly Dictionary<string, string> _text = new Dictionary<string, string>();
        private readonly HashSet<string> _invalid = new HashSet<string>();
        private HudLanguage Language { get { return _settings.DisplayLanguage; } }
        private string Text(SettingsMessage message) { return message.Get(Language); }
        private float UiScale { get { return Mathf.Clamp(Screen.height / 1080f, .65f, 2.5f); } }
        private Rect Panel { get { return new Rect(Screen.width / UiScale - 358f, 12f, 346f, 675f); } }

        internal void OnGUI()
        {
            if (!Active) return;
            try { Draw(); }
            catch (Exception e) { _log.ErrorOnce("HUD editor overlay", e); End(false); }
        }
        private void PrepareStyles()
        {
            if (_skin == GUI.skin && _label != null) return;
            _skin = GUI.skin;
            if (!_font) _font = Font.CreateDynamicFontFromOSFont(new string[] { "Microsoft YaHei", "Yu Gothic", "Malgun Gothic", "Arial" }, 16);
            _label = new GUIStyle(GUI.skin.label); _label.font = _font; _label.fontSize = 15; _label.wordWrap = true;
            _heading = new GUIStyle(_label); _heading.fontSize = 18; _heading.fontStyle = FontStyle.Bold;
            _toggle = new GUIStyle(GUI.skin.toggle); _toggle.font = _font; _toggle.fontSize = 15;
            _button = new GUIStyle(GUI.skin.button); _button.font = _font; _button.fontSize = 15; _button.wordWrap = true;
            _field = new GUIStyle(GUI.skin.textField); _field.font = _font; _field.fontSize = 15;
        }
        private void Draw()
        {
            PrepareStyles();
            float scale = UiScale;
            Vector2 pointerPixels = Event.current == null ? Vector2.zero : Event.current.mousePosition;
            Matrix4x4 previousMatrix = GUI.matrix; int previousDepth = GUI.depth;
            GUI.depth = -10000; GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            try
            {
                Rect panel = Panel;
                if (_dragging && Event.current != null && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp))
                    Pointer(Event.current, pointerPixels);
                for (int i = 0; i < Targets.Targets.Count; i++)
                {
                    HudTarget target = Targets.Targets[i]; Rect bounds;
                    if (!target.Pickable || !Targets.Values[(int)target.Kind].Allowed || !target.TryBounds(out bounds)) continue;
                    Rect box = Scaled(bounds, 1f / scale);
                    Border(box, target == _selected ? new Color(1f, .8f, .15f, 1f) : new Color(.25f, .85f, 1f, .65f));
                    if (target == _selected) Handles(box);
                }
                GUI.Box(panel, GUIContent.none);
                float x = panel.x + 12f, y = panel.y + 10f, width = panel.width - 24f;
                GUI.Label(new Rect(x, y, width, 27f), Text(EditorText.Title), _heading); y += 30f;
                GUI.Label(new Rect(x, y, width, 48f), Text(EditorText.Help), _label); y += 51f;
                GUI.Label(new Rect(x, y, width, 25f), Text(EditorText.Whitelist), _heading); y += 29f;
                for (int i = 0; i < (int)HudTargetKind.Count; i++)
                {
                    HudGeometry value = Targets.Values[i];
                    bool allowed = GUI.Toggle(new Rect(x, y, 24f, 23f), value.Allowed, GUIContent.none, _toggle);
                    if (GUI.Button(new Rect(x + 28f, y, width - 110f, 23f), Text(EditorText.Kinds[i]), _button)) SelectCategory((HudTargetKind)i);
                    bool previousEnabled = GUI.enabled; GUI.enabled = Targets.HasInstance((HudTargetKind)i);
                    Targets.Previews[i] = GUI.Toggle(new Rect(x + width - 78f, y, 78f, 23f), Targets.Previews[i],
                        new GUIContent(Text(EditorText.Preview), Text(EditorText.PreviewHelp)), _toggle);
                    GUI.enabled = previousEnabled;
                    y += 25f;
                    if (allowed != value.Allowed)
                    { value.Allowed = allowed; if (_selected != null && (int)_selected.Kind == i && !allowed) { _selected = null; _dragging = false; } }
                }
                y += 9f;
                Rect selectedBounds;
                if (_selected != null && (!_selected.TryBounds(out selectedBounds) || !Targets.Values[(int)_selected.Kind].Allowed)) { _selected = null; _dragging = false; }
                if (_editingKind < 0)
                    GUI.Label(new Rect(x, y, width, 75f), Text(EditorText.Select), _label);
                else
                {
                    GUI.Label(new Rect(x, y, width, 29f), Text(EditorText.Kinds[_editingKind]), _heading); y += 32f;
                    HudGeometry value = Targets.Values[_editingKind];
                    value.X = Number("X", "X (%)", value.X * 100f, -100f, 100f, x, ref y, width) / 100f;
                    value.Y = Number("Y", "Y (%)", value.Y * 100f, -100f, 100f, x, ref y, width) / 100f;
                    value.Scale = Number("Scale", Text(EditorText.Scale), value.Scale, .2f, 3f, x, ref y, width);
                    value.Width = Number("Width", Text(EditorText.Width), value.Width, .25f, 4f, x, ref y, width);
                    value.Height = Number("Height", Text(EditorText.Height), value.Height, .25f, 4f, x, ref y, width);
                    value.Opacity = Number("Opacity", Text(EditorText.Opacity), value.Opacity * 100f, 0f, 100f, x, ref y, width) / 100f;
                    value.Validate();
                    if (GUI.Button(new Rect(x, y + 3f, width, 30f), Text(EditorText.Reset), _button))
                    { bool allowed = value.Allowed; Targets.Values[_editingKind] = new HudGeometry { Allowed = allowed }; ClearText(); }
                    y += 37f;
                }
                if (_error) GUI.Label(new Rect(x, panel.yMax - 39f, width, 35f), Text(EditorText.SaveError), _label);
                Event input = Event.current;
                if (input != null && input.type == EventType.KeyDown && input.keyCode == KeyCode.BackQuote) input.Use();
                if (input != null && !panel.Contains(pointerPixels / scale)) Pointer(input, pointerPixels);
                else if (input != null && _dragging && (input.type == EventType.MouseDrag || input.type == EventType.MouseUp)) Pointer(input, pointerPixels);
            }
            finally { GUI.matrix = previousMatrix; GUI.depth = previousDepth; }
        }
        private float Number(string key, string label, float value, float min, float max, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, 94f, 25f), label, _label);
            float slider = GUI.HorizontalSlider(new Rect(x + 96f, y + 5f, width - 174f, 21f), value, min, max);
            string text;
            if (!_text.TryGetValue(key, out text)) text = value.ToString("0.###", CultureInfo.InvariantCulture);
            if (Mathf.Abs(slider - value) > .00001f)
            { value = slider; text = value.ToString("0.###", CultureInfo.InvariantCulture); _invalid.Remove(key); }
            Color previous = GUI.color; if (_invalid.Contains(key)) GUI.color = Color.red;
            string edited = GUI.TextField(new Rect(x + width - 72f, y, 72f, 25f), text, 18, _field); GUI.color = previous;
            if (edited != text)
            {
                float parsed;
                if (float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                    && !float.IsNaN(parsed) && !float.IsInfinity(parsed) && parsed >= min && parsed <= max)
                { value = parsed; _invalid.Remove(key); }
                else _invalid.Add(key);
            }
            _text[key] = edited; y += 32f; return value;
        }
        private void ClearText() { _text.Clear(); _invalid.Clear(); GUI.FocusControl(null); }
        private void Select(HudTarget target) { _selected = target; _editingKind = target == null ? -1 : (int)target.Kind; _dragging = false; ClearText(); }
        private void SelectCategory(HudTargetKind kind)
        {
            Select(null); _editingKind = (int)kind;
            foreach (HudTarget target in Targets.Targets)
            {
                Rect bounds;
                if (target.Pickable && target.Kind == kind && Targets.Values[(int)kind].Allowed && target.TryBounds(out bounds))
                { _selected = target; break; }
            }
        }
        private void Pointer(Event input, Vector2 mouse)
        {
            float scale = UiScale;
            if (input.type == EventType.MouseDown && input.button == 0)
            {
                HudTarget picked = null; Rect bounds = new Rect(); float area = float.MaxValue;
                // Prefer the current target at overlapping resize grips; otherwise pick the smallest visible target.
                Rect current;
                if (_selected != null && _selected.TryBounds(out current) && Expanded(current, 9f * scale).Contains(mouse))
                { picked = _selected; bounds = current; }
                else foreach (HudTarget target in Targets.Targets)
                {
                    Rect candidate;
                    if (!target.Pickable || !Targets.Values[(int)target.Kind].Allowed || !target.TryBounds(out candidate)
                        || !candidate.Contains(mouse) || candidate.width * candidate.height >= area) continue;
                    picked = target; bounds = candidate; area = candidate.width * candidate.height;
                }
                Select(picked);
                if (picked != null)
                {
                    _down = mouse; _downBounds = bounds; _downValue = Targets.Values[(int)picked.Kind].Copy();
                    _handle = HitHandle(bounds, mouse, 9f * scale); _dragging = true; _moved = false;
                }
                input.Use();
            }
            else if (_dragging && input.type == EventType.MouseDrag && input.button == 0)
            {
                Vector2 delta = mouse - _down;
                if (delta.magnitude >= 4f * scale) _moved = true;
                if (_moved && _selected != null)
                {
                    Targets.Values[(int)_selected.Kind] = HudDragMath.Drag(_downValue, _handle, delta.x, delta.y,
                        _downBounds.width, _downBounds.height, Screen.width, Screen.height);
                    ClearText();
                }
                input.Use();
            }
            else if (_dragging && input.type == EventType.MouseUp && input.button == 0) { _dragging = false; input.Use(); }
        }
        internal static HudHandle HitHandle(Rect bounds, Vector2 point, float edge)
        {
            HudHandle handle = HudHandle.Move;
            float ex = Mathf.Min(edge, bounds.width * .2f), ey = Mathf.Min(edge, bounds.height * .2f);
            if (Mathf.Abs(point.x - bounds.xMin) <= ex) handle |= HudHandle.Left;
            else if (Mathf.Abs(point.x - bounds.xMax) <= ex) handle |= HudHandle.Right;
            if (Mathf.Abs(point.y - bounds.yMin) <= ey) handle |= HudHandle.Top;
            else if (Mathf.Abs(point.y - bounds.yMax) <= ey) handle |= HudHandle.Bottom;
            return handle;
        }
        private static Rect Scaled(Rect rect, float scale) { return new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale); }
        private static Rect Expanded(Rect rect, float margin) { return Rect.MinMaxRect(rect.xMin - margin, rect.yMin - margin, rect.xMax + margin, rect.yMax + margin); }
        private static void Border(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void Handles(Rect rect)
        {
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
                if (i != 1 || j != 1) GUI.DrawTexture(new Rect((i == 0 ? rect.xMin : i == 1 ? rect.center.x : rect.xMax) - 3f,
                    (j == 0 ? rect.yMin : j == 1 ? rect.center.y : rect.yMax) - 3f, 6f, 6f), Texture2D.whiteTexture);
        }
    }
}
