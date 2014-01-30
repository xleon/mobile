using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data.Views
{
    public class ListModelsView<T> : ModelsView<T>
        where T : Model, new()
    {
        private readonly List<T> data;

        public ListModelsView (IEnumerable<T> enumerable)
        {
            data = enumerable.ToList ();
            IsLoading = false;
            HasMore = false;
        }

        public override void Reload ()
        {
        }

        public override void LoadMore ()
        {
        }

        public override IEnumerable<T> Models {
            get { return data; }
        }

        public override long Count {
            get { return data.Count; }
        }
    }
}
