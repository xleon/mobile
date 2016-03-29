namespace Toggl.Phoebe._Net
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
