using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Models
{
    public class UserModel : Model<UserData>
    {
        private static string GetPropertyName<T> (Expression<Func<UserModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<UserData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyEmail = GetPropertyName (m => m.Email);
        public static readonly string PropertyStartOfWeek = GetPropertyName (m => m.StartOfWeek);
        public static readonly string PropertyDateFormat = GetPropertyName (m => m.DateFormat);
        public static readonly string PropertyTimeFormat = GetPropertyName (m => m.TimeFormat);
        public static readonly string PropertyImageUrl = GetPropertyName (m => m.ImageUrl);
        public static readonly string PropertyLocale = GetPropertyName (m => m.Locale);
        public static readonly string PropertyTimezone = GetPropertyName (m => m.Timezone);
        public static readonly string PropertySendProductEmails = GetPropertyName (m => m.SendProductEmails);
        public static readonly string PropertySendTimerNotifications = GetPropertyName (m => m.SendTimerNotifications);
        public static readonly string PropertySendWeeklyReport = GetPropertyName (m => m.SendWeeklyReport);
        public static readonly string PropertyTrackingMode = GetPropertyName (m => m.TrackingMode);
        public static readonly string PropertyDefaultWorkspace = GetPropertyName (m => m.DefaultWorkspace);

        public UserModel ()
        {
        }

        public UserModel (UserData data) : base (data)
        {
        }

        public UserModel (Guid id) : base (id)
        {
        }

        protected override UserData Duplicate (UserData data)
        {
            return new UserData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.DefaultWorkspaceId == Guid.Empty) {
                throw new ValidationException ("DefaultWorkspace must be set for User model.");
            }
        }

        protected override void DetectChangedProperties (UserData oldData, UserData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name)
                OnPropertyChanged (PropertyName);
            if (oldData.Email != newData.Email)
                OnPropertyChanged (PropertyEmail);
            if (oldData.StartOfWeek != newData.StartOfWeek)
                OnPropertyChanged (PropertyStartOfWeek);
            if (oldData.DateFormat != newData.DateFormat)
                OnPropertyChanged (PropertyDateFormat);
            if (oldData.TimeFormat != newData.TimeFormat)
                OnPropertyChanged (PropertyTimeFormat);
            if (oldData.ImageUrl != newData.ImageUrl)
                OnPropertyChanged (PropertyImageUrl);
            if (oldData.Locale != newData.Locale)
                OnPropertyChanged (PropertyLocale);
            if (oldData.Timezone != newData.Timezone)
                OnPropertyChanged (PropertyTimezone);
            if (oldData.SendProductEmails != newData.SendProductEmails)
                OnPropertyChanged (PropertySendProductEmails);
            if (oldData.SendTimerNotifications != newData.SendTimerNotifications)
                OnPropertyChanged (PropertySendTimerNotifications);
            if (oldData.SendWeeklyReport != newData.SendWeeklyReport)
                OnPropertyChanged (PropertySendWeeklyReport);
            if (oldData.TrackingMode != newData.TrackingMode)
                OnPropertyChanged (PropertyTrackingMode);
            if (oldData.DefaultWorkspaceId != newData.DefaultWorkspaceId || defaultWorkspace.IsNewInstance)
                OnPropertyChanged (PropertyDefaultWorkspace);
        }

        public string Name {
            get {
                EnsureLoaded ();
                return Data.Name;
            }
            set {
                if (Name == value)
                    return;

                MutateData (data => data.Name = value);
            }
        }

        public string Email {
            get {
                EnsureLoaded ();
                return Data.Email;
            }
            set {
                if (Email == value)
                    return;

                MutateData (data => data.Email = value);
            }
        }

        public DayOfWeek StartOfWeek {
            get {
                EnsureLoaded ();
                return Data.StartOfWeek;
            }
            set {
                if (StartOfWeek == value)
                    return;

                MutateData (data => data.StartOfWeek = value);
            }
        }

        public string DateFormat {
            get {
                EnsureLoaded ();
                return Data.DateFormat;
            }
            set {
                if (DateFormat == value)
                    return;

                MutateData (data => data.DateFormat = value);
            }
        }

        public string TimeFormat {
            get {
                EnsureLoaded ();
                return Data.TimeFormat;
            }
            set {
                if (TimeFormat == value)
                    return;

                MutateData (data => data.TimeFormat = value);
            }
        }

        public string ImageUrl {
            get {
                EnsureLoaded ();
                return Data.ImageUrl;
            }
            set {
                if (ImageUrl == value)
                    return;

                MutateData (data => data.ImageUrl = value);
            }
        }

        public string Locale {
            get {
                EnsureLoaded ();
                return Data.Locale;
            }
            set {
                if (Locale == value)
                    return;

                MutateData (data => data.Locale = value);
            }
        }

        public string Timezone {
            get {
                EnsureLoaded ();
                return Data.Timezone;
            }
            set {
                if (Timezone == value)
                    return;

                MutateData (data => data.Timezone = value);
            }
        }

        public bool SendProductEmails {
            get {
                EnsureLoaded ();
                return Data.SendProductEmails;
            }
            set {
                if (SendProductEmails == value)
                    return;

                MutateData (data => data.SendProductEmails = value);
            }
        }

        public bool SendTimerNotifications {
            get {
                EnsureLoaded ();
                return Data.SendTimerNotifications;
            }
            set {
                if (SendTimerNotifications == value)
                    return;

                MutateData (data => data.SendTimerNotifications = value);
            }
        }

        public bool SendWeeklyReport {
            get {
                EnsureLoaded ();
                return Data.SendWeeklyReport;
            }
            set {
                if (SendWeeklyReport == value)
                    return;

                MutateData (data => data.SendWeeklyReport = value);
            }
        }

        public TrackingMode TrackingMode {
            get {
                EnsureLoaded ();
                return Data.TrackingMode;
            }
            set {
                if (TrackingMode == value)
                    return;

                MutateData (data => data.TrackingMode = value);
            }
        }

        private ForeignRelation<WorkspaceModel> defaultWorkspace;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            defaultWorkspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = m => MutateData (data => data.DefaultWorkspaceId = m.Id),
            };
        }

        [ModelRelation]
        public WorkspaceModel DefaultWorkspace {
            get { return defaultWorkspace.Get (Data.DefaultWorkspaceId); }
            set { defaultWorkspace.Set (value); }
        }

        public static implicit operator UserModel (UserData data)
        {
            return new UserModel (data);
        }

        public static implicit operator UserData (UserModel model)
        {
            return model.Data;
        }
    }
}
