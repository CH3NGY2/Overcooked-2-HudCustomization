using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed class HudTargetRegistry : IDisposable
    {
        private readonly List<HudTarget> _targets = new List<HudTarget>();
        private readonly Dictionary<RectTransform, HudTarget> _byRect = new Dictionary<RectTransform, HudTarget>();
        private readonly List<OwnerSpec> _owners = new List<OwnerSpec>();
        private readonly Harmony _harmony = new Harmony(PluginInfo.Guid + ".editor.targets");
        private readonly HudLog _log;
        private readonly IList<HudTarget> _view;
        internal HudGeometry[] Values;
        internal bool Enabled;
        internal readonly bool[] Previews = new bool[(int)HudTargetKind.Count];
        internal void ClearPreviews()
        {
            Array.Clear(Previews, 0, Previews.Length);
            foreach (HudTarget target in _targets)
                try { target.Preview(false); } catch (Exception e) { _log.ErrorOnce("HUD editor preview release " + target.Kind, e); }
        }
        internal bool HasInstance(HudTargetKind kind)
        { foreach (HudTarget target in _targets) if (target.Pickable && target.Alive && target.Kind == kind) return true; return false; }
        internal IList<HudTarget> Targets { get { return _view; } }
        internal HudTargetRegistry(HudGeometry[] values, HudLog log) { Values = values; _log = log; _view = _targets.AsReadOnly(); }
        internal void Start()
        {
            AddOwner(typeof(ClientWorkableItem), "m_guibar", typeof(ProgressUIController), HudTargetKind.Chopping, "StartSynchronising", typeof(Component));
            AddOwner(typeof(ClientCookingHandler), "m_gui", typeof(CookingUIController), HudTargetKind.Cooking, "StartSynchronising", typeof(Component));
            AddOwner(typeof(ClientMixingHandler), "m_progressUI", typeof(CookingUIController), HudTargetKind.Mixing, "StartSynchronising", typeof(Component));
            AddOwner(typeof(ClientHeatedStationGUI), "m_uiInstance", typeof(HeatValueUIController), HudTargetKind.Heat, "OnHeatValueChanged", typeof(float));
            AddOwner(typeof(ClientWashingStation), "m_progressUI", typeof(ProgressUIController), HudTargetKind.Washing, "Awake");
            AddOwner(typeof(ClientWashable), "m_progressBar", typeof(ProgressUIController), HudTargetKind.Washing, "StartSynchronising", typeof(Component));
            AddOwner(typeof(ClientFlammable), "m_fireGUIBar", typeof(ProgressUIController), HudTargetKind.Fire, "Ignite");
            Patch(Required.Method(typeof(ScoreUIController), "Awake"), null, "FixedCreated");
            Patch(Required.Method(typeof(LevelTimerUIController), "Awake"), null, "FixedCreated");
            Patch(Required.Method(typeof(RectTransformExtension), "UpdateAspectRatio"), "BeforeLayout", "AfterLayout");
            Patch(Required.Method(typeof(UI_Move), "UpdateGraphics"), null, "GraphicsRefreshed");
            Patch(Required.Method(typeof(ScoreUIController), "ScoreUpdate", typeof(TeamID), typeof(TeamMonitor.TeamScoreStats)), "BeforeScore", "AfterScore");
            _harmony.Patch(Required.Method(typeof(ScoreUIController), "ScoreUpdate", typeof(TeamID), typeof(TeamMonitor.TeamScoreStats)),
                null, null, null, new HarmonyMethod(typeof(HudTargetRegistry), "ScoreFinalizer"), null);
            Patch(Required.Method(typeof(GameUtils), "InstantiateUIControllerOnScalingHUDCanvas", typeof(GameObject)), null, "FloatingCreated");
            Discover();
        }
        private void AddOwner(Type type, string field, Type displayType, HudTargetKind kind, string method, params Type[] parameters)
        {
            try
            {
                OwnerSpec spec = new OwnerSpec { Type = type, Field = Required.Field(type, field, displayType), Kind = kind };
                Patch(Required.Method(type, method, parameters), null, "OwnerCreated");
                _owners.Add(spec);
            }
            catch (Exception error) { _log.ErrorOnce(type.FullName + "." + method + " HUD editor registration", error); }
        }
        private void Patch(MethodInfo method, string prefix, string postfix)
        {
            _harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(HudTargetRegistry), prefix),
                postfix == null ? null : new HarmonyMethod(typeof(HudTargetRegistry), postfix));
            _log.Info("HUD editor patch " + method.DeclaringType.FullName + "." + method.Name + ": " + prefix + "/" + postfix);
        }
        internal void Discover()
        {
            foreach (OwnerSpec spec in _owners)
                foreach (UnityEngine.Object owner in Resources.FindObjectsOfTypeAll(spec.Type)) ObserveOwner(owner as Component);
            foreach (ScoreUIController score in Resources.FindObjectsOfTypeAll<ScoreUIController>()) Observe(score, score, HudTargetKind.Score, true);
            foreach (LevelTimerUIController timer in Resources.FindObjectsOfTypeAll<LevelTimerUIController>()) Observe(timer, timer, HudTargetKind.Timer, true);
        }
        internal void ObserveOwner(Component owner)
        {
            if (!owner || !owner.gameObject.scene.IsValid()) return;
            foreach (OwnerSpec spec in _owners)
                if (spec.Type.IsInstanceOfType(owner))
                { Observe(owner, spec.Field.GetValue(owner) as Component, spec.Kind, true); return; }
        }
        private void Observe(Component owner, Component display, HudTargetKind kind, bool pickable)
        {
            if (!display || !display.gameObject.scene.IsValid()) return;
            RectTransform rect = display.transform as RectTransform;
            if (!rect || _byRect.ContainsKey(rect)) return;
            HudTarget target = new HudTarget(owner, display, kind, pickable, _log);
            _targets.Add(target); _byRect.Add(rect, target);
        }
        internal void Tick()
        {
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                HudTarget target = _targets[i];
                if (!target.Alive)
                { target.Restore(); _byRect.Remove(target.Rect); _targets.RemoveAt(i); continue; }
                try { target.Preview(Enabled && Previews[(int)target.Kind]); target.Apply(Values[(int)target.Kind], Enabled); }
                catch (Exception error)
                {
                    _log.ErrorOnce("HUD editor target " + target.Kind, error);
                    target.Restore(); _byRect.Remove(target.Rect); _targets.RemoveAt(i);
                }
            }
        }
        public void Dispose()
        {
            foreach (HudTarget target in _targets)
                try { target.Restore(); } catch (Exception error) { _log.ErrorOnce("HUD editor target restore " + target.Kind, error); }
            _targets.Clear(); _byRect.Clear(); _harmony.UnpatchSelf();
        }
        private sealed class OwnerSpec { internal Type Type; internal FieldInfo Field; internal HudTargetKind Kind; }
        private static HudTargetRegistry Current { get { return Plugin.Editor == null ? null : Plugin.Editor.Targets; } }
        private static void OwnerCreated(Component __instance)
        { HudTargetRegistry r = Current; if (r != null) try { r.ObserveOwner(__instance); } catch (Exception e) { r._log.ErrorOnce(__instance.GetType() + " HUD registration", e); } }
        private static void FixedCreated(Component __instance)
        { HudTargetRegistry r = Current; if (r != null) try { r.Observe(__instance, __instance, __instance is ScoreUIController ? HudTargetKind.Score : HudTargetKind.Timer, true); } catch (Exception e) { r._log.ErrorOnce("Fixed HUD registration", e); } }
        private static void BeforeLayout(RectTransformExtension __instance)
        { HudTargetRegistry r = Current; HudTarget target; if (r != null && r._byRect.TryGetValue((RectTransform)__instance.transform, out target)) try { target.BeforeLayout(); } catch (Exception e) { r._log.ErrorOnce("HUD editor before native layout", e); } }
        private static void AfterLayout(RectTransformExtension __instance)
        { HudTargetRegistry r = Current; HudTarget target; if (r != null && r._byRect.TryGetValue((RectTransform)__instance.transform, out target)) try { target.Apply(r.Values[(int)target.Kind], r.Enabled); } catch (Exception e) { r._log.ErrorOnce("HUD editor after native layout", e); } }
        private static void GraphicsRefreshed(UI_Move __instance)
        { HudTargetRegistry r = Current; HudTarget target; if (r != null && r._byRect.TryGetValue((RectTransform)__instance.transform, out target)) try { target.RefreshGraphics(); } catch (Exception e) { r._log.ErrorOnce("HUD editor UI_Move.UpdateGraphics", e); } }
        [ThreadStatic] private static ScoreUIController _scoring;
        private static void BeforeScore(ScoreUIController __instance)
        {
            _scoring = __instance;
            HudTargetRegistry r = Current; HudTarget target;
            if (r != null && r._byRect.TryGetValue((RectTransform)__instance.transform, out target))
                try { target.BeforeLayout(); } catch (Exception e) { r._log.ErrorOnce("ScoreUIController.ScoreUpdate geometry baseline", e); }
        }
        private static void AfterScore() { _scoring = null; }
        private static Exception ScoreFinalizer(Exception __exception) { _scoring = null; return __exception; }
        private static void FloatingCreated(GameObject __result)
        {
            HudTargetRegistry r = Current;
            if (r == null || !_scoring || !__result) return;
            try { r.Observe(__result.transform, __result.transform, HudTargetKind.Score, false); }
            catch (Exception e) { r._log.ErrorOnce("Score floating text HUD registration", e); }
        }
    }
}
