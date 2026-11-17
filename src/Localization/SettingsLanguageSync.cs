using System;

namespace OC2HudCustomization
{
    // Called at a low rate by Plugin.Update on the Unity thread. The callbacks are the real
    // game reader, tag updater and optional source-SDK notification in production.
    internal sealed class SettingsLanguageSync
    {
        private readonly Func<HudLanguage?> _read;
        private readonly Func<HudLanguage, bool> _apply;
        private readonly Func<bool> _refresh;
        private readonly Action<string, Exception> _report;
        private bool _pending = true;

        internal SettingsLanguageSync(Func<HudLanguage?> read, Func<HudLanguage, bool> apply,
            Func<bool> refresh, Action<string, Exception> report)
        { _read = read; _apply = apply; _refresh = refresh; _report = report; }

        internal void Poll()
        {
            try
            {
                HudLanguage? language = _read();
                if (language.HasValue && _apply(language.Value)) _pending = true;
            }
            catch (Exception exception) { _report("Game language lookup", exception); }
            if (!_pending) return;
            try { if (_refresh()) _pending = false; }
            catch (Exception exception) { _report("ModSettings localized metadata refresh", exception); }
        }

        internal void Invalidate() { _pending = true; }
    }
}
