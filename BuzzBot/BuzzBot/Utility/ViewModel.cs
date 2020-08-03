using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BuzzBot.Utility
{
    public class ViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new InvalidOperationException("Can not call set with a null property name");
            if (_fields.ContainsKey(propertyName) &&
                _fields[propertyName] != null &&
                !(_fields[propertyName] is T))
                throw new InvalidOperationException($"Field is defined with a value of type {_fields[propertyName].GetType()}. " +
                                                    $"Setter called with an expected type {typeof(T)}");
            var field = _fields.ContainsKey(propertyName) ? (T) _fields[propertyName] : default(T);
            bool changed = !EqualityComparer<T>.Default.Equals(field, value);
            _fields[propertyName] = value;
            if (changed)
            {
                OnPropertyChanged(propertyName);
            }

            return changed;
        }

        public virtual T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new InvalidOperationException("Can not call set with a null property name");
            if (_fields.ContainsKey(propertyName) &&
                _fields[propertyName] != null &&
                !(_fields[propertyName] is T))
                throw new InvalidOperationException($"Field is defined with a value of type {_fields[propertyName].GetType()}. " +
                                                    $"Getter called with an expected type {typeof(T)}");
            return _fields.ContainsKey(propertyName) ? (T)_fields[propertyName] : default(T);
        }
    }
}