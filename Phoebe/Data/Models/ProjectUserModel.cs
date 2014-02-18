using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class ProjectUserModel : IntermediateModel<ProjectModel, UserModel>
    {
        private static string GetPropertyName<T> (Expression<Func<ProjectUserModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        #region Data

        private bool isManager;
        public static readonly string PropertyIsManager = GetPropertyName ((m) => m.IsManager);

        [JsonProperty ("manager")]
        public bool IsManager {
            get {
                lock (SyncRoot) {
                    return isManager;
                }
            }
            set {
                lock (SyncRoot) {
                    if (isManager == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsManager, delegate {
                        isManager = value;
                    });
                }
            }
        }

        private int rate;
        public static readonly string PropertyHourlyRate = GetPropertyName ((m) => m.HourlyRate);

        [JsonProperty ("rate")]
        public int HourlyRate {
            get {
                lock (SyncRoot) {
                    return rate;
                }
            }
            set {
                lock (SyncRoot) {
                    if (rate == value)
                        return;

                    ChangePropertyAndNotify (PropertyHourlyRate, delegate {
                        rate = value;
                    });
                }
            }
        }

        #endregion

        #region Relations

        [SQLite.Ignore]
        [JsonProperty ("pid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public override ProjectModel From {
            get { return base.From; }
            set { base.From = value; }
        }

        [SQLite.Ignore]
        [JsonProperty ("uid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public override UserModel To {
            get { return base.To; }
            set { base.To = value; }
        }

        #endregion
    }
}

