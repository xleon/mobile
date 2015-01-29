using System;
using System.Collections.Generic;
using UIKit;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Ross.DataSources
{
    public abstract class PlainDataViewSource<T> : GroupedDataViewSource<T, string, T>
    {
        protected PlainDataViewSource (UITableView tableView, IDataView<T> dataView) : base (tableView, dataView)
        {
        }

        protected override IEnumerable<string> GetSections ()
        {
            yield return String.Empty;
        }

        protected override IEnumerable<T> GetRows (string section)
        {
            return DataView.Data;
        }
    }
}
