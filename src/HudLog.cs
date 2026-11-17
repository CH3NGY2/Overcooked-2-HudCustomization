using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace OC2HudCustomization
{
    internal sealed class HudLog
    {
        private readonly ManualLogSource _source;
        private readonly HashSet<string> _reported = new HashSet<string>();
        internal HudLog(ManualLogSource source) { _source = source; }
        internal void Info(string message) { _source.LogInfo(message); }
        internal void WarnOnce(string key, string message)
        {
            if (_reported.Add(key)) _source.LogWarning(message);
        }
        internal void ErrorOnce(string target, Exception exception)
        {
            if (_reported.Add("error:" + target)) _source.LogError(target + "\n" + exception);
        }
    }
}
