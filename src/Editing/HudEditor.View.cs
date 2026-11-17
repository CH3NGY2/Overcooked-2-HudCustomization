using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC2HudCustomization
{
    // IMGUI is restricted to HUD outlines/grips and the existing screen-space drag math.
    // The entire floating panel and all settings controls live on the native uGUI Canvas.
    internal sealed partial class HudEditor
    {
        private IHudEditorTarget _selected;
        private int _editingKind = -1;
        private bool _dragging, _moved, _error;
        private HudHandle _handle;
        private Vector2 _down;
        private Rect _downBounds;
        private HudGeometry _downValue;
        private readonly HashSet<string> _invalid = new HashSet<string>();
        internal HudLanguage DisplayLanguage { get { return _settings.DisplayLanguage; } }
        internal string Text(SettingsMessage message) { return message.Get(DisplayLanguage); }
        internal int EditingKind { get { return _editingKind; } }
        internal bool HasError { get { return _error; } }
        private float UiScale { get { return Mathf.Clamp(Screen.height / 1080f, .65f, 2.5f); } }
        private Rect Panel { get { return _panel == null ? new Rect() : _panel.Bounds; } }

        internal void Ui(Action action)
        { try { if (Active) action(); } catch (Exception e) { _log.ErrorOnce("HUD editor native UI callback", e); End(false); } }
        internal void OnGUI()
        {
            if (!Active) return;
            try { Draw(); }
            catch (Exception e) { _log.ErrorOnce("HUD editor overlay", e); End(false); }
        }
        private void Draw()
        {
            float scale = UiScale;
            Vector2 pointerPixels = Event.current == null ? Vector2.zero : Event.current.mousePosition;
            Matrix4x4 previousMatrix = GUI.matrix; int previousDepth = GUI.depth;
            GUI.depth = -10000; GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            try
            {
                Rect selectedBounds;
                if (_selected != null && !_selected.TryBounds(out selectedBounds)) { _selected = null; _dragging = false; }
                foreach (IHudEditorTarget target in EditableTargets)
                {
                    Rect bounds;
                    if (!target.TryBounds(out bounds)) continue;
                    Rect box = Scaled(bounds, 1f / scale);
                    Border(box, target == _selected ? new Color(1f, .8f, .15f, 1f) : new Color(.25f, .85f, 1f, .65f));
                    if (target == _selected) Handles(box);
                }
                Event input = Event.current;
                if (input != null && input.type == EventType.KeyDown && input.keyCode == KeyCode.BackQuote) input.Use();
                if (input != null && !_panel.Dragging && !_panel.BlocksHudInput
                    && (!Panel.Contains(pointerPixels / scale) || _dragging)) Pointer(input, pointerPixels);
            }
            finally { GUI.matrix = previousMatrix; GUI.depth = previousDepth; }
        }
        private void ClearText() { _invalid.Clear(); if (_panel != null) _panel.Refresh(true); }
        private IEnumerable<IHudEditorTarget> EditableTargets
        {
            get
            {
                if (_editingKind == 9)
                {
                    ContentsAdapter contents = Plugin.Runtime == null ? null : Plugin.Runtime.Contents;
                    if (contents != null) foreach (ContentsState state in contents.Targets) yield return state;
                }
                else foreach (HudTarget target in Targets.Targets)
                    if (target.Pickable && (int)target.Kind == _editingKind) yield return target;
            }
        }
        private void Select(IHudEditorTarget target)
        {
            if (target == null) { _selected = null; _dragging = false; return; }
            int kind = target.Category;
            if (_editingKind != kind) ActivateCategory(kind);
            _selected = target; _dragging = false;
        }
        internal void ToggleCategory(int kind)
        {
            if (kind == _editingKind)
            { Targets.ClearPreviews(); _selected = null; _editingKind = -1; _dragging = false; ClearText(); }
            else ActivateCategory(kind);
        }
        private void ActivateCategory(int kind)
        {
            if (!CategoryAvailable(kind)) return;
            _selected = null; _editingKind = kind; _dragging = false;
            Targets.ClearPreviews();
            if (kind < (int)HudTargetKind.Count)
            {
                // Historical Editable=false remains readable; explicitly selecting a category
                // enables picking it in this draft, without disabling other saved HUD geometry.
                Targets.Values[kind].Allowed = true; Targets.Previews[kind] = true;
                Targets.PreparePreview((HudTargetKind)kind);
                Targets.Tick();
            }
            foreach (IHudEditorTarget target in EditableTargets)
            { Rect bounds; if (target.TryBounds(out bounds)) { _selected = target; break; } }
            ClearText();
        }
        private void Pointer(Event input, Vector2 mouse)
        {
            float scale = UiScale;
            if (input.type == EventType.MouseDown && input.button == 0)
            {
                IHudEditorTarget picked = null; Rect bounds = new Rect(); float area = float.MaxValue;
                Rect current;
                if (_selected != null && _selected.TryBounds(out current) && Expanded(current, 9f * scale).Contains(mouse))
                { picked = _selected; bounds = current; }
                else foreach (IHudEditorTarget target in EditableTargets)
                {
                    Rect candidate;
                    if (!target.TryBounds(out candidate)
                        || !candidate.Contains(mouse) || candidate.width * candidate.height >= area) continue;
                    picked = target; bounds = candidate; area = candidate.width * candidate.height;
                }
                Select(picked);
                if (picked != null)
                {
                    _down = mouse; _downBounds = bounds; _downValue = ReadGeometry(picked.Category);
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
                    bool contents = _selected.Category == 9;
                    WriteGeometry(_selected.Category, HudDragMath.Drag(_downValue, _handle, delta.x, delta.y,
                        _downBounds.width, _downBounds.height, Screen.width, Screen.height, contents ? .5f : .2f, contents ? 2f : 3f));
                    _invalid.Clear();
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
