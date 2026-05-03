namespace Gui.Models;

public class ParserErrorInfo
{
    public string Fragment { get; }
    public string Location { get; }
    public string Description { get; }
    public int Line { get; }
    public int Column { get; }

    public ParserErrorInfo(string fragment, string location, string description)
    {
        Fragment = fragment;
        Location = location;
        Description = description;
        Line = 0;
        Column = 0;

        if (string.IsNullOrEmpty(location))
            return;

        var parts = location.Split(',');
        if (parts.Length >= 2)
        {
            var linePart = parts[0].Replace("строка", "").Trim();
            var colPart = parts[1].Replace("позиция", "").Trim();
            int.TryParse(linePart, out int line);
            int.TryParse(colPart, out int col);
            Line = line;
            Column = col;
        }
    }
}