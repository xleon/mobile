using System;
using System.ComponentModel;
using System.Linq.Expressions;
using XPlatUtils;

#if false
#define NotifyPropertyChanging
#endif
namespace Toggl.Phoebe.Data
{
    public partial class Model :
        #if NotifyPropertyChanging
        INotifyPropertyChanging,
        #endif
        INotifyPropertyChanged
    {
        #if NotifyPropertyChanging
        public event PropertyChangingEventHandler PropertyChanging;
        #endif
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanging<T> (Expression<Func<T>> expr)
        {
            #if NotifyPropertyChanging
            OnPropertyChanged (GetPropertyName (expr));
            #endif
        }

        protected virtual void OnPropertyChanging (string property)
        {
            #if NotifyPropertyChanging
            var propertyChanging = PropertyChanging;
            if (propertyChanging != null)
            propertyChanging (this, new PropertyChangingEventArgs (GetPropertyName (expr)));
            #endif
        }

        /// <summary>
        /// Helper function to call PropertyChanging and PropertyChanged events before and after
        /// the property has been changed.
        /// </summary>
        /// <param name="expr">Expression in the format of () =&gt; PropertyName for the name of the argument of the events.</param>
        /// <param name="change">Delegate to do the actual property changing.</param>
        /// <typeparam name="T">Type of the property being changed (compiler will deduce this for you).</typeparam>
        protected void ChangePropertyAndNotify<T> (Expression<Func<T>> expr, Action change)
        {
            ChangePropertyAndNotify (GetPropertyName (expr), change);
        }

        protected void ChangePropertyAndNotify (string propertyName, Action change)
        {
            OnPropertyChanging (propertyName);
            change ();
            OnPropertyChanged (propertyName);
        }

        protected void OnPropertyChanged<T> (Expression<Func<T>> expr)
        {
            OnPropertyChanged (GetPropertyName (expr));
        }

        protected virtual void OnPropertyChanged (string property)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged (this, new PropertyChangedEventArgs (property));

            ServiceContainer.Resolve<Messenger> ().Publish (new ModelChangedMessage (this, property));

            // Automatically mark the object dirty, if property doesn't explicitly disable it
            var propInfo = GetType ().GetProperty (property);
            if (propInfo.GetCustomAttributes (typeof(DontDirtyAttribute), true).Length == 0) {
                MarkDirty ();
            }
        }
    }
}