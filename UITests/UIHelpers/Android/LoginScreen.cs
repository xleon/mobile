using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace UITests.UIHelpers.Android
{
    public sealed class LoginScreen
    {
        public Query NoUserStartButton { get; } = c => c.Marked("StartNowButton");
    }
}

