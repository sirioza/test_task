using System.Globalization;

namespace StringsSorter.Extensions;

internal static class StringExtensions
{
    public static void ParseLine(this string line, out string text, out long num)
    {
        int dot = line.IndexOf('.');
        if (dot > 0)
        {
            long.TryParse(line.AsSpan(0, dot), NumberStyles.Integer, CultureInfo.InvariantCulture, out num);
            text = line.Substring(dot + 1);
        }
        else
        {
            num = 0;
            text = line;
        }
    }
}
