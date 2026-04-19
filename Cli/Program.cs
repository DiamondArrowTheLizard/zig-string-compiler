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
Console.WriteLine("--- Парсер ---");

var parser = new Parser();
var errors = parser.Parse(lexer.Nodes.ToList());

if (errors.Count == 0)
{
    Console.WriteLine("Без ошибок.");
}
else
{
    Console.WriteLine($"Найдено ошибок: {errors.Count}");
    Console.WriteLine();
    Console.WriteLine($"{"Неверный фрагмент",-20} {"Местоположение",-15} Описание ошибки");
    Console.WriteLine(new string('-', 80));
    foreach (var err in errors)
    {
        string location = $"Строка {err.Line}, Нач {err.Column}";
        Console.WriteLine($"{err.InvalidFragment,-20} {location,-15} {err.Description}");
    }
}