using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Ross.DataSources
{
    public static class ChangesResolver
    {
        private static List<DetectionGroup> DetectGroups (List<int> data)
        {
            // Find groups of ascending indices
            var groups = new List<DetectionGroup> ();
            DetectionGroup currentGroup = null;

            foreach (var val in data) {
                if (currentGroup != null) {
                    if (val > currentGroup.Last) {
                        currentGroup.Last = val;
                        currentGroup.Size += 1;
                    } else {
                        currentGroup = null;
                    }
                }

                if (currentGroup == null) {
                    if (val >= 0) {
                        currentGroup = new DetectionGroup () {
                            First = val,
                            Last = val,
                            Size = 1,
                        };
                        groups.Add (currentGroup);
                    }
                }
            }
            return groups;
        }

        private static List<DetectionGroup> FindGroupsToKeep (List<DetectionGroup> groups)
        {
            // Find best combination of groups with most items present
            var selectionSize = groups.Count;
            if (selectionSize < 1)
                return new List<DetectionGroup> (0);

            bool[] bestSelection = new bool[selectionSize];
            int bestScore = 0;

            bool[] currentSelection = new bool[selectionSize];
            var stack = new Stack<Tuple<int, bool>> ();

            // Initial values to test for index 0
            stack.Push (new Tuple<int, bool> (0, true));
            stack.Push (new Tuple<int, bool> (0, false));

            while (stack.Count > 0) {
                var frame = stack.Pop ();
                var currentIndex = frame.Item1;
                var currentValue = frame.Item2;
                currentSelection [currentIndex] = currentValue;

                if (currentIndex + 1 < selectionSize) {
                    // Generate next instructions
                    stack.Push (new Tuple<int, bool> (currentIndex + 1, true));
                    stack.Push (new Tuple<int, bool> (currentIndex + 1, false));
                } else {
                    // Grade selection
                    var score = 0;
                    var prevLast = -1;
                    var selectedGroups = groups.Where ((_, i) => currentSelection [i]);
                    foreach (var grp in selectedGroups) {
                        if (grp.First <= prevLast)
                            break;

                        prevLast = grp.Last;
                        score += grp.Size;
                    }

                    // Found best new match:
                    if (score > bestScore) {
                        bestScore = score;
                        Array.Copy (currentSelection, bestSelection, selectionSize);
                    }
                }
            }

            return groups.Where ((_, i) => bestSelection [i]).ToList ();
        }

        public static IEnumerable<ResolveResult> Resolve<T> (List<T> oldList, List<T> newList, Func<T, T, bool> comparer)
        {
            // Make an array of old indexes that correspond to the new array items
            var idxMap = newList
                .Select (newItem => oldList.FindIndex (oldItem => comparer (newItem, oldItem)))
                .ToList ();

            // Find groups to keep
            var groups = new Queue<DetectionGroup> (FindGroupsToKeep (DetectGroups (idxMap)));

            // Determine items to delete
            for (var oldIdx = 0; oldIdx < oldList.Count; oldIdx++) {
                var oldItem = oldList [oldIdx];
                if (!newList.Any (newItem => comparer (newItem, oldItem))) {
                    yield return new ResolveResult () {
                        OldIndex = oldIdx,
                        NewIndex = -1,
                        Action = ResolvedAction.Delete,
                    };
                }
            }

            // Determine items to insert or keep:
            DetectionGroup currentGroup = null;
            for (var newIdx = 0; newIdx < idxMap.Count; newIdx++) {
                var oldIdx = idxMap [newIdx];

                // Reached end of a group:
                if (oldIdx == -1 || (currentGroup != null && currentGroup.Last < oldIdx)) {
                    currentGroup = null;
                }

                // See if we can use next group item:
                if (currentGroup == null) {
                    var nextGroup = groups.Count > 0 ? groups.Peek () : null;
                    if (nextGroup != null && nextGroup.First == oldIdx) {
                        currentGroup = groups.Dequeue ();
                    }
                }

                if (currentGroup == null) {
                    yield return new ResolveResult () {
                        OldIndex = -1,
                        NewIndex = newIdx,
                        Action = ResolvedAction.Insert,
                    };
                } else {
                    yield return new ResolveResult () {
                        OldIndex = oldIdx,
                        NewIndex = newIdx,
                        Action = ResolvedAction.Keep,
                    };
                }
            }
        }

        private class DetectionGroup
        {
            public int First;
            public int Last;
            public int Size;
        }

        public enum ResolvedAction
        {
            Insert,
            Delete,
            Keep,
        }

        public struct ResolveResult
        {
            public int OldIndex;
            public int NewIndex;
            public ResolvedAction Action;
        }
    }
}
