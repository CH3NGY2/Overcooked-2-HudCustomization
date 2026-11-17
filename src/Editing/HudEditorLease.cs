using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OC2HudCustomization
{
    // Every native pause/input token belongs to this session. Never clear another menu's pause.
    internal sealed class HudEditorLease : IDisposable
    {
        private sealed class PlayerLease { internal PlayerControls Player; internal Suppressor Token; }
        private sealed class DomainLease { internal T17EventSystem Domain; internal Suppressor Token; }
        private readonly List<PlayerLease> _players = new List<PlayerLease>();
        private readonly List<DomainLease> _domains = new List<DomainLease>();
        private readonly UnityEngine.Object _owner;
        private readonly HudLog _log;
        private TimeManager _time;
        private static readonly FieldInfo PauseOwners = Required.Field(typeof(TimeManager), "m_arbitrationSupressors", typeof(List<object>[]));
        private List<object>[] _pauseOwners;
        internal bool AllowsRecipePreview
        {
            get
            {
                if (!_time || _pauseOwners == null) return false;
                foreach (List<object> owners in _pauseOwners)
                    foreach (object owner in owners) if (!ReferenceEquals(owner, this)) return false;
                return true;
            }
        }
        private Suppressor _engagement;
        private T17FrontendFlow _frontend;
        private bool _previousBlock, _cursorVisible, _main, _ui, _camera, _cursorOwned;
        private CursorLockMode _cursorLock;
        internal HudEditorLease(UnityEngine.Object owner, HudLog log) { _owner = owner; _log = log; }
        internal void Acquire()
        {
            _time = GameUtils.RequestManager<TimeManager>();
            if (!_time) throw new InvalidOperationException("No native TimeManager available");
            _pauseOwners = (List<object>[])PauseOwners.GetValue(_time);
            _main = true; _time.SetPaused(TimeManager.PauseLayer.Main, true, this);
            _ui = true; _time.SetPaused(TimeManager.PauseLayer.UI, true, this);
            _camera = true; _time.SetPaused(TimeManager.PauseLayer.Camera, true, this);
            _cursorVisible = Cursor.visible; _cursorLock = Cursor.lockState; _cursorOwned = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            _frontend = T17FrontendFlow.Instance;
            if (_frontend) { _previousBlock = _frontend.BlockFocusKitchen; _frontend.BlockFocusKitchen = true; }
            GamepadEngagementManager engagement = GameUtils.RequestManager<GamepadEngagementManager>();
            if (engagement) _engagement = engagement.Suppressor.AddSuppressor(_owner);
            foreach (PlayerControls player in UnityEngine.Object.FindObjectsOfType<PlayerControls>()) Track(player);
            foreach (T17EventSystem domain in UnityEngine.Object.FindObjectsOfType<T17EventSystem>()) Track(domain);
            Maintain();
        }
        internal void Track(PlayerControls player)
        {
            if (!player || !player.gameObject.scene.IsValid()) return;
            foreach (PlayerLease item in _players) if (item.Player == player) return;
            _players.Add(new PlayerLease { Player = player, Token = player.Suppress(_owner) });
        }
        internal void Track(T17EventSystem domain)
        {
            if (!domain || !domain.gameObject.scene.IsValid()) return;
            foreach (DomainLease item in _domains) if (item.Domain == domain) return;
            _domains.Add(new DomainLease { Domain = domain, Token = domain.Disable(_owner) });
        }
        internal void Maintain()
        {
            foreach (PlayerLease item in _players) if (item.Player && item.Token.IsReleased()) item.Token = item.Player.Suppress(_owner);
            foreach (DomainLease item in _domains) if (item.Domain && item.Token.IsReleased()) item.Token = item.Domain.Disable(_owner);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        private void Release(string label, Action action)
        { try { action(); } catch (Exception e) { _log.ErrorOnce("HUD editor release " + label, e); } }
        public void Dispose()
        {
            foreach (PlayerLease item in _players) Release("player", delegate { if (item.Player) item.Player.ReleaseSuppressor(item.Token); else item.Token.Release(); });
            foreach (DomainLease item in _domains) Release("UI input", delegate { if (item.Domain) item.Domain.ReleaseSuppressor(item.Token); else item.Token.Release(); });
            _players.Clear(); _domains.Clear();
            if (_engagement != null) { Release("engagement", _engagement.Release); _engagement = null; }
            Release("frontend focus", delegate { if (_frontend && _frontend.BlockFocusKitchen) _frontend.BlockFocusKitchen = _previousBlock; });
            _frontend = null;
            if (_time)
            {
                if (_camera) Release("camera pause", delegate { _time.SetPaused(TimeManager.PauseLayer.Camera, false, this); });
                if (_ui) Release("UI pause", delegate { _time.SetPaused(TimeManager.PauseLayer.UI, false, this); });
                if (_main) Release("main pause", delegate { _time.SetPaused(TimeManager.PauseLayer.Main, false, this); });
            }
            _main = _ui = _camera = false; _time = null; _pauseOwners = null;
            if (_cursorOwned) Release("cursor", delegate { Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible; });
            _cursorOwned = false;
        }
    }
}
