using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    // A session-only presentation lease. Never call SetState/SetProgress on the gameplay
    // controller: those methods also change timers, sounds and completion animations.
    internal sealed class HudPreview : IDisposable
    {
        private static readonly FieldInfo WorkBar = Required.Field(typeof(ProgressUIController), "m_progressBar", typeof(ProgressBarUI));
        private static readonly FieldInfo CookBar = Required.Field(typeof(CookingUIController), "m_progressBar", typeof(ProgressBarUI));
        private static readonly FieldInfo HeatBar = Required.Field(typeof(HeatValueUIController), "m_progressBar", typeof(ProgressGaugeUI));
        private readonly List<OwnedValue<bool>> _visibility = new List<OwnedValue<bool>>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<Graphic> _graphics = new List<Graphic>();
        private readonly List<OwnedValue<bool>> _enabled = new List<OwnedValue<bool>>();
        private OwnedValue<float> _value;
        private readonly HudLog _log;
        internal HudPreview(RectTransform root, HudLog log)
        {
            _log = log;
            try
            {
                Component display = root.GetComponent<ProgressUIController>(); Component bar = null;
                if (display) bar = WorkBar.GetValue(display) as Component;
                else if ((display = root.GetComponent<CookingUIController>())) bar = CookBar.GetValue(display) as Component;
                else if ((display = root.GetComponent<HeatValueUIController>())) bar = HeatBar.GetValue(display) as Component;
                TrackObject(root.gameObject);
                if (bar)
                {
                    List<GameObject> path = new List<GameObject>();
                    for (Transform node = bar.transform; node && node != root; node = node.parent) path.Add(node.gameObject);
                    for (int i = path.Count - 1; i >= 0; i--) TrackObject(path[i]);
                    ProgressBarUI progress = bar as ProgressBarUI;
                    ProgressGaugeUI gauge = bar as ProgressGaugeUI;
                    if (progress) _value = new OwnedValue<float>(delegate { return progress.Value; }, delegate(float v) { if (progress) progress.SetValue(v); }, Conflict);
                    else if (gauge) _value = new OwnedValue<float>(delegate { return gauge.Value; }, delegate(float v) { if (gauge) gauge.SetValue(v); }, Conflict);
                    foreach (Graphic graphic in bar.GetComponentsInChildren<Graphic>(true)) TrackGraphic(graphic);
                    if (_value != null) _value.Apply(.5f);
                }
                else foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    if (graphic.gameObject.activeSelf) TrackGraphic(graphic);
                Tick();
            }
            catch { Dispose(); throw; }
        }
        private void Conflict() { _log.WarnOnce("hud-editor-preview-conflict", "HUD preview yielded a presentation value changed by another owner."); }
        private void TrackObject(GameObject item)
        {
            _objects.Add(item);
            _visibility.Add(new OwnedValue<bool>(delegate { return item && item.activeSelf; }, delegate(bool value) { if (item) item.SetActive(value); }, Conflict));
        }
        private void TrackGraphic(Graphic graphic)
        {
            _graphics.Add(graphic);
            _enabled.Add(new OwnedValue<bool>(delegate { return graphic && graphic.enabled; }, delegate(bool value) { if (graphic) graphic.enabled = value; }, Conflict));
        }
        internal void Tick()
        {
            for (int i = 0; i < _visibility.Count; i++) if (_objects[i]) _visibility[i].Apply(true);
            for (int i = 0; i < _enabled.Count; i++) if (_graphics[i]) _enabled[i].Apply(true);
        }
        public void Dispose()
        {
            // Restore the bar before hiding its parent, so OnDisable observes original content.
            Release(delegate { if (_value != null) _value.Restore(); }); _value = null;
            for (int i = _enabled.Count - 1; i >= 0; i--) { int index = i; Release(delegate { if (_graphics[index]) _enabled[index].Restore(); }); }
            for (int i = _visibility.Count - 1; i >= 0; i--) { int index = i; Release(delegate { if (_objects[index]) _visibility[index].Restore(); }); }
            _enabled.Clear(); _graphics.Clear(); _visibility.Clear(); _objects.Clear();
        }
        private void Release(Action action) { try { action(); } catch (Exception e) { _log.ErrorOnce("HUD preview restore", e); } }
    }
}
