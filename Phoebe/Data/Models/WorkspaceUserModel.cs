using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class WorkspaceUserModel : IntermediateModel<WorkspaceModel, UserModel>
    {
        private static string GetPropertyName<T> (Expression<Func<WorkspaceUserModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        protected override void OnPropertyChanged (string property)
        {
            base.OnPropertyChanged (property);

            if (property == PropertyIsShared && IsShared) {
                if (!String.IsNullOrEmpty (email)
                    || !String.IsNullOrEmpty (name)) {
                    // Magic to keep the related user data up to date
                    var user = To;

                    if (user.ModifiedAt < ModifiedAt) {
                        if (!String.IsNullOrEmpty (email))
                            user.Email = email;
                        if (!String.IsNullOrEmpty (name))
                            user.Name = name;
                        user.ModifiedAt = ModifiedAt;
                        user.IsDirty = false;
                        email = null;
                        name = null;
                    }
                }
            }
        }

        #region Data

        private bool isAdmin;
        public static readonly string PropertyIsAdmin = GetPropertyName ((m) => m.IsAdmin);

        [JsonProperty ("admin")]
        public bool IsAdmin {
            get {
                lock (SyncRoot) {
                    return isAdmin;
                }
            }
            set {
                lock (SyncRoot) {
                    if (isAdmin == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsAdmin, delegate {
                        isAdmin = value;
                    });
                }
            }
        }

        private bool isActive;
        public static readonly string PropertyIsActive = GetPropertyName ((m) => m.IsActive);

        [JsonProperty ("active")]
        public bool IsActive {
            get {
                lock (SyncRoot) {
                    return isActive;
                }
            }
            set {
                lock (SyncRoot) {
                    if (isActive == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsActive, delegate {
                        isActive = value;
                    });
                }
            }
        }

        private string name;

        [JsonProperty ("name")]
        private string Name {
            get {
                lock (SyncRoot) {
                    if (!IsShared) {
                        return name;
                    } else {
                        return To.Name;
                    }
                }
            }
            set {
                lock (SyncRoot) {
                    if (!IsShared) {
                        name = value;
                    }
                }
            }
        }

        private string email;

        [JsonProperty ("email")]
        private string Email {
            get {
                lock (SyncRoot) {
                    if (!IsShared) {
                        return email;
                    } else {
                        return To.Email;
                    }
                }
            }
            set {
                lock (SyncRoot) {
                    if (!IsShared) {
                        email = value;
                    }
                }
            }
        }

        #endregion

        #region Relations

        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public override WorkspaceModel From {
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

        public static implicit operator WorkspaceModel (WorkspaceUserModel m)
        {
            return m.From;
        }

        public static implicit operator UserModel (WorkspaceUserModel m)
        {
            return m.To;
        }
    }
}
