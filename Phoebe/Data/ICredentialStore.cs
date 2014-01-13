using System;

namespace Toggl.Phoebe.Data
{
    public interface ICredentialStore
    {
        Guid? UserId { get; set; }

        string ApiToken { get; set; }

        void Clear ();
    }
}
