using System;
using UnityEngine;

namespace OC2HudCustomization
{
    // RecipeTransitionIn: horizontal travel ends at 5/9; its main unfold starts near .85.
    // Move during the tail of horizontal travel, then reveal the native ingredient animation.
    internal sealed class RecipeMotion
    {
        private bool _entering, _initialized, _horizontalSeen, _descending;
        private float _quietTime, _startProgress, _progress, _endProgress;
        private int _quietFrames;
        private Vector2 _position, _velocity, _start, _target;
        internal bool Entering { get { return _entering; } }
        internal float BranchOpacity { get; private set; }
        internal RecipeMotion(bool entering) { _entering = entering; BranchOpacity = entering ? 0f : 1f; }
        private static float Ease(float value)
        { value = Math.Max(0f, Math.Min(1f, value)); return value * value * (3f - 2f * value); }

        internal Vector2 Step(Vector2 target, Vector2 entry, float horizontalRemaining, float nativeProgress, float deltaTime)
        {
            float step = Math.Max(0f, Math.Min(deltaTime, .05f));
            if (!_initialized)
            { _position = _entering ? entry : target; _initialized = true; }
            if (_entering)
            {
                _progress = Math.Max(_progress, nativeProgress);
                BranchOpacity = Ease((_progress - .78f) / .07f);
                if (horizontalRemaining > .0001f) _horizontalSeen = true;
                if (nativeProgress >= 1f)
                { _entering = false; BranchOpacity = 1f; _position = target; return _position; }
                if (!_descending)
                {
                    _position = entry;
                    // Initial shader offset is zero before its Animator first evaluates.
                    if (!_horizontalSeen || nativeProgress < 0f)
                    {
                        if (horizontalRemaining > .0001f) { _quietTime = 0f; _quietFrames = 0; return _position; }
                        _quietTime += step; _quietFrames++;
                        if (_quietFrames < 3 || _quietTime < .05f || nativeProgress >= 0f) return _position;
                        _entering = false; BranchOpacity = 1f; return _position;
                    }
                    if (horizontalRemaining > .2f) return _position;
                    _descending = true; _start = _position; _target = target;
                    _startProgress = _progress;
                    _endProgress = Math.Min(.78f, _startProgress + .14f);
                }
                if ((target - _target).sqrMagnitude > .0001f)
                { _start = _position; _startProgress = Math.Min(_progress, _endProgress); _target = target; }
                float span = _endProgress - _startProgress;
                float amount = span > .0001f ? Ease((_progress - _startProgress) / span) : 1f;
                _position = amount >= 1f ? _target : _start + (_target - _start) * amount;
                return _position;
            }
            const float frequency = 36f;
            float decay = (float)Math.Exp(-frequency * step);
            Vector2 distance = _position - target;
            Vector2 carry = _velocity + frequency * distance;
            _position = target + (distance + carry * step) * decay;
            _velocity = (_velocity - frequency * carry * step) * decay;
            if ((_position - target).sqrMagnitude < .0025f && _velocity.sqrMagnitude < .0025f)
            { _position = target; _velocity = Vector2.zero; }
            return _position;
        }
        internal void Reset()
        {
            _entering = false; _initialized = false; _descending = false; _horizontalSeen = false;
            _quietTime = 0f; _quietFrames = 0; _velocity = Vector2.zero; BranchOpacity = 1f;
        }
    }
}
