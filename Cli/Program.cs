using Core.Lexer;
using Core.Parser;
using System;
using System.IO;
using System.Linq;

string? line;
var lexer = new Lexer();

while ((line = Console.ReadLine()) != null)
{
    lexer.ParseLine(line);
}

var csv = new LexerCsv();
csv.Build(lexer);
csv.WriteToStream(Console.Out);

Console.WriteLine();
Console.WriteLine("--- Parser Results ---");

var parser = new Parser();
var errors = parser.Parse(lexer.Nodes.ToList());

if (errors.Count == 0)
{
    Console.WriteLine("Parsing succeeded. No syntax errors.");
}
else
{
    Console.WriteLine($"Found {errors.Count} syntax error(s):");
    Console.WriteLine();
    Console.WriteLine($"{"Invalid Fragment",-20} {"Location",-15} Description");
    Console.WriteLine(new string('-', 80));
    foreach (var err in errors)
    {
        string location = $"Ln {err.Line}, Col {err.Column}";
        Console.WriteLine($"{err.InvalidFragment,-20} {location,-15} {err.Description}");
    }
}