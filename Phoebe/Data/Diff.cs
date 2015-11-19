using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public enum DiffSectionType
    {
        Copy,
        Add,
        Remove,
        Replace
    }

    public struct DiffSection
    {
        public readonly DiffSectionType Type;
        public readonly int OldIndex;
        public readonly int NewIndex;

        public DiffSection(DiffSectionType type, int oldIndex, int newIndex)
        {
            Type = type;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public override string ToString ()
        {
            return string.Format ("[{0}, NewIndex={1}, OldIndex={2}]", Enum.GetName(typeof(DiffSectionType), Type), NewIndex, OldIndex);
        }
    }

    public struct LongestCommonSubstringResult
    {
        private readonly bool _success;
        private readonly int _posA;
        private readonly int _posB;
        private readonly int _length;

        public LongestCommonSubstringResult(int posA, int posB, int length)
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
                ? string.Format("LCS ({0}, {1} x{2})", _posA, _posB, _length)
                : "LCS (-)";
        }
    }

    // Adapted from http://devdirective.com/post/115/creating-a-reusable-though-simple-diff-implementation-in-csharp-part-3
    public static class Diff
    {
        public static IEnumerable<DiffSection> Calculate<T>(
            IList<T> collectionA, IList<T> collectionB, Func<T,T,bool> equals = null,
            int firstStart = 0, int firstEnd = -1, int secondStart = 0, int secondEnd = -1)
        {
            equals = equals ?? new Func<T,T,bool>((x,y) => object.Equals(x,y));
            firstEnd = firstEnd > -1 ? firstEnd : collectionA.Count;
            secondEnd = secondEnd > -1 ? secondEnd : collectionB.Count;

            var lcs = FindLongestCommonSubstring(
                collectionA, collectionB, equals,
                firstStart, firstEnd, secondStart, secondEnd);

            if (lcs.Success) {
                // deal with the section before
                var sectionsBefore = Calculate (
                    collectionA, collectionB, equals,
                    firstStart, lcs.PositionA, secondStart, lcs.PositionB);

                foreach (var section in sectionsBefore)
                    yield return section;

                // output the copy operation
                for (int i = 0; i < lcs.Length; i++)
                    yield return new DiffSection (DiffSectionType.Copy, lcs.PositionA + i, lcs.PositionB + i);

                // deal with the section after
                var sectionsAfter = Calculate (
                    collectionA, collectionB, equals,
                    lcs.PositionA + lcs.Length, firstEnd,
                    lcs.PositionB + lcs.Length, secondEnd);

                foreach (var section in sectionsAfter)
                    yield return section;

                yield break;
            }

            // if we get here, no LCS
            var deleted = firstEnd - firstStart;
            var inserted = secondEnd - secondStart;
            var replaced = Math.Max(0, Math.Min (deleted, inserted));

            if (replaced >= 0) {
                // we got content from first collection --> replaced
                for (int i = 0; i < replaced; i++)
                    yield return new DiffSection (DiffSectionType.Replace, firstStart + i, secondStart + i);
            }

            if (firstStart < firstEnd) {
                // we got content from first collection --> deleted
                for (int i = replaced; i < (firstEnd - firstStart); i++)
                    yield return new DiffSection (DiffSectionType.Remove, firstStart + i, secondStart);
            }
            if (secondStart < secondEnd) {
                // we got content from second collection --> inserted
                for (int i = replaced; i < (secondEnd - secondStart); i++)
                    yield return new DiffSection (DiffSectionType.Add, firstStart, secondStart + i);
            }
        }

        static LongestCommonSubstringResult FindLongestCommonSubstring<T>(
            IList<T> collectionA, IList<T> collectionB, Func<T,T,bool> equals,
            int firstStart, int firstEnd,int secondStart, int secondEnd)
        {
            // default result, if we can't find anything
            var result = new LongestCommonSubstringResult();

            for (int index1 = firstStart; index1 < firstEnd; index1++) {
                for (int index2 = secondStart; index2 < secondEnd; index2++) {
                    if (equals(collectionA [index1], collectionB [index2])) {
                        int length = CountEqual (
                            collectionA, collectionB, equals,
                            index1, firstEnd, index2, secondEnd);

                        // Is longer than what we already have --> record new LCS
                        if (length > result.Length) {
                            result = new LongestCommonSubstringResult (index1, index2, length);
                        }
                    }
                }
            }

            return result;
        }

        static int CountEqual<T>(
            IList<T> collectionA, IList<T> collectionB, Func<T,T,bool> equals,
            int firstStart, int firstEnd, int secondStart, int secondEnd)
        {
            int length = 0;
            while (firstStart < firstEnd && secondStart < secondEnd) {
                if (!equals(collectionA[firstStart], collectionB[secondStart]))
                    break;

                firstStart++;
                secondStart++;
                length++;
            }
            return length;
        }
    }
}
