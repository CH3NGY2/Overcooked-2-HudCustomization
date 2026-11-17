using System.Collections.Generic;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed class ContentsAdapter
    {
        private readonly ContentsAccess _access;
        private readonly HudLog _log;
        private readonly Dictionary<IngredientContentsUIContainer, ContentsState> _states = new Dictionary<IngredientContentsUIContainer, ContentsState>();
        private readonly Dictionary<ClientIngredientContentGUI, IngredientContentsUIContainer> _owners = new Dictionary<ClientIngredientContentGUI, IngredientContentsUIContainer>();
        private readonly List<IngredientContentsUIContainer> _dead = new List<IngredientContentsUIContainer>();
        private readonly List<ClientIngredientContentGUI> _deadOwners = new List<ClientIngredientContentGUI>();

        internal ContentsAdapter(ContentsAccess access, HudLog log) { _access = access; _log = log; }

        internal bool HasInstance
        { get { foreach (ContentsState state in _states.Values) if (state.UI && state.UI.gameObject.scene.IsValid()) return true; return false; } }
        internal IEnumerable<ContentsState> Targets { get { return _states.Values; } }

        internal void Observe(ClientIngredientContentGUI owner, HudOptions options)
        {
            if (!owner) return;
            IngredientContentsUIContainer ui = (IngredientContentsUIContainer)_access.Instance.GetValue(owner);
            if (!ui) return;
            IngredientContentsUIContainer old;
            if (_owners.TryGetValue(owner, out old) && old != ui) Forget(old);
            _owners[owner] = ui;
            ContentsState state;
            if (!_states.TryGetValue(ui, out state))
            { state = new ContentsState(ui, _access, _log); _states.Add(ui, state); }
            state.MarkDirty(); state.Apply(options);
        }

        internal void Refresh(IngredientContentsUIContainer ui, HudOptions options)
        {
            ContentsState state;
            if (_states.TryGetValue(ui, out state)) { state.MarkDirty(); state.Apply(options); }
        }

        internal void BeforeRefresh(UISubElementContainer ui)
        {
            IngredientContentsUIContainer contents = ui as IngredientContentsUIContainer;
            ContentsState state;
            if (contents && _states.TryGetValue(contents, out state)) state.BeforeNativeRefresh();
        }

        internal void Suspend(ClientIngredientContentGUI owner, bool remove)
        {
            IngredientContentsUIContainer ui;
            if (!_owners.TryGetValue(owner, out ui)) return;
            ContentsState state;
            if (_states.TryGetValue(ui, out state)) state.Restore();
            if (remove) { Forget(ui); _owners.Remove(owner); }
        }

        private void Forget(IngredientContentsUIContainer ui)
        {
            ContentsState state;
            if (!object.ReferenceEquals(ui, null) && _states.TryGetValue(ui, out state))
            { state.Release(); _states.Remove(ui); }
        }

        internal void Tick(HudOptions options)
        {
            _dead.Clear();
            foreach (KeyValuePair<IngredientContentsUIContainer, ContentsState> pair in _states)
            {
                if (!pair.Key) _dead.Add(pair.Key);
                else if (options.Enabled) pair.Value.Apply(options);
            }
            for (int i = 0; i < _dead.Count; i++) Forget(_dead[i]);
            _deadOwners.Clear();
            foreach (ClientIngredientContentGUI owner in _owners.Keys) if (!owner) _deadOwners.Add(owner);
            for (int i = 0; i < _deadOwners.Count; i++)
            { Forget(_owners[_deadOwners[i]]); _owners.Remove(_deadOwners[i]); }
        }

        internal void MarkDirty() { foreach (ContentsState state in _states.Values) state.MarkDirty(); }
        internal void Restore() { foreach (ContentsState state in _states.Values) state.Restore(); }
    }
}
