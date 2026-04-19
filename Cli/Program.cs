using Core.Lexer;
using System;
using System.IO;

string? line;
var lexer = new Lexer();

while ((line = Console.ReadLine()) != null)
{
    lexer.ParseLine(line);
}

var csv = new LexerCsv();
csv.Build(lexer);
csv.WriteToStream(Console.Out);