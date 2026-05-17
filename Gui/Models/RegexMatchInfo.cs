namespace Gui.Models;

public class RegexMatchInfo
{
    public string Value { get; }
    public int Index { get; }
    public int Length { get; }
    public int Line { get; }
    public int Column { get; }

    public RegexMatchInfo(string value, int index, int length, int line, int column)
    {
        Value = value;
        Index = index;
        Length = length;
        Line = line;
        Column = column;
    }
}