using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Parser;

public class ParserCsv
{
    private const string Header = "Неверный фрагмент,Местоположение,Описание";
    private List<string>? _lines;

    public IReadOnlyList<string>? Lines => _lines;

    public void Build(ParseResult result)
    {
        _lines = new List<string>();
        if (result.Errors.Any())
        {
            foreach (var error in result.Errors)
                _lines.Add($"\"{error.Fragment}\",\"{error.Location}\",\"{error.Description}\"");
        }
    }

    public void WriteToStream(TextWriter writer)
    {
        writer.WriteLine(Header);
        if (_lines != null)
        {
            foreach (var line in _lines)
                writer.WriteLine(line);
        }
    }

    public void PrintAll(TextWriter writer) => WriteToStream(writer);
}