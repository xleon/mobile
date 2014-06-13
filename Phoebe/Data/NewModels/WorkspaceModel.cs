using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.NewModels
{
    public class WorkspaceModel : Model<WorkspaceData>
    {
        private static string GetPropertyName<T> (Expression<Func<WorkspaceModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<WorkspaceData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyIsPremium = GetPropertyName (m => m.IsPremium);
        public static readonly string PropertyDefaultRate = GetPropertyName (m => m.DefaultRate);
        public static readonly string PropertyDefaultCurrency = GetPropertyName (m => m.DefaultCurrency);
        public static readonly string PropertyProjectCreationPrivileges = GetPropertyName (m => m.ProjectCreationPrivileges);
        public static readonly string PropertyBillableRatesVisibility = GetPropertyName (m => m.BillableRatesVisibility);
        public static readonly string PropertyRoundingMode = GetPropertyName (m => m.RoundingMode);
        public static readonly string PropertyRoundingPercision = GetPropertyName (m => m.RoundingPercision);
        public static readonly string PropertyLogoUrl = GetPropertyName (m => m.LogoUrl);

        public WorkspaceModel ()
        {
        }

        public WorkspaceModel (WorkspaceData data) : base (data)
        {
        }

        public WorkspaceModel (Guid id) : base (id)
        {
        }

        protected override WorkspaceData Duplicate (WorkspaceData data)
        {
            return new WorkspaceData (data);
        }

        protected override void OnBeforeSave ()
        {
        }

        protected override void DetectChangedProperties (WorkspaceData oldData, WorkspaceData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name)
                OnPropertyChanged (PropertyName);
            if (oldData.IsPremium != newData.IsPremium)
                OnPropertyChanged (PropertyIsPremium);
            if (oldData.DefaultRate != newData.DefaultRate)
                OnPropertyChanged (PropertyDefaultRate);
            if (oldData.DefaultCurrency != newData.DefaultCurrency)
                OnPropertyChanged (PropertyDefaultCurrency);
            if (oldData.ProjectCreationPrivileges != newData.ProjectCreationPrivileges)
                OnPropertyChanged (PropertyProjectCreationPrivileges);
            if (oldData.BillableRatesVisibility != newData.BillableRatesVisibility)
                OnPropertyChanged (PropertyBillableRatesVisibility);
            if (oldData.RoundingMode != newData.RoundingMode)
                OnPropertyChanged (PropertyRoundingMode);
            if (oldData.RoundingMode != newData.RoundingMode)
                OnPropertyChanged (PropertyRoundingMode);
            if (oldData.LogoUrl != newData.LogoUrl)
                OnPropertyChanged (PropertyLogoUrl);
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

        public bool IsPremium {
            get {
                EnsureLoaded ();
                return Data.IsPremium;
            }
            set {
                if (IsPremium == value)
                    return;

                MutateData (data => data.IsPremium = value);
            }
        }

        public decimal? DefaultRate {
            get {
                EnsureLoaded ();
                return Data.DefaultRate;
            }
            set {
                if (DefaultRate == value)
                    return;

                MutateData (data => data.DefaultRate = value);
            }
        }

        public string DefaultCurrency {
            get {
                EnsureLoaded ();
                return Data.DefaultCurrency;
            }
            set {
                if (DefaultCurrency == value)
                    return;

                MutateData (data => data.DefaultCurrency = value);
            }
        }

        public AccessLevel ProjectCreationPrivileges {
            get {
                EnsureLoaded ();
                return Data.ProjectCreationPrivileges;
            }
            set {
                if (ProjectCreationPrivileges == value)
                    return;

                MutateData (data => data.ProjectCreationPrivileges = value);
            }
        }

        public AccessLevel BillableRatesVisibility {
            get {
                EnsureLoaded ();
                return Data.BillableRatesVisibility;
            }
            set {
                if (BillableRatesVisibility == value)
                    return;

                MutateData (data => data.BillableRatesVisibility = value);
            }
        }

        public RoundingMode RoundingMode {
            get {
                EnsureLoaded ();
                return Data.RoundingMode;
            }
            set {
                if (RoundingMode == value)
                    return;

                MutateData (data => data.RoundingMode = value);
            }
        }

        public int RoundingPercision {
            get {
                EnsureLoaded ();
                return Data.RoundingPercision;
            }
            set {
                if (RoundingPercision == value)
                    return;

                MutateData (data => data.RoundingPercision = value);
            }
        }

        public string LogoUrl {
            get {
                EnsureLoaded ();
                return Data.LogoUrl;
            }
            set {
                if (LogoUrl == value)
                    return;

                MutateData (data => data.LogoUrl = value);
            }
        }

        public static implicit operator WorkspaceModel (WorkspaceData data)
        {
            return new WorkspaceModel (data);
        }

        public static implicit operator WorkspaceData (WorkspaceModel model)
        {
            return model.Data;
        }
    }
}
