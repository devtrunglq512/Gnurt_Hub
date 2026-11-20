using System;
using System.Collections.Generic;

namespace Gnurt
{
    /// <summary>
    /// Một giá trị kèm sự kiện báo khi nó đổi (pattern Observer cho một biến).
    /// Hợp cho settings và UI: <c>IsBusy.Subscribe(busy =&gt; button.SetEnabled(!busy));</c>
    /// </summary>
    public sealed class Observable<T>
    {
        T _value;

        /// <summary>Bắn khi Value đổi sang giá trị khác.</summary>
        public event Action<T> Changed;

        public Observable(T value = default) => _value = value;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value)) return; // không đổi thì không báo
                _value = value;
                Changed?.Invoke(value);
            }
        }

        /// <summary>Nghe thay đổi và nhận luôn giá trị hiện tại. Gọi Dispose() để thôi nghe.</summary>
        public IDisposable Subscribe(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Changed += handler;
            handler(_value);
            return new Unsubscriber(this, handler);
        }

        public override string ToString() => _value?.ToString() ?? "null";

        sealed class Unsubscriber : IDisposable
        {
            Observable<T> _owner;
            readonly Action<T> _handler;

            public Unsubscriber(Observable<T> owner, Action<T> handler)
            {
                _owner = owner;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_owner == null) return;
                _owner.Changed -= _handler;
                _owner = null;
            }
        }
    }
}
