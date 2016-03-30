namespace Toggl.Phoebe.Net
{
    public enum AuthResult {
        None,
        Authenticating,
        Success,
        InvalidCredentials,
        NoGoogleAccount,
        NoDefaultWorkspace,
        GoogleError,
        NetworkError,
        SystemError
    }
}
