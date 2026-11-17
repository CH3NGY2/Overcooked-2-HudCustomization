using System;
using System.Collections.Generic;

namespace OC2HudCustomization
{
    // Each property has one ownership rule, shared by recipe and contents restoration.
    internal sealed class OwnedValue<T>
    {
        private readonly Func<T> _read;
        private readonly Action<T> _write;
        private readonly Action _conflict;
        private readonly Func<T, T, bool> _equal;
        private T _original, _last;
        private bool _applied, _blocked;

        internal OwnedValue(Func<T> read, Action<T> write, Action conflict)
            : this(read, write, conflict, EqualityComparer<T>.Default.Equals) { }

        internal OwnedValue(Func<T> read, Action<T> write, Action conflict, Func<T, T, bool> equal)
        {
            _read = read; _write = write; _conflict = conflict; _equal = equal;
        }

        internal bool Apply(T value)
        {
            if (!CanApply()) return false;
            T current = _read();
            if (!_applied) _original = current;
            if (!_equal(current, value)) _write(value);
            _last = _read();
            _applied = true;
            return true;
        }

        internal bool CanApply()
        {
            if (_blocked) return false;
            if (_applied && !_equal(_read(), _last)) { Block(); return false; }
            return true;
        }

        internal void Restore()
        {
            if (!_applied) return;
            if (_equal(_read(), _last)) _write(_original);
            else Block();
            _applied = false;
        }

        private void Block()
        {
            if (!_blocked) { _blocked = true; _conflict(); }
        }
    }
}
