using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class WorkspaceModel : Model
    {
        public static long NextId {
            get { return Model.NextId<WorkspaceModel> (); }
        }

        #region Data

        private string name;

        [JsonProperty ("name")]
        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (() => Name, delegate {
                    name = value;
                });
            }
        }

        private bool premium;

        [JsonProperty ("premium")]
        public bool IsPremium {
            get { return premium; }
            set {
                if (premium == value)
                    return;

                ChangePropertyAndNotify (() => IsPremium, delegate {
                    premium = value;
                });
            }
        }

        private bool admin;

        [JsonProperty ("admin")]
        public bool IsAdmin {
            get { return admin; }
            set {
                if (admin == value)
                    return;

                ChangePropertyAndNotify (() => IsAdmin, delegate {
                    admin = value;
                });
            }
        }

        private decimal? defaultRate;

        [JsonProperty ("default_hourly_rate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? DefaultRate {
            get { return defaultRate; }
            set {
                if (defaultRate == value)
                    return;

                ChangePropertyAndNotify (() => DefaultRate, delegate {
                    defaultRate = value;
                });
            }
        }

        private string defaultCurrency;

        [JsonProperty ("default_currency")]
        public string DefaultCurrency {
            get { return defaultCurrency; }
            set {
                if (defaultCurrency == value)
                    return;

                ChangePropertyAndNotify (() => DefaultCurrency, delegate {
                    defaultCurrency = value;
                });
            }
        }

        private AccessLevel projectCreationPriv = AccessLevel.Any;

        public AccessLevel ProjectCreationPrivileges {
            get { return projectCreationPriv; }
            set {
                if (value != AccessLevel.Admin && value != AccessLevel.Any)
                    throw new ArgumentException ("Only a subset of access levels is allowed: Admin, Any");

                if (projectCreationPriv == value)
                    return;

                ChangePropertyAndNotify (() => ProjectCreationPrivileges, delegate {
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

        public AccessLevel BillableRatesVisibility {
            get { return ratesVisbility; }
            set {
                if (value != AccessLevel.Admin && value != AccessLevel.Any)
                    throw new ArgumentException ("Only a subset of access levels is allowed: Admin, Any");

                if (ratesVisbility == value)
                    return;

                ChangePropertyAndNotify (() => BillableRatesVisibility, delegate {
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

        [JsonProperty ("rounding")]
        public RoundingMode RoundingMode {
            get { return roundingMode; }
            set {
                if (roundingMode == value)
                    return;

                ChangePropertyAndNotify (() => RoundingMode, delegate {
                    roundingMode = value;
                });
            }
        }

        private int roundingPercision;

        [JsonProperty ("rounding_minutes")]
        public int RoundingPercision {
            get { return roundingPercision; }
            set {
                if (roundingPercision == value)
                    return;

                ChangePropertyAndNotify (() => RoundingPercision, delegate {
                    roundingPercision = value;
                });
            }
        }

        private string logoUrl;

        [JsonProperty ("logo_url")]
        public string LogoUrl {
            get { return logoUrl; }
            set {
                if (logoUrl == value)
                    return;

                ChangePropertyAndNotify (() => LogoUrl, delegate {
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

        #endregion

    }
}
