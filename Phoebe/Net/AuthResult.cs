namespace Toggl.Phoebe.Net
{
    public enum AuthResult {
        None,
        Authenticating,
        Success,
        InvalidCredentials,
        NoDefaultWorkspace,
        NetworkError,
        SystemError
    }
}
