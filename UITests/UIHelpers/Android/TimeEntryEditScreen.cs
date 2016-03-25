using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace UITests.UIHelpers.Android
{
    public sealed class TimeEntryEditScreen
    {
        public Query NavigateUpButton { get; }
            = c => c.Marked("EditTimeEntryFragmentToolbar").Descendant().Class("ImageButton");
    }
}

