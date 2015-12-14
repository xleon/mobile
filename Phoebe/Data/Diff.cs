using System;
using System.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public enum DiffComparison {
        Different,
        Same,
        Updated
    }

    public enum DiffType {
//        Copy,
        Add,
        Remove,
        Move,
        Replace
    }

    public interface IDiffComparable
    {
        DiffComparison Compare (IDiffComparable other);
    }

    public class DiffSection<T>
    {
        public DiffType Type { get; private set; }
        public int OldIndex { get; private set; }
        public int NewIndex { get; private set; }
        public T OldItem { get; private set; }
        public T NewItem { get; private set; }

        public DiffSection (DiffType type, int oldIndex, T oldItem, int newIndex, T newItem)
        {
            Type = type;
            OldIndex = oldIndex;
            OldItem = oldItem;
            NewIndex = newIndex;
            NewItem = newItem;
        }

        static public DiffSection<T> Move (DiffSection<T> rmDiff, DiffSection<T> addDiff)
        {
            return new DiffSection<T> (DiffType.Move, rmDiff.OldIndex, rmDiff.OldItem, addDiff.NewIndex, addDiff.NewItem);
        }

        static public DiffSection<T> Replace (DiffSection<T> rmDiff, DiffSection<T> addDiff)
        {
            return new DiffSection<T> (DiffType.Replace, rmDiff.OldIndex, rmDiff.OldItem, addDiff.NewIndex, addDiff.NewItem);
        }

        public override string ToString ()
        {
            return string.Format ("[{0}, NewIndex={1}, NewItem={2}, OldIndex={3}, OldItem={4}]",
                                  Enum.GetName (typeof (DiffType), Type), NewIndex, NewItem, OldIndex, OldItem);
        }
    }

    public struct LongestCommonSubstringResult {
        public readonly bool Success;
        public readonly int PositionA;
        public readonly int PositionB;
        public readonly int Length;

        public LongestCommonSubstringResult (int posA, int posB, int length)
        {
            Success = true;
            PositionA = posA;
            PositionB = posB;
            Length = length;
        }

        public override string ToString()
        {
            return Success
                   ? string.Format ("LCS ({0}, {1} x{2})", PositionA, PositionB, Length)
                   : "LCS (-)";
        }
    }

    // Adapted from http://devdirective.com/post/115/creating-a-reusable-though-simple-diff-implementation-in-csharp-part-3
    public static class Diff
    {
        /// <summary>
        /// Calculates move and replace operation besides add, remove and copy diffs
        /// </summary>
        public static IList<DiffSection<T>> CalculateExtra<T> (
            IList<T> listA, IList<T> listB, int startA = 0, int endA = -1, int startB = 0, int endB = -1)
        where T : IDiffComparable
        {
            var diffs = Calculate (listA, listB)
                        // Calculate Replace diffs
                        .CollapsePairs ((x, y) => x.Type == DiffType.Remove && y.Type == DiffType.Add &&
                                        x.OldItem.Compare (y.NewItem) != DiffComparison.Different
                                        ? DiffSection<T>.Replace (x, y) : null)
                        .GroupBy (diff => diff.Type)
                        .ToDictionary (gr => gr.Key, gr => gr.ToList ());

            // Calculate Move diffs
            if (diffs.ContainsKey (DiffType.Add) && diffs.ContainsKey (DiffType.Remove)) {
                diffs.Add (DiffType.Move, new List<DiffSection<T>> ());

                for (var addDiffIndex = diffs[DiffType.Add].Count - 1; addDiffIndex >= 0; addDiffIndex--) {
                    var addDiff = diffs [DiffType.Add] [addDiffIndex];
                    var rmDiffIndex = diffs [DiffType.Remove].IndexOf (rmDiff =>
                                      listA [rmDiff.OldIndex].Compare (addDiff.NewItem) != DiffComparison.Different);

                    if (rmDiffIndex != -1) {
                        var rmDiff = diffs [DiffType.Remove] [rmDiffIndex];
                        diffs [DiffType.Add].RemoveAt (addDiffIndex);
                        diffs [DiffType.Remove].RemoveAt (rmDiffIndex);
                        diffs [DiffType.Move].Add (DiffSection<T>.Move (rmDiff, addDiff));
                    }
                }
            }

            return diffs.SelectMany (x => x.Value).OrderBy (x => x.NewIndex).ThenBy (x => x.OldIndex).ToList ();
        }

        public static IEnumerable<DiffSection<T>> Calculate<T> (
            IList<T> listA, IList<T> listB, int startA = 0, int endA = -1, int startB = 0, int endB = -1)
        where T : IDiffComparable
        {
            endA = endA > -1 ? endA : listA.Count;
            endB = endB > -1 ? endB : listB.Count;

            var lcs = FindLongestCommonSubstring (
                          listA, listB, startA, endA, startB, endB);

            if (lcs.Success) {
                // deal with the section before
                var sectionsBefore =
                    Calculate (listA, listB, startA, lcs.PositionA, startB, lcs.PositionB);

                foreach (var section in sectionsBefore) {
                    yield return section;
                }

                // output the copy operation
//                for (int i = 0; i < lcs.Length; i++) {
//                    int indexA = lcs.PositionA + i, indexB = lcs.PositionB + i;
//                    yield return new DiffSection<T> (DiffType.Copy, indexA, listA[indexA], indexB, listB[indexB]);
//                }

                // deal with the section after
                var sectionsAfter =
                    Calculate (listA, listB, lcs.PositionA + lcs.Length, endA, lcs.PositionB + lcs.Length, endB);

                foreach (var section in sectionsAfter) {
                    yield return section;
                }

                yield break;
            }

            // if we get here, no LCS
            var rmCount = endA - startA;
            var addCount = endB - startB;
            var end = Math.Max (rmCount, addCount);

            for (int i = 0; i < end; i++) {
                // we got content from first collection --> deleted
                if (i < rmCount) {
                    yield return new DiffSection<T> (DiffType.Remove, startA + i, listA[startA + i], startB, default (T));
                }

                // we got content from second collection --> inserted
                if (i < addCount) {
                    yield return new DiffSection<T> (DiffType.Add, startA, default (T), startB + i, listB[startB + i]);
                }

            }
        }

        private static LongestCommonSubstringResult FindLongestCommonSubstring<T> (
            IList<T> listA, IList<T> listB, int startA, int endA, int startB, int endB)
        where T : IDiffComparable
        {
            // default result, if we can't find anything
            var result = new LongestCommonSubstringResult();

            for (int index1 = startA; index1 < endA; index1++) {
                for (int index2 = startB; index2 < endB; index2++) {
                    if (listA[index1].Compare (listB[index2]) == DiffComparison.Same) {
                        int length = CountEqual (
                                         listA, listB, index1, endA, index2, endB);

                        // Is longer than what we already have --> record new LCS
                        if (length > result.Length) {
                            result = new LongestCommonSubstringResult (index1, index2, length);
                        }
                    }
                }
            }

            return result;
        }

        private static int CountEqual<T> (IList<T> listA, IList<T> listB, int startA, int endA, int startB, int endB)
        where T : IDiffComparable
        {
            int length = 0;
            while (startA < endA && startB < endB) {
                if (listA[startA].Compare (listB[startB]) != DiffComparison.Same) {
                    break;
                }
                startA++;
                startB++;
                length++;
            }
            return length;
        }
    }
}
