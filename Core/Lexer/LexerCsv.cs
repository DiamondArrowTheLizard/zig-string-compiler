using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Lexer;

public class LexerCsv
{
    private const string CsvFieldNames = "Условный Код,Тип Лексемы,Лексема,Местоположение";
    private List<string>? _content;

    public IReadOnlyList<string>? Content => _content;

    public void Build(Lexer lexer)
    {
        _content = new List<string>();

        foreach (var node in lexer.Nodes)
        {
            string typeDesc = node.TokenDesc ?? "UNKNOWN";
            string value = node.WordCurrent ?? "";
            string line = $"{node.TokenCurrent:D},{typeDesc},{value},строка {node.Line}, {node.WordStart}-{node.WordEnd}";
            _content.Add(line);
        }
    }

    public void WriteToStream(TextWriter writer)
    {
        writer.WriteLine(CsvFieldNames);
        if (_content != null)
        {
            foreach (var line in _content)
                writer.WriteLine(line);
        }
    }

    public void ReadFromStream(TextReader reader)
    {
        _content = new List<string>();
        bool skipHeader = true;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (skipHeader)
            {
                skipHeader = false;
                continue;
            }
            _content.Add(line);
        }
    }

    public void PrintFieldNames(TextWriter writer)
    {
        writer.WriteLine(CsvFieldNames);
    }

    public void PrintLine(TextWriter writer, uint lineNumber)
    {
        if (_content == null)
            return;

        foreach (var line in _content)
        {
            int pos = line.IndexOf("строка ");
            if (pos >= 0)
            {
                var span = line.AsSpan(pos + 7);
                int spaceIdx = span.IndexOf(' ');
                if (spaceIdx > 0)
                    span = span[..spaceIdx];
                if (uint.TryParse(span, out uint lne) && lne == lineNumber)
                {
                    writer.WriteLine(line);
                    return;
                }
            }
        }
    }

    public void PrintAll(TextWriter writer) => WriteToStream(writer);
}