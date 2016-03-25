using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace UITests.UIHelpers.Android
{
    public sealed class MainScreen
    {
        public Query StartStopButton { get; } = c => c.Marked("StartStopBtn");
        public Query LayoverButton { get; } = c => c.Marked("LayoverButton");
        public Query ActiveTimerData { get; }
            = c => c.Marked("MainToolbar").Descendant().Marked("TimeEntryDataView");
    }
}

