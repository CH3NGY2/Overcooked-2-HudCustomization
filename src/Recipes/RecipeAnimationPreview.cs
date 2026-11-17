using UnityEngine;

namespace OC2HudCustomization
{
    // Only the native card Animator uses a display clock. Order/round/cooking timers
    // still read the native paused layers. Never change global time or a timer value.
    internal sealed class RecipeAnimationPreview
    {
        private readonly Animator _animator;
        private readonly OwnedValue<AnimatorUpdateMode> _mode;
        private readonly OwnedValue<float> _speed;
        private bool _active;
        internal RecipeAnimationPreview(Animator animator, HudLog log)
        {
            _animator = animator;
            System.Action conflict = delegate { log.WarnOnce("recipe-preview-animation", "Recipe preview yielded an Animator property changed by another owner."); };
            _mode = new OwnedValue<AnimatorUpdateMode>(delegate { return animator.updateMode; }, delegate(AnimatorUpdateMode v) { animator.updateMode = v; }, conflict);
            _speed = new OwnedValue<float>(delegate { return animator.speed; }, delegate(float v) { animator.speed = v; }, conflict);
        }
        internal void Apply(bool active)
        {
            if (!_animator) return;
            if (active)
            {
                if (_mode.CanApply() && _speed.CanApply())
                { _mode.Apply(AnimatorUpdateMode.UnscaledTime); _speed.Apply(1f); _active = true; }
            }
            else
            {
                if (!_active) return;
                _active = false;
                // New cards can be created after TimeManager's pause scan. They must
                // also remain paused if a different menu still owns a native pause.
                bool owned = _speed.CanApply();
                _speed.Restore(); _mode.Restore();
                if (owned && TimeManager.GetDeltaTime(_animator.gameObject) == 0f) _animator.speed = 0f;
            }
        }
    }
}
