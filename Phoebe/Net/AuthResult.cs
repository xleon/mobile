namespace Toggl.Phoebe.Net
{
    public enum AuthResult {
        None,
        Success,
        InvalidCredentials,
        NoGoogleAccount,
        NoDefaultWorkspace,
        GoogleError,
        NetworkError,
        SystemError
    }
}
