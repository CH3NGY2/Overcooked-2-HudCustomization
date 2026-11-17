using UnityEngine;

namespace OC2HudCustomization
{
    internal sealed class AlphaLayer
    {
        private readonly GameObject _target;
        private readonly HudLog _log;
        private CanvasGroup _owned;
        private float _lastAlpha = 1f;
        private bool _blocked, _retiring;

        internal AlphaLayer(GameObject target, HudLog log) { _target = target; _log = log; }
        internal bool IsRetiring { get { return _retiring && _owned; } }

        internal void Apply(float multiplier)
        {
            if (!_target || _blocked) return;
            // Unity Destroy is deferred. A disable/re-enable in one frame must wait for our
            // retiring component instead of misclassifying it as a foreign CanvasGroup.
            if (_retiring && _owned) return;
            _retiring = false;
            if (!_owned)
            {
                if (Mathf.Approximately(multiplier, 1f)) return;
                if (_target.GetComponent<CanvasGroup>())
                {
                    _blocked = true;
                    _log.WarnOnce("existing-alpha", "Existing CanvasGroup found; opacity is left to its owner on affected objects.");
                    return;
                }
                CanvasGroup[] descendants = _target.GetComponentsInChildren<CanvasGroup>(true);
                for (int i = 0; i < descendants.Length; i++)
                    if (descendants[i].ignoreParentGroups)
                        _log.WarnOnce("ignore-parent-alpha", "A child CanvasGroup ignores parent groups; its native opacity boundary is preserved.");
                _owned = _target.AddComponent<CanvasGroup>();
                _lastAlpha = _owned.alpha;
            }
            if (!Mathf.Approximately(_owned.alpha, _lastAlpha))
            {
                _blocked = true;
                _log.WarnOnce("alpha-conflict", "Another component changed an owned CanvasGroup; opacity writes stop for this object.");
                return;
            }
            if (!Mathf.Approximately(multiplier, _lastAlpha)) _owned.alpha = multiplier;
            _lastAlpha = multiplier;
        }

        internal void Restore()
        {
            if (!_owned || _retiring) return;
            if (Mathf.Approximately(_owned.alpha, _lastAlpha))
            {
                _owned.alpha = 1f;
                Object.Destroy(_owned);
                _lastAlpha = 1f;
                _retiring = true;
            }
            else
            {
                // Ownership was transferred: do not destroy a component now used elsewhere.
                _blocked = true;
                _log.WarnOnce("alpha-restore-conflict", "Owned CanvasGroup changed externally; preserve it rather than erase another mod's state.");
            }
        }
    }
}
