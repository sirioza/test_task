using StringsSorter.Models;

namespace StringsSorter.Helpers
{
    internal static class CompareHelper
    {
        public static int CompareLines((string, long) a, (string, long) b) =>
            Compare(a.Item1, b.Item1, a.Item2, b.Item2);

        public static int CompareLines(LineEntry a, LineEntry b) =>
            Compare(a.Text, b.Text, a.Number, b.Number);

        private static int Compare(string strA, string strB, long numA, long numB)
        {
            int cmp = string.CompareOrdinal(strA, strB);

            return cmp != 0 ? cmp : numA.CompareTo(numB);
        }
    }
}
