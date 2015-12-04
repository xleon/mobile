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
        Copy,
        Add,
        Remove,
        Move,
        Replace
    }

    public interface IDiffComparable
    {
        DiffComparison Compare (IDiffComparable other);
    }

    public class DiffSection<T> {
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

        public override string ToString ()
        {
            return string.Format ("[{0}, NewIndex={1}, OldIndex={2}]", Enum.GetName (typeof (DiffType), Type), NewIndex, OldIndex);
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
        private static void AddKeys<T> (IDictionary<DiffType, List<DiffSection<T>>> diffs, params DiffType[] diffTypes)
            where T : IDiffComparable
        {
            foreach (var diffType in diffTypes)
                if (!diffs.ContainsKey (diffType))
                    diffs.Add (diffType, new List<DiffSection<T>> ());
        }

        /// <summary>
        /// Calculates move and replace operation besides add, remove and copy diffs
        /// </summary>
        public static IList<DiffSection<T>> CalculateExtra<T> (
            IList<T> listA, IList<T> listB, int startA = 0, int endA = -1, int startB = 0, int endB = -1)
        where T : IDiffComparable
        {
            var extraDiffs = new List<DiffSection<T>> ();
            var diffs = Calculate (listA, listB)
                        .GroupBy (diff => diff.Type)
                        .ToDictionary (gr => gr.Key, gr => gr.ToList ());
            AddKeys (diffs, DiffType.Add, DiffType.Remove, DiffType.Copy);

            foreach (var addDiff in diffs[DiffType.Add]) {
                var rmDiffIndex = diffs[DiffType.Remove].IndexOf (rmDiff =>
                                  listA[rmDiff.OldIndex].Compare (addDiff.NewItem) != DiffComparison.Different);

                if (rmDiffIndex != -1) {
                    var rmDiff = diffs[DiffType.Remove][rmDiffIndex];
                    extraDiffs.Add (new DiffSection<T> (DiffType.Move, rmDiff.OldIndex, rmDiff.OldItem, addDiff.NewIndex, addDiff.NewItem));
                    diffs[DiffType.Remove].RemoveAt (rmDiffIndex);
                } else {
                    extraDiffs.Add (addDiff);
                }
            }

            foreach (var diff in diffs[DiffType.Remove]) {
                extraDiffs.Add (diff);
            }

            foreach (var diff in diffs[DiffType.Copy]) {
                if (listA [diff.OldIndex].Compare (listB [diff.NewIndex]) == DiffComparison.Updated) {
                    extraDiffs.Add (new DiffSection<T> (DiffType.Replace, diff.OldIndex, diff.OldItem, diff.NewIndex, diff.NewItem));
                } else {
                    extraDiffs.Add (diff);
                }
            }

            return extraDiffs;
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
                for (int i = 0; i < lcs.Length; i++) {
                    int indexA = lcs.PositionA + i, indexB = lcs.PositionB + i;
                    yield return new DiffSection<T> (DiffType.Copy, indexA, listA[indexA], indexB, listB[indexB]);
                }

                // deal with the section after
                var sectionsAfter =
                    Calculate (listA, listB, lcs.PositionA + lcs.Length, endA, lcs.PositionB + lcs.Length, endB);

                foreach (var section in sectionsAfter) {
                    yield return section;
                }

                yield break;
            }

            // if we get here, no LCS
            if (startA < endA) {
                // we got content from first collection --> deleted
                for (int i = 0; i < (endA - startA); i++) {
                    yield return new DiffSection<T> (DiffType.Remove, startA + i, listA[startA + i], startB, default (T));
                }
            }
            if (startB < endB) {
                // we got content from second collection --> inserted
                for (int i = 0; i < (endB - startB); i++) {
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
                    if (listA[index1].Compare (listB[index2]) != DiffComparison.Different) {
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
                if (listA[startA].Compare (listB[startB]) == DiffComparison.Different) {
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
