using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OC2HudCustomization
{
    // Cold barbeques create their heat gauge lazily. Borrow only their real display
    // prefab for the editor; do not invoke OnHeatValueChanged or heat the station.
    internal sealed class HudHeatPreviews : IDisposable
    {
        private static readonly FieldInfo NativeDisplay = Required.Field(typeof(ClientHeatedStationGUI), "m_uiInstance", typeof(HeatValueUIController));
        private readonly List<ClientHeatedStationGUI> _owners = new List<ClientHeatedStationGUI>();
        private readonly List<HeatValueUIController> _displays = new List<HeatValueUIController>();
        private readonly HudLog _log;
        internal HudHeatPreviews(HudLog log) { _log = log; }
        internal void Track(Component owner)
        {
            ClientHeatedStationGUI heat = owner as ClientHeatedStationGUI;
            if (heat && !_owners.Contains(heat)) _owners.Add(heat);
        }
        internal bool Available
        {
            get
            {
                foreach (ClientHeatedStationGUI owner in _owners)
                {
                    if (!owner || !owner.gameObject.scene.IsValid() || !owner.isActiveAndEnabled) continue;
                    HeatedStationGUI definition = owner.GetSynchronisedComponent() as HeatedStationGUI;
                    if (definition && definition.m_heatUIPrefab) return true;
                }
                return false;
            }
        }
        internal void Show(Action<Component, Component> register)
        {
            // Idempotent while this category remains selected.
            if (_displays.Count != 0) return;
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                ClientHeatedStationGUI owner = _owners[i];
                if (!owner) { _owners.RemoveAt(i); continue; }
                if (!owner.isActiveAndEnabled || NativeDisplay.GetValue(owner) as HeatValueUIController) continue;
                HeatedStationGUI definition = owner.GetSynchronisedComponent() as HeatedStationGUI;
                if (!definition || !definition.m_heatUIPrefab) continue;
                GameObject clone = null;
                try
                {
                    clone = GameUtils.InstantiateHoverIconUIController(definition.m_heatUIPrefab.gameObject,
                        NetworkUtils.FindVisualRoot(owner.gameObject), "HoverIconCanvas", definition.m_Offset);
                    clone.name = "HudEditorHeatPreview";
                    HeatValueUIController display = clone.GetComponent<HeatValueUIController>();
                    if (!display) throw new InvalidOperationException("Native heat prefab lacks HeatValueUIController");
                    clone.SetActive(false); register(owner, display); _displays.Add(display);
                }
                catch (Exception e) { if (clone) UnityEngine.Object.Destroy(clone); _log.ErrorOnce("HUD cold heat preview", e); }
            }
        }
        internal bool Owns(RectTransform rect)
        { foreach (HeatValueUIController display in _displays) if (display && display.transform == rect) return true; return false; }
        public void Dispose()
        {
            foreach (HeatValueUIController display in _displays)
                if (display) { display.gameObject.SetActive(false); UnityEngine.Object.Destroy(display.gameObject); }
            _displays.Clear();
        }
    }
}
