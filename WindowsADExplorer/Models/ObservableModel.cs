using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace WindowsADExplorer.Models
{
    public abstract class ObservableModel<TModel> : INotifyPropertyChanging, INotifyPropertyChanged
        where TModel : ObservableModel<TModel>
    {
        private readonly Dictionary<PropertyInfo, object> lookup;

        protected ObservableModel()
        {
            this.lookup = new Dictionary<PropertyInfo, object>();
        }

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected T Get<T>(Expression<Func<TModel, T>> getter)
        {
            PropertyInfo property = getProperty(getter);
            return getValue<T>(property);
        }

        private T getValue<T>(PropertyInfo property)
        {
            if (lookup.ContainsKey(property))
            {
                return (T)lookup[property];
            }
            else
            {
                T value = default(T);
                lookup.Add(property, value);
                return value;
            }
        }

        protected void Set<T>(Expression<Func<TModel, T>> setter, T value)
        {
            PropertyInfo property = getProperty(setter);
            setValue<T>(property, value, EqualityComparer<T>.Default);
        }

        private void setValue<T>(PropertyInfo property, T value, IEqualityComparer<T> comparer)
        {
            T original = getValue<T>(property);
            if (comparer.Equals(value, original))
            {
                return;
            }
            onPropertyChanging(property);
            lookup[property] = value;
            onPropertyChanged(property);
        }

        protected string GetPropertyName<T>(Expression<Func<TModel, T>> accessor)
        {
            return getProperty<T>(accessor).Name;
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<TModel, T>> accessor)
        {
            PropertyInfo property = getProperty(accessor);
            onPropertyChanged(property);
        }

        private void onPropertyChanging(PropertyInfo property)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(property.Name));
            }
        }

        private void onPropertyChanged(PropertyInfo property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property.Name));
            }
        }

        private static PropertyInfo getProperty<T>(Expression<Func<TModel, T>> accessor)
        {
            MemberExpression memberExpression = accessor.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new InvalidOperationException("The expression must refer to a property.");
            }
            PropertyInfo property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new InvalidOperationException("The expression must refer to a property.");
            }
            if (!property.DeclaringType.IsAssignableFrom(typeof(TModel)))
            {
                throw new InvalidOperationException("The expression must refer to a property of the current type.");
            }
            return property;
        }
    }
}
