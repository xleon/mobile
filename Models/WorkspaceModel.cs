using System;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Models
{
    public class WorkspaceModel : Model
    {
        public static long NextId {
            get { return Model.NextId<WorkspaceModel> (); }
        }

        #region Data

        private string name;

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

        private RoundingMode roundingMode = RoundingMode.Up;

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

        // TODO: Reverse relations

        #endregion

    }
}
