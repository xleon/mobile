using UITests.UIHelpers.Android;

namespace UITests.UIHelpers
{
    public sealed class UI
    {
        public LoginScreen LoginScreen { get; } = new LoginScreen();
        public MainScreen MainScreen { get; } = new MainScreen();
        public TimeEntryEditScreen TimeEntryEditScreen { get; } = new TimeEntryEditScreen();
    }
}

