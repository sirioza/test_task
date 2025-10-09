namespace StringsSorter.Models;

public struct LineEntry(long number, string text, string original)
{
    public long Number = number;
    public string Text = text;
    public string Original = original;
}
