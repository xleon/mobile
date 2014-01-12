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
        /// <param name="propertyName">Property name, see example for suggested usage.</param>
        /// <param name="change">Delegate to do the actual property changing.</param>
        /// <example>
        /// // ...
        /// private string description;
        /// public static readonly string PropertyDescription = GetPropertyName ((m) => m.Description);
        /// 
        /// public string Description {
        ///     get { return description; }
        ///     set {
        ///         if (description == value)
        ///             return;
        ///         ChangePropertyAndNotify (PropertyDescription, delegate {
        ///             description = value;
        ///         });
        ///     }
        /// }
        /// // ...
        /// </example>
        protected void ChangePropertyAndNotify (string propertyName, Action change)
        {
            OnPropertyChanging (propertyName);
            change ();
            OnPropertyChanged (propertyName);
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