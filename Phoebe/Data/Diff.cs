using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public enum DiffSectionType {
        Copy,
        Add,
        Remove
    }

    public struct DiffSection {
        public readonly DiffSectionType Type;
        public readonly int OldIndex;
        public readonly int NewIndex;

        public DiffSection (DiffSectionType type, int oldIndex, int newIndex)
        {
            Type = type;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public override string ToString ()
        {
            return string.Format ("[{0}, NewIndex={1}, OldIndex={2}]", Enum.GetName (typeof (DiffSectionType), Type), NewIndex, OldIndex);
        }
    }

    public struct LongestCommonSubstringResult {
        private readonly bool _success;
        private readonly int _posA;
        private readonly int _posB;
        private readonly int _length;

        public LongestCommonSubstringResult (int posA, int posB, int length)
        {
            _success = true;
            _posA = posA;
            _posB = posB;
            _length = length;
        }

        public bool Success
        {
            get { return _success; }
        }

        public int PositionA
        {
            get { return _posA; }
        }

        public int PositionB
        {
            get { return _posB; }
        }

        public int Length
        {
            get { return _length; }
        }

        public override string ToString()
        {
            return _success
                   ? string.Format ("LCS ({0}, {1} x{2})", _posA, _posB, _length)
                   : "LCS (-)";
        }
    }

    // Adapted from http://devdirective.com/post/115/creating-a-reusable-though-simple-diff-implementation-in-csharp-part-3
    public static class Diff
    {
        public static IEnumerable<DiffSection> Calculate<T> (
            IList<T> listA, IList<T> listB, int startA = 0, int endA = -1, int startB = 0, int endB = -1)
        where T : IEquatable<T> {
            endA = endA > -1 ? endA : listA.Count;
            endB = endB > -1 ? endB : listB.Count;

            var lcs = FindLongestCommonSubstring (
                listA, listB, startA, endA, startB, endB);

            if (lcs.Success)
            {
                // deal with the section before
                var sectionsBefore = Calculate (
                    listA, listB, startA, lcs.PositionA, startB, lcs.PositionB);

                foreach (var section in sectionsBefore) {
                    yield return section;
                }

                // output the copy operation
                for (int i = 0; i < lcs.Length; i++) {
                    yield return new DiffSection (DiffSectionType.Copy, lcs.PositionA + i, lcs.PositionB + i);
                }

                // deal with the section after
                var sectionsAfter = Calculate (
                                        listA, listB, lcs.PositionA + lcs.Length, endA, lcs.PositionB + lcs.Length, endB);

                foreach (var section in sectionsAfter) {
                    yield return section;
                }

                yield break;
            }

            // if we get here, no LCS
            if (startA < endA)
            {
                // we got content from first collection --> deleted
                for (int i = 0; i < (endA - startA); i++) {
                    yield return new DiffSection (DiffSectionType.Remove, startA + i, startB);
                }
            }
            if (startB < endB)
            {
                // we got content from second collection --> inserted
                for (int i = 0; i < (endB - startB); i++) {
                    yield return new DiffSection (DiffSectionType.Add, startA, startB + i);
                }
            }
        }

        private static LongestCommonSubstringResult FindLongestCommonSubstring<T> (
            IList<T> listA, IList<T> listB, int startA, int endA, int startB, int endB)
        where T : IEquatable<T> {
            // default result, if we can't find anything
            var result = new LongestCommonSubstringResult();

            for (int index1 = startA; index1 < endA; index1++)
            {
                for (int index2 = startB; index2 < endB; index2++) {
                    if (listA[index1].Equals (listB[index2])) {
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
        where T : IEquatable<T> {
            int length = 0;
            while (startA < endA && startB < endB)
            {
                if (!listA[startA].Equals (listB[startB])) {
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
