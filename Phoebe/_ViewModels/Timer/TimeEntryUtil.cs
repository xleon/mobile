using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Reactive;

namespace Toggl.Phoebe._ViewModels.Timer
{
    // Empty interface just to hide references to IDiffComparable
    public interface IHolder : IDiffComparable
    {
    }

    public interface ITimeEntryHolder : IHolder
    {
        RichTimeEntry Entry { get; }
        IList<RichTimeEntry> EntryCollection { get; }
        IList<string> Guids { get; }

        TimeSpan GetDuration ();
        DateTime GetStartTime ();
    }

    public interface IGrouper<T, TGroup>
    {
        IEnumerable<TGroup> Group (IEnumerable<T> items);
        IEnumerable<T> Ungroup (IEnumerable<TGroup> groups);
    }
}

