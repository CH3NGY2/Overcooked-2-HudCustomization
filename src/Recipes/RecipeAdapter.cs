using System.Collections.Generic;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed class RecipeAdapter
    {
        private sealed class FlowState
        {
            internal readonly RecipeFlowGUI Flow;
            internal readonly float Gap, Margin;
            internal readonly List<RecipeCardState> Cards = new List<RecipeCardState>();
            internal FlowState(RecipeFlowGUI flow, RecipeAccess access)
            {
                Flow = flow;
                Gap = (float)access.Gap.GetValue(flow); Margin = (float)access.Margin.GetValue(flow);
            }
        }

        private readonly RecipeAccess _access;
        private readonly HudLog _log;
        private readonly Dictionary<RecipeFlowGUI, FlowState> _flows = new Dictionary<RecipeFlowGUI, FlowState>();
        private readonly Dictionary<RecipeWidgetUIController, RecipeCardState> _cards = new Dictionary<RecipeWidgetUIController, RecipeCardState>();
        private readonly List<RecipeFlowGUI> _deadFlows = new List<RecipeFlowGUI>();
        private readonly List<RecipeWidgetUIController> _deadCards = new List<RecipeWidgetUIController>();

        internal RecipeAdapter(RecipeAccess access, HudLog log) { _access = access; _log = log; }

        // Only actual flow-owned widgets enter this registry. RecipeViewer clones never do.
        internal void Observe(RecipeFlowGUI flow, bool nativeLayout, bool added)
        {
            if (!flow) return;
            FlowState state;
            if (!_flows.TryGetValue(flow, out state)) { state = new FlowState(flow, _access); _flows.Add(flow, state); }
            state.Cards.Clear();
            if (nativeLayout) Add(state, (List<RecipeFlowGUI.RecipeWidgetData>)_access.Ordered.GetValue(flow), true, false);
            else
            {
                Add(state, (List<RecipeFlowGUI.RecipeWidgetData>)_access.Active.GetValue(flow), false, added);
                Add(state, (List<RecipeFlowGUI.RecipeWidgetData>)_access.Dying.GetValue(flow), false, false);
            }
        }

        private void Add(FlowState flow, List<RecipeFlowGUI.RecipeWidgetData> list, bool nativeLayout, bool added)
        {
            for (int i = 0; i < list.Count; i++)
            {
                RecipeWidgetUIController widget = list[i].m_widget;
                if (!widget) continue;
                RecipeCardState card;
                if (!_cards.TryGetValue(widget, out card))
                {
                    card = new RecipeCardState(widget, _access, _log, added && i == list.Count - 1);
                    _cards.Add(widget, card);
                }
                if (nativeLayout) card.CaptureNativeOffset();
                flow.Cards.Add(card);
            }
        }

        internal void Refresh(RecipeWidgetUIController widget, HudOptions options)
        {
            RecipeCardState card;
            if (!_cards.TryGetValue(widget, out card)) return;
            card.InvalidateBounds();
            if (options.Enabled) card.Prepare(options);
        }

        internal void Tick(HudOptions options)
        {
            bool preview = Plugin.Editor != null && Plugin.Editor.AllowsRecipePreview;
            _deadFlows.Clear();
            foreach (KeyValuePair<RecipeFlowGUI, FlowState> pair in _flows)
            {
                if (!pair.Key) { _deadFlows.Add(pair.Key); continue; }
                if (options.Enabled && pair.Key.isActiveAndEnabled) Apply(pair.Value, options, preview);
            }
            for (int i = 0; i < _deadFlows.Count; i++) _flows.Remove(_deadFlows[i]);
            _deadCards.Clear();
            foreach (KeyValuePair<RecipeWidgetUIController, RecipeCardState> pair in _cards)
                if (!pair.Key) _deadCards.Add(pair.Key);
            for (int i = 0; i < _deadCards.Count; i++) _cards.Remove(_deadCards[i]);
        }

        private void Apply(FlowState state, HudOptions options, bool preview)
        {
            bool customLayout = options.RecipesPerRow > 0 || !Mathf.Approximately(options.RecipeScale, 1f);
            Transform space = state.Flow.transform;
            RectTransform area = space as RectTransform;
            float safeWidth = float.MaxValue;
            HudRowLayout layout = new HudRowLayout();
            bool started = false;
            float firstTop = 0f, firstRight = 0f, firstLeft = 0f;
            float deltaTime = preview ? Time.unscaledDeltaTime : TimeManager.GetDeltaTime(state.Flow.gameObject);
            for (int i = 0; i < state.Cards.Count; i++)
            {
                RecipeCardState card = state.Cards[i];
                if (!card.Widget) continue;
                card.Prepare(options);
                card.Preview(preview);
                if (!customLayout) continue;
                Rect bounds;
                if (!card.Measure(space, out bounds)) continue;
                Rect head;
                float headTop = card.MeasureHead(space, out head) ? head.yMax : bounds.yMax;
                if (!started)
                {
                    started = true;
                    if (area && area.rect.xMax > bounds.xMin + state.Margin) safeWidth = area.rect.xMax - bounds.xMin - state.Margin;
                    layout = new HudRowLayout(bounds.xMin, headTop, options.RecipesPerRow, state.Gap, options.RecipeRowSpacing, safeWidth);
                    firstTop = headTop; firstLeft = bounds.xMin;
                }
                if (bounds.width > safeWidth)
                    _log.WarnOnce("wide-recipe", "A single recipe exceeds the parent safe width; its chosen scale is preserved.");
                float left, top;
                layout.Place(bounds.width, bounds.height, out left, out top);
                bool firstRow = Mathf.Abs(top - firstTop) < .01f;
                if (firstRow) firstRight = left + bounds.width;
                float entryLeft = firstRow ? left : Mathf.Max(firstLeft, Mathf.Min(firstRight + state.Gap, firstLeft + safeWidth - bounds.width));
                card.Move(new Vector2(left, top), new Vector2(entryLeft, firstTop), bounds, space, deltaTime);
            }
        }

        internal void Forget(RecipeFlowGUI flow)
        {
            FlowState state;
            if (!_flows.TryGetValue(flow, out state)) return;
            for (int i = 0; i < state.Cards.Count; i++)
            {
                state.Cards[i].Restore();
                _cards.Remove(state.Cards[i].Widget);
            }
            _flows.Remove(flow);
        }

        internal void Forget(RecipeWidgetUIController widget)
        {
            RecipeCardState state;
            if (_cards.TryGetValue(widget, out state)) { state.Restore(); _cards.Remove(widget); }
        }

        internal void Restore()
        {
            foreach (RecipeCardState state in _cards.Values) state.Restore();
        }
        internal void StopPreview() { foreach (RecipeCardState state in _cards.Values) state.Preview(false); }
    }
}
