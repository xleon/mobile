using System;
using System.Collections.Generic;
using UIKit;

namespace Toggl.Ross.Views
{
    public static class UIViewExtensions
    {
        public static IEnumerable<UIView> TraverseTree(this UIView view)
        {
            var q = new Queue<UIView> ();
            q.Enqueue(view);

            while (q.Count > 0)
            {
                var v = q.Dequeue();
                yield return v;

                foreach (var subview in v.Subviews)
                {
                    q.Enqueue(subview);
                }
            }
        }
    }
}

