using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class WorkspaceModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<WorkspaceModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly RelatedModelsCollection<UserModel, WorkspaceUserModel, WorkspaceModel, UserModel> usersCollection;

        public WorkspaceModel ()
        {
            usersCollection = new RelatedModelsCollection<UserModel, WorkspaceUserModel, WorkspaceModel, UserModel> (this);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "Workspace name cannot be empty.");
                } else if (Model.Query<WorkspaceModel> (
                               (m) => m.Name == Name
                               && m.Id != Id
                           ).NotDeleted ().Count () > 0) {
                    ctx.AddError (PropertyName, "Workspace with such name already exists.");
                }
            }
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (PropertyName, delegate {
                    name = value;
                });
            }
        }

        private bool premium;
        public static readonly string PropertyIsPremium = GetPropertyName ((m) => m.IsPremium);

        [JsonProperty ("premium")]
        public bool IsPremium {
            get { return premium; }
            set {
                if (premium == value)
                    return;

                ChangePropertyAndNotify (PropertyIsPremium, delegate {
                    premium = value;
                });
            }
        }

        private bool admin;
        public static readonly string PropertyIsAdmin = GetPropertyName ((m) => m.IsAdmin);

        [JsonProperty ("admin")]
        public bool IsAdmin {
            get { return admin; }
            set {
                if (admin == value)
                    return;

                ChangePropertyAndNotify (PropertyIsAdmin, delegate {
                    admin = value;
                });
            }
        }

        private decimal? defaultRate;
        public static readonly string PropertyDefaultRate = GetPropertyName ((m) => m.DefaultRate);

        [JsonProperty ("default_hourly_rate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? DefaultRate {
            get { return defaultRate; }
            set {
                if (defaultRate == value)
                    return;

                ChangePropertyAndNotify (PropertyDefaultRate, delegate {
                    defaultRate = value;
                });
            }
        }

        private string defaultCurrency;
        public static readonly string PropertyDefaultCurrency = GetPropertyName ((m) => m.DefaultCurrency);

        [JsonProperty ("default_currency")]
        public string DefaultCurrency {
            get { return defaultCurrency; }
            set {
                if (defaultCurrency == value)
                    return;

                ChangePropertyAndNotify (PropertyDefaultCurrency, delegate {
                    defaultCurrency = value;
                });
            }
        }

        private AccessLevel projectCreationPriv = AccessLevel.Any;
        public static readonly string PropertyProjectCreationPrivileges = GetPropertyName ((m) => m.ProjectCreationPrivileges);

        public AccessLevel ProjectCreationPrivileges {
            get { return projectCreationPriv; }
            set {
                if (value != AccessLevel.Admin && value != AccessLevel.Any)
                    throw new ArgumentException ("Only a subset of access levels is allowed: Admin, Any");

                if (projectCreationPriv == value)
                    return;

                ChangePropertyAndNotify (PropertyProjectCreationPrivileges, delegate {
                    projectCreationPriv = value;
                });
            }
        }

        [JsonProperty ("only_admins_may_create_projects")]
        private bool OnlyAdminsMayCreateProjects {
            get { return ProjectCreationPrivileges == AccessLevel.Admin; }
            set { ProjectCreationPrivileges = value ? AccessLevel.Admin : AccessLevel.Any; }
        }

        private AccessLevel ratesVisbility = AccessLevel.Any;
        public static readonly string PropertyBillableRatesVisibility = GetPropertyName ((m) => m.BillableRatesVisibility);

        public AccessLevel BillableRatesVisibility {
            get { return ratesVisbility; }
            set {
                if (value != AccessLevel.Admin && value != AccessLevel.Any)
                    throw new ArgumentException ("Only a subset of access levels is allowed: Admin, Any");

                if (ratesVisbility == value)
                    return;

                ChangePropertyAndNotify (PropertyBillableRatesVisibility, delegate {
                    ratesVisbility = value;
                });
            }
        }

        [JsonProperty ("only_admins_see_billable_rates")]
        private bool OnlyAdminsSeeBillableRates {
            get { return BillableRatesVisibility == AccessLevel.Admin; }
            set { BillableRatesVisibility = value ? AccessLevel.Admin : AccessLevel.Any; }
        }

        private RoundingMode roundingMode = RoundingMode.Up;
        public static readonly string PropertyRoundingMode = GetPropertyName ((m) => m.RoundingMode);

        [JsonProperty ("rounding")]
        public RoundingMode RoundingMode {
            get { return roundingMode; }
            set {
                if (roundingMode == value)
                    return;

                ChangePropertyAndNotify (PropertyRoundingMode, delegate {
                    roundingMode = value;
                });
            }
        }

        private int roundingPercision;
        public static readonly string PropertyRoundingPercision = GetPropertyName ((m) => m.RoundingPercision);

        [JsonProperty ("rounding_minutes")]
        public int RoundingPercision {
            get { return roundingPercision; }
            set {
                if (roundingPercision == value)
                    return;

                ChangePropertyAndNotify (PropertyRoundingPercision, delegate {
                    roundingPercision = value;
                });
            }
        }

        private string logoUrl;
        public static readonly string PropertyLogoUrl = GetPropertyName ((m) => m.LogoUrl);

        [JsonProperty ("logo_url")]
        public string LogoUrl {
            get { return logoUrl; }
            set {
                if (logoUrl == value)
                    return;

                ChangePropertyAndNotify (PropertyLogoUrl, delegate {
                    logoUrl = value;
                });
            }
        }

        #endregion

        #region Relations

        public IModelQuery<ClientModel> Clients {
            get { return Model.Query<ClientModel> ((m) => m.WorkspaceId == Id); }
        }

        public IModelQuery<ProjectModel> Projects {
            get { return Model.Query<ProjectModel> ((m) => m.WorkspaceId == Id); }
        }

        public IModelQuery<TaskModel> Tasks {
            get { return Model.Query<TaskModel> ((m) => m.WorkspaceId == Id); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.WorkspaceId == Id); }
        }

        public RelatedModelsCollection<UserModel, WorkspaceUserModel, WorkspaceModel, UserModel> Users {
            get { return usersCollection; }
        }

        #endregion

    }
}
